using ClinicManagementSystem.Application.DTOs.Audit;
using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Repositories;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ClinicManagementSystem.UnitTests.Application.Audit;

public class AdminAuditLogServiceTests
{
    private readonly Mock<IAuditLogRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly AdminAuditLogService _service;

    public AdminAuditLogServiceTests()
    {
        _unitOfWork.Setup(x => x.AuditLogRepository).Returns(_repository.Object);
        _repository.Setup(x => x.GetAuditPageAsync(It.IsAny<string[]?>(), It.IsAny<int?>(), It.IsAny<int?>(),
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((new List<AuditLogReadModel>(), 0));
        _repository.Setup(x => x.GetAuditUsersAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((new List<AuditUserReadModel>(), 0));
        _service = new(_unitOfWork.Object, NullLogger<AdminAuditLogService>.Instance);
    }

    [Theory]
    [InlineData(null, "2026-10-13")]
    [InlineData("2026-10-01", null)]
    [InlineData("2026-10-13", "2026-10-01")]
    [InlineData("2026-01-01", "2027-01-02")]
    [InlineData("0001-01-01", "0001-01-01")]
    [InlineData("9999-12-31", "9999-12-31")]
    public async Task InvalidDateFilters_DoNotReadDatabase(string? from, string? to)
    {
        var result = await _service.GetListAsync(new() { From = from == null ? null : DateOnly.Parse(from), To = to == null ? null : DateOnly.Parse(to) });
        Assert.Equal(400, result.StatusCode); Assert.Null(result.Content);
        _repository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0, 20, null, null, null)]
    [InlineData(1, 0, null, null, null)]
    [InlineData(1, 101, null, null, null)]
    [InlineData(1, 20, 0, null, null)]
    [InlineData(1, 20, null, -1, null)]
    public async Task InvalidIdsAndPagination_DoNotReadDatabase(int page, int size, int? user, int? entity, string? name)
    {
        Assert.Equal(400, (await _service.GetListAsync(new() { Page = page, PageSize = size, UserId = user, EntityId = entity, Entity = name })).StatusCode);
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task NullQueriesAndOversizedFilters_AreRejected()
    {
        Assert.Equal(400, (await _service.GetListAsync(null!)).StatusCode);
        Assert.Equal(400, (await _service.GetUsersAsync(null!)).StatusCode);
        Assert.Equal(400, (await _service.GetListAsync(new() { Entity = new('a', 129) })).StatusCode);
        Assert.Equal(400, (await _service.GetUsersAsync(new() { Keyword = new('a', 101) })).StatusCode);
        Assert.Equal(400, (await _service.GetUsersAsync(new() { Page = 0 })).StatusCode);
        Assert.Equal(400, (await _service.GetUsersAsync(new() { PageSize = 101 })).StatusCode);
        _repository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("2026-10-13", "2026-10-13", "2026-10-12T17:00:00Z", "2026-10-13T17:00:00Z")]
    [InlineData("2026-01-01", "2027-01-01", "2025-12-31T17:00:00Z", "2027-01-01T17:00:00Z")]
    public async Task DateBoundaries_AreVietnamCalendarConvertedToUtc(string from, string to, string startUtc, string endUtc)
    {
        Assert.Equal(200, (await _service.GetListAsync(new() { From = DateOnly.Parse(from), To = DateOnly.Parse(to), UserId = 7, EntityId = 9 })).StatusCode);
        _repository.Verify(x => x.GetAuditPageAsync(null, 7, 9,
            It.Is<DateTime?>(v => v == DateTime.Parse(startUtc).ToUniversalTime() && v.Value.Kind == DateTimeKind.Utc),
            It.Is<DateTime?>(v => v == DateTime.Parse(endUtc).ToUniversalTime() && v.Value.Kind == DateTimeKind.Utc), 1, 20), Times.Once);
    }

    [Theory]
    [InlineData(" patients ", "patients", "Patient")]
    [InlineData("MedicalRecord", "medical_records", "MedicalRecord")]
    [InlineData("PRESCRIPTIONS", "prescriptions", "Prescription")]
    [InlineData("attachments", "attachments", "Attachment")]
    [InlineData("legacy_custom", "legacy_custom", null)]
    public async Task EntityFilter_ExpandsKnownAliasesAndSupportsCustomStoredNames(string input, string first, string? second)
    {
        await _service.GetListAsync(new() { Entity = input });
        _repository.Verify(x => x.GetAuditPageAsync(It.Is<string[]?>(v => v != null && v[0] == first && (second == null ? v.Length == 1 : v.Length == 2 && v[1] == second)), null, null, null, null, 1, 20), Times.Once);
    }

    [Fact]
    public async Task AllTimeFilterAndLargeTotalPageCalculation_AreSupportedWithoutOverflow()
    {
        _repository.Setup(x => x.GetAuditPageAsync(null, null, null, null, null, int.MaxValue, 100))
            .ReturnsAsync((new List<AuditLogReadModel>(), int.MaxValue));
        var result = await _service.GetListAsync(new() { Entity = "  ", Page = int.MaxValue, PageSize = 100 });
        Assert.Equal(200, result.StatusCode); Assert.Equal(21474837, result.Content!.TotalPages);
        Assert.Equal(int.MaxValue, result.Content.Page); Assert.Empty(result.Content.Items);
    }

    [Fact]
    public async Task List_DoesNotExposeDetailsAndLabelsTimestampProvenance()
    {
        var time = new DateTime(2026, 10, 13, 1, 2, 3, DateTimeKind.Unspecified);
        _repository.Setup(x => x.GetAuditPageAsync(null, null, null, null, null, 1, 20)).ReturnsAsync((new List<AuditLogReadModel>
        {
            new() { Id = 10, UserId = 7, Username = "doctor", FullName = "Hà", Action = "VIEW_RECORD", EntityName = "medical_records", EntityId = 12, OccurredAt = time, IsUtcTimestamp = true, Succeeded = true, IpAddress = "127.0.0.1", Details = "Secret old details" },
            new() { Id = 9, UserId = null, Action = "READ", EntityName = "Patient", OccurredAt = time, IsUtcTimestamp = false }
        }, 2));
        var result = (await _service.GetListAsync(new())).Content!;
        Assert.Equal(2, result.TotalItems); Assert.Equal(1, result.TotalPages);
        Assert.True(result.Items[0].IsUtcTimestamp); Assert.False(result.Items[1].IsUtcTimestamp);
        Assert.Equal(time.Ticks, result.Items[1].OccurredAtUtc.Ticks);
        Assert.Equal(DateTimeKind.Utc, result.Items[0].OccurredAtUtc.Kind);
        Assert.Null(result.Items[1].UserId); Assert.Null(result.Items[1].Username);
        var json = System.Text.Json.JsonSerializer.Serialize(result);
        Assert.DoesNotContain("Details", json); Assert.DoesNotContain("Secret", json);
    }

    [Fact]
    public async Task Detail_NotFoundOrInvalidId_ReturnsExpectedStatus_AndFoundIncludesLegacyDetails()
    {
        Assert.Equal(400, (await _service.GetByIdAsync(0)).StatusCode);
        Assert.Equal(404, (await _service.GetByIdAsync(999)).StatusCode);
        _repository.Setup(x => x.GetAuditDetailAsync(7)).ReturnsAsync(new AuditLogReadModel
            { Id = 7, Action = "VIEW_PATIENT", EntityName = "patients", Details = "<script>alert('old')</script>", IsUtcTimestamp = false });
        var detail = (await _service.GetByIdAsync(7)).Content!;
        Assert.Equal("<script>alert('old')</script>", detail.Details); Assert.False(detail.IsUtcTimestamp);
        _repository.Verify(x => x.GetAuditDetailAsync(0), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UserOptions_TrimKeywordAndPreserveMissingUsersForNumericFallback()
    {
        _repository.Setup(x => x.GetAuditUsersAsync("doctor", 2, 50)).ReturnsAsync((new List<AuditUserReadModel>
            { new() { Id = 7, Username = "doctor", FullName = "Hà" }, new() { Id = 19 } }, 51));
        var result = (await _service.GetUsersAsync(new() { Keyword = " doctor ", Page = 2 })).Content!;
        Assert.Equal(2, result.TotalPages); Assert.Equal(2, result.Page); Assert.Equal(50, result.PageSize);
        Assert.Equal(19, result.Items[1].Id); Assert.Null(result.Items[1].Username);
    }

    [Fact]
    public async Task RepositoryFailures_ReturnSafeMessages_NoSqlOrSecretContents()
    {
        _repository.Setup(x => x.GetAuditPageAsync(It.IsAny<string[]?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>())).ThrowsAsync(new Exception("Connection string SECRET"));
        _repository.Setup(x => x.GetAuditDetailAsync(7)).ThrowsAsync(new Exception("Connection string SECRET"));
        _repository.Setup(x => x.GetAuditUsersAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>())).ThrowsAsync(new Exception("Connection string SECRET"));
        var list = await _service.GetListAsync(new()); var detail = await _service.GetByIdAsync(7); var users = await _service.GetUsersAsync(new());
        Assert.Equal(500, list.StatusCode); Assert.Equal(500, detail.StatusCode); Assert.Equal(500, users.StatusCode);
        Assert.DoesNotContain("SECRET", list.Message + detail.Message + users.Message);
        Assert.Null(list.Content); Assert.Null(detail.Content); Assert.Null(users.Content);
    }
}
