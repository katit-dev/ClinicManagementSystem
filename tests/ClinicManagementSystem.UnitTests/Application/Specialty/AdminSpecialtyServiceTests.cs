using ClinicManagementSystem.Application.DTOs.Specialty;
using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.Repositories;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ClinicManagementSystem.UnitTests.Application.AdminSpecialties;

using SpecialtyEntity = ClinicManagementSystem.Infrastructure.Models.Specialty;

public class AdminSpecialtyServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ISpecialtyRepository> _repository = new();
    private readonly Mock<IAuditLogRepository> _audit = new();
    private readonly AdminSpecialtyService _service;

    public AdminSpecialtyServiceTests()
    {
        _uow.Setup(x => x.SpecialtyRepository).Returns(_repository.Object);
        _uow.Setup(x => x.AuditLogRepository).Returns(_audit.Object);
        _service = new AdminSpecialtyService(_uow.Object, NullLogger<AdminSpecialtyService>.Instance);
    }

    [Fact]
    public async Task Create_NormalizesInputAndPersistsAuditWithGeneratedId()
    {
        _repository.Setup(x => x.AddAsync(It.IsAny<SpecialtyEntity>()))
            .Callback<SpecialtyEntity>(s => s.Id = 15).Returns(Task.CompletedTask);

        var result = await _service.CreateAsync(new() { Code = " ent ", Name = " Tai mũi họng ", Description = "  " }, 7);

        Assert.Equal(201, result.StatusCode);
        Assert.Equal("ENT", result.Content!.Code);
        Assert.Equal("Tai mũi họng", result.Content.Name);
        Assert.Null(result.Content.Description);
        Assert.True(result.Content.IsActive);
        _audit.Verify(x => x.AddAsync(It.Is<AuditLog>(a =>
            a.UserId == 7 && a.EntityId == 15 && a.Action == "CreateSpecialty")), Times.Once);
        _uow.Verify(x => x.CommitTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task Create_DuplicateCodeDoesNotWrite()
    {
        _repository.Setup(x => x.CodeExistsAsync("ENT", null)).ReturnsAsync(true);
        var result = await _service.CreateAsync(new() { Code = "ent", Name = "Tai mũi họng" }, 7);
        Assert.Equal(409, result.StatusCode);
        _uow.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Theory]
    [InlineData("", "Chuyên khoa")]
    [InlineData("ENT", "  ")]
    [InlineData("MÃ", "Chuyên khoa")]
    [InlineData("CODE WITH SPACE", "Chuyên khoa")]
    [InlineData("123456789012345678901", "Chuyên khoa")]
    public async Task Create_InvalidDataDoesNotWrite(string code, string name)
    {
        var result = await _service.CreateAsync(new() { Code = code, Name = name }, 7);
        Assert.Equal(400, result.StatusCode);
        _uow.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Update_ExcludesOwnIdAndPreservesStatusAndCreationTime()
    {
        var created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var entity = new SpecialtyEntity { Id = 3, Code = "ENT", Name = "Old", IsActive = false, CreatedAt = created };
        _repository.Setup(x => x.GetByIdAsync(3)).ReturnsAsync(entity);

        var result = await _service.UpdateAsync(3, new() { Code = "ent", Name = "Updated" }, 7);

        Assert.Equal(200, result.StatusCode);
        Assert.False(result.Content!.IsActive);
        Assert.Equal(created, result.Content.CreatedAt);
        Assert.NotNull(result.Content.UpdatedAt);
        _repository.Verify(x => x.CodeExistsAsync("ENT", 3), Times.Once);
    }

    [Fact]
    public async Task Update_MissingSpecialtyReturns404()
    {
        var result = await _service.UpdateAsync(99, new() { Code = "ENT", Name = "Updated" }, 7);
        Assert.Equal(404, result.StatusCode);
        _uow.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Disable_ActiveDependenciesReturnsConflictWithoutChangingEntity()
    {
        var entity = new SpecialtyEntity { Id = 3, Name = "ENT", IsActive = true };
        _repository.Setup(x => x.GetByIdAsync(3)).ReturnsAsync(entity);
        _repository.Setup(x => x.HasActiveDependenciesAsync(3)).ReturnsAsync(true);

        var result = await _service.SetStatusAsync(3, false, 7);

        Assert.Equal(409, result.StatusCode);
        Assert.True(entity.IsActive);
        _uow.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Disable_NoActiveDependenciesPreservesEntityAndWritesAudit()
    {
        var entity = new SpecialtyEntity { Id = 3, Code = "ENT", Name = "ENT", IsActive = true };
        _repository.Setup(x => x.GetByIdAsync(3)).ReturnsAsync(entity);
        var result = await _service.SetStatusAsync(3, false, 7);

        Assert.Equal(200, result.StatusCode);
        Assert.Equal(3, result.Content!.Id);
        Assert.False(entity.IsActive);
        _repository.Verify(x => x.DeleteAsync(It.IsAny<int>()), Times.Never);
        _audit.Verify(x => x.AddAsync(It.Is<AuditLog>(a => a.Action == "DeactivateSpecialty")), Times.Once);
    }

    [Fact]
    public async Task SetStatus_SameStateIsIdempotent()
    {
        _repository.Setup(x => x.GetByIdAsync(3)).ReturnsAsync(new SpecialtyEntity { Id = 3, IsActive = true });
        var result = await _service.SetStatusAsync(3, true, 7);
        Assert.Equal(200, result.StatusCode);
        _uow.Verify(x => x.SaveChangesAsync(), Times.Never);
        _audit.Verify(x => x.AddAsync(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task Save_AuditFailureRollsBackAndReturnsSafeError()
    {
        _audit.Setup(x => x.AddAsync(It.IsAny<AuditLog>())).ThrowsAsync(new InvalidOperationException("internal failure"));
        var result = await _service.CreateAsync(new() { Code = "ENT", Name = "ENT" }, 7);
        Assert.Equal(500, result.StatusCode);
        Assert.DoesNotContain("internal failure", result.Message);
        _uow.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        _uow.Verify(x => x.CommitTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task List_PassesFiltersAndReturnsPagination()
    {
        var rows = new List<SpecialtyEntity> { new() { Id = 1, Code = "ENT", Name = "Tai mũi họng", IsActive = true } };
        _repository.Setup(x => x.GetAdminPageAsync("ENT", true, 2, 10)).ReturnsAsync((rows, 21));
        var result = await _service.GetListAsync(new() { Keyword = " ENT ", Active = true, Page = 2, PageSize = 10 });

        Assert.Equal(200, result.StatusCode);
        Assert.Equal(21, result.Content!.TotalItems);
        Assert.Equal(3, result.Content.TotalPages);
        Assert.Single(result.Content.Items);
        Assert.Equal(2, result.Content.Page);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task List_InvalidPaginationReturns400(int page, int size)
    {
        var result = await _service.GetListAsync(new() { Page = page, PageSize = size });
        Assert.Equal(400, result.StatusCode);
        _repository.Verify(x => x.GetAdminPageAsync(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }
}
