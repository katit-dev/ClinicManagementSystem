using System.Linq.Expressions;
using System.Text.Json;
using ClinicManagementSystem.Application.DTOs.Service;
using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.Repositories;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable;
using Moq;

namespace ClinicManagementSystem.UnitTests.Application.AdminServices;

public class AdminServiceCatalogServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IServiceRepository> _repository = new();
    private readonly Mock<ISpecialtyRepository> _specialties = new();
    private readonly Mock<IAuditLogRepository> _audit = new();
    private readonly AdminServiceCatalogService _service;
    private static readonly DateTime Now = new(2026, 10, 12, 8, 0, 0, DateTimeKind.Utc);

    public AdminServiceCatalogServiceTests()
    {
        _uow.Setup(x => x.ServiceRepository).Returns(_repository.Object);
        _uow.Setup(x => x.SpecialtyRepository).Returns(_specialties.Object);
        _uow.Setup(x => x.AuditLogRepository).Returns(_audit.Object);
        _service = new(_uow.Object, NullLogger<AdminServiceCatalogService>.Instance, new FixedClock());
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(Now);
    }

    private static SaveServiceRequestDTO Request() => new()
    {
        Code = "DV001", Name = "Xét nghiệm máu", Price = 250000, DurationMinutes = 15
    };

    private static Service Entity(bool active = true) => new()
    {
        Id = 5, Code = "DV001", Name = "Dịch vụ cũ", Description = "Mô tả cũ",
        Price = 200000, DurationMinutes = 30, IsActive = active,
        CreatedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    [Fact]
    public async Task Create_NormalizesAndAuditsGeneratedIdInSameTransaction()
    {
        Service? inserted = null;
        AuditLog? audit = null;
        var sequence = new List<string>();
        _uow.Setup(x => x.BeginTransactionAsync()).Callback(() => sequence.Add("begin")).Returns(Task.CompletedTask);
        _repository.Setup(x => x.AcquireAdminLockAsync()).Callback(() => sequence.Add("lock")).Returns(Task.CompletedTask);
        _repository.Setup(x => x.AddAsync(It.IsAny<Service>())).Callback<Service>(s => inserted = s).Returns(Task.CompletedTask);
        _uow.Setup(x => x.SaveChangesAsync()).Callback(() => { sequence.Add("save"); inserted!.Id = 18; }).Returns(Task.CompletedTask);
        _audit.Setup(x => x.AddAsync(It.IsAny<AuditLog>())).Callback<AuditLog>(a => { audit = a; sequence.Add("audit"); }).Returns(Task.CompletedTask);
        _uow.Setup(x => x.CommitTransactionAsync()).Callback(() => sequence.Add("commit")).Returns(Task.CompletedTask);
        var request = Request(); request.Code = " dv001 "; request.Name = " Xét nghiệm máu "; request.Description = "  ";

        var result = await _service.CreateAsync(request, 7);

        Assert.Equal(201, result.StatusCode);
        Assert.Equal("DV001", result.Content!.Code);
        Assert.Equal("Xét nghiệm máu", result.Content.Name);
        Assert.Null(result.Content.Description);
        Assert.Null(result.Content.SpecialtyId);
        Assert.True(result.Content.IsActive);
        Assert.Equal(Now, result.Content.CreatedAt);
        Assert.Null(result.Content.UpdatedAt);
        Assert.Equal(18, audit!.EntityId);
        Assert.Equal(7, audit.UserId);
        Assert.Equal("CreateService", audit.Action);
        Assert.True(audit.Succeeded);
        using var json = JsonDocument.Parse(audit.Details!);
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("Before").ValueKind);
        Assert.Equal(18, json.RootElement.GetProperty("After").GetProperty("Id").GetInt32());
        Assert.Equal(new[] { "begin", "lock", "save", "audit", "save", "commit" }, sequence);
    }

    [Theory]
    [InlineData("", "Valid", 10, 15)]
    [InlineData("DV 01", "Valid", 10, 15)]
    [InlineData("DỊCHVỤ", "Valid", 10, 15)]
    [InlineData("DV001", "  ", 10, 15)]
    [InlineData("DV001", "Valid", -1, 15)]
    [InlineData("DV001", "Valid", 10, 0)]
    [InlineData("DV001", "Valid", 10, -1)]
    public async Task Create_InvalidDataDoesNotStartTransaction(string code, string name, int price, int duration)
    {
        var result = await _service.CreateAsync(new() { Code = code, Name = name, Price = price, DurationMinutes = duration }, 7);
        Assert.Equal(400, result.StatusCode);
        _uow.Verify(x => x.BeginTransactionAsync(), Times.Never);
    }

    [Theory]
    [InlineData("1.001")]
    [InlineData("10000000000")]
    public async Task Create_PriceCannotBeRoundedOrOverflowDatabasePrecision(string price)
    {
        var request = Request(); request.Price = decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(400, (await _service.CreateAsync(request, 7)).StatusCode);
        _uow.Verify(x => x.BeginTransactionAsync(), Times.Never);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Create_MissingPriceOrDurationRejected(bool missingPrice)
    {
        var request = Request();
        if (missingPrice) request.Price = null; else request.DurationMinutes = null;
        Assert.Equal(400, (await _service.CreateAsync(request, 7)).StatusCode);
        _uow.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Create_FreeServiceAndMaximumPriceAreAccepted()
    {
        var request = Request(); request.Price = 0;
        Assert.Equal(201, (await _service.CreateAsync(request, 7)).StatusCode);
        request.Price = 9999999999.99m;
        Assert.Equal(201, (await _service.CreateAsync(request, 7)).StatusCode);
    }

    [Fact]
    public async Task Create_DuplicateIncludingInactiveCodeRollsBackWithoutWrites()
    {
        _repository.Setup(x => x.CodeExistsAsync("DV001", null)).ReturnsAsync(true);
        Assert.Equal(409, (await _service.CreateAsync(Request(), 7)).StatusCode);
        _uow.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(), Times.Never);
        _audit.Verify(x => x.AddAsync(It.IsAny<AuditLog>()), Times.Never);
    }

    [Theory]
    [InlineData(false, 400)]
    [InlineData(true, 409)]
    public async Task Create_MissingOrInactiveSpecialtyRejected(bool exists, int status)
    {
        if (exists) _specialties.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(new Specialty { Id = 2, Name = "Inactive", IsActive = false });
        var request = Request(); request.SpecialtyId = 2;
        Assert.Equal(status, (await _service.CreateAsync(request, 7)).StatusCode);
        _repository.Verify(x => x.AddAsync(It.IsAny<Service>()), Times.Never);
        _uow.Verify(x => x.RollbackTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task Create_ActiveSpecialtyIsLinkedAndMapped()
    {
        _specialties.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(new Specialty { Id = 2, Name = "Tim mạch", IsActive = true });
        var request = Request(); request.SpecialtyId = 2;
        var result = await _service.CreateAsync(request, 7);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal("Tim mạch", result.Content!.SpecialtyName);
        Assert.True(result.Content.SpecialtyIsActive);
        _repository.Verify(x => x.AddAsync(It.Is<Service>(s => s.SpecialtyId == 2)), Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Update_PreservesIdStatusCreationAndAuditsBeforeAfter(bool active)
    {
        var entity = Entity(active);
        _repository.Setup(x => x.GetAdminDetailAsync(5, true)).ReturnsAsync(entity);
        AuditLog? audit = null;
        _audit.Setup(x => x.AddAsync(It.IsAny<AuditLog>())).Callback<AuditLog>(a => audit = a).Returns(Task.CompletedTask);
        var result = await _service.UpdateAsync(5, Request(), 7);

        Assert.Equal(200, result.StatusCode);
        Assert.Equal(5, result.Content!.Id);
        Assert.Equal(active, result.Content.IsActive);
        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), result.Content.CreatedAt);
        Assert.Equal(Now, result.Content.UpdatedAt);
        _repository.Verify(x => x.CodeExistsAsync("DV001", 5), Times.Once);
        using var json = JsonDocument.Parse(audit!.Details!);
        Assert.Equal("UpdateService", audit.Action);
        Assert.Equal(200000, json.RootElement.GetProperty("Before").GetProperty("Price").GetDecimal());
        Assert.Equal(250000, json.RootElement.GetProperty("After").GetProperty("Price").GetDecimal());
    }

    [Fact]
    public async Task Update_CanRemoveOptionalSpecialtyAndDescription()
    {
        var entity = Entity(); entity.SpecialtyId = 2; entity.Specialty = new Specialty { Id = 2, Name = "Tim mạch", IsActive = true };
        _repository.Setup(x => x.GetAdminDetailAsync(5, true)).ReturnsAsync(entity);
        Assert.Equal(200, (await _service.UpdateAsync(5, Request(), 7)).StatusCode);
        Assert.Null(entity.SpecialtyId); Assert.Null(entity.Specialty); Assert.Null(entity.Description);
    }

    [Fact]
    public async Task Update_MissingServiceIs404AndDoesNotWrite()
    {
        Assert.Equal(404, (await _service.UpdateAsync(99, Request(), 7)).StatusCode);
        _uow.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Deactivate_PreservesReferencesPriceAndOldSnapshotsAndHidesFromDoctorCatalog()
    {
        var entity = Entity();
        var oldOrder = new ClinicManagementSystem.Infrastructure.Models.MedicalRecordService { ServiceId = 5, UnitPriceSnapshot = 175000 };
        entity.MedicalRecordServices.Add(oldOrder);
        _repository.Setup(x => x.GetAdminDetailAsync(5, true)).ReturnsAsync(entity);
        var rows = new List<Service> { entity, new() { Id = 6, Name = "Đang dùng", IsActive = true } };
        _repository.Setup(x => x.WhereSql(It.IsAny<Expression<Func<Service, bool>>>()))
            .Returns((Expression<Func<Service, bool>> predicate) => rows.Where(predicate.Compile()).ToList().BuildMock());

        var result = await _service.DeactivateAsync(5, 7);
        var catalog = await new ServiceCatalogService(_uow.Object, NullLogger<ServiceCatalogService>.Instance).GetActiveServicesAsync();

        Assert.Equal(200, result.StatusCode); Assert.False(result.Content!.IsActive);
        Assert.Equal(200000, entity.Price); Assert.Equal("Dịch vụ cũ", entity.Name);
        Assert.Same(oldOrder, Assert.Single(entity.MedicalRecordServices));
        Assert.Equal(175000, oldOrder.UnitPriceSnapshot);
        _uow.VerifyGet(x => x.InvoiceRepository, Times.Never);
        _uow.VerifyGet(x => x.InvoiceItemRepository, Times.Never);
        Assert.Equal(6, Assert.Single(catalog.Content!).Id);
        _repository.Verify(x => x.DeleteAsync(It.IsAny<int>()), Times.Never);
        _audit.Verify(x => x.AddAsync(It.Is<AuditLog>(a => a.Action == "DeactivateService")), Times.Once);
        _uow.Verify(x => x.CommitTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task Deactivate_AlreadyInactiveIsIdempotentWithoutAuditOrSave()
    {
        _repository.Setup(x => x.GetAdminDetailAsync(5, true)).ReturnsAsync(Entity(false));
        Assert.Equal(200, (await _service.DeactivateAsync(5, 7)).StatusCode);
        _uow.Verify(x => x.SaveChangesAsync(), Times.Never);
        _audit.Verify(x => x.AddAsync(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task Deactivate_MissingServiceIs404()
    {
        Assert.Equal(404, (await _service.DeactivateAsync(99, 7)).StatusCode);
        _uow.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Theory]
    [InlineData("create")]
    [InlineData("update")]
    [InlineData("deactivate")]
    public async Task Mutation_AuditFailureRollsBackWithoutCommitOrInternalDetails(string operation)
    {
        _repository.Setup(x => x.GetAdminDetailAsync(5, true)).ReturnsAsync(Entity());
        _audit.Setup(x => x.AddAsync(It.IsAny<AuditLog>())).ThrowsAsync(new InvalidOperationException("secret SQL details"));
        var result = operation switch
        {
            "create" => await _service.CreateAsync(Request(), 7),
            "update" => await _service.UpdateAsync(5, Request(), 7),
            _ => await _service.DeactivateAsync(5, 7)
        };
        Assert.Equal(500, result.StatusCode); Assert.DoesNotContain("secret", result.Message);
        _uow.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        _uow.Verify(x => x.CommitTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task Mutation_ConcurrencyFailureIs409AndRollsBack()
    {
        _uow.Setup(x => x.SaveChangesAsync()).ThrowsAsync(new DbUpdateConcurrencyException("internal"));
        Assert.Equal(409, (await _service.CreateAsync(Request(), 7)).StatusCode);
        _uow.Verify(x => x.RollbackTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task Mutation_InvalidActorDoesNotStartTransaction()
    {
        Assert.Equal(401, (await _service.CreateAsync(Request(), 0)).StatusCode);
        Assert.Equal(401, (await _service.UpdateAsync(5, Request(), 0)).StatusCode);
        Assert.Equal(401, (await _service.DeactivateAsync(5, 0)).StatusCode);
        _uow.Verify(x => x.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task List_ForwardsFiltersPaginationAndRetainsLegacyNullableFields()
    {
        _repository.Setup(x => x.GetAdminPageAsync("DV", false, 2, 2, 10)).ReturnsAsync((new List<Service>
        { new() { Id = 5, Name = "Legacy", IsActive = false, SpecialtyId = 2 } }, 21));
        var result = await _service.GetListAsync(new() { Keyword = " DV ", Active = false, SpecialtyId = 2, Page = 2, PageSize = 10 });
        Assert.Equal(200, result.StatusCode); Assert.Equal(21, result.Content!.TotalItems);
        Assert.Equal(3, result.Content.TotalPages); Assert.Equal(2, result.Content.Page);
        var row = Assert.Single(result.Content.Items); Assert.Null(row.Code); Assert.Null(row.DurationMinutes);
    }

    [Theory]
    [InlineData(0, 10, null)]
    [InlineData(1, 0, null)]
    [InlineData(1, 101, null)]
    [InlineData(1, 10, 0)]
    public async Task List_InvalidFiltersRejected(int page, int size, int? specialty)
    {
        Assert.Equal(400, (await _service.GetListAsync(new() { Page = page, PageSize = size, SpecialtyId = specialty })).StatusCode);
        _repository.Verify(x => x.GetAdminPageAsync(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Get_InvalidAndMissingIdsHandled()
    {
        Assert.Equal(400, (await _service.GetByIdAsync(0)).StatusCode);
        Assert.Equal(404, (await _service.GetByIdAsync(99)).StatusCode);
        Assert.Equal(400, (await _service.UpdateAsync(0, Request(), 7)).StatusCode);
        Assert.Equal(400, (await _service.DeactivateAsync(0, 7)).StatusCode);
    }
}
