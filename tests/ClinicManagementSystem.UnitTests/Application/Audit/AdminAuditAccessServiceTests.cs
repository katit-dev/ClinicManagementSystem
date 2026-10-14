using ClinicManagementSystem.Application.DTOs.Audit;
using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Repositories;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ClinicManagementSystem.UnitTests.Application.Audit;

public class AdminAuditAccessServiceTests
{
    private readonly Mock<IAuditLogRepository> _repo = new();
    private readonly AdminAuditLogService _service;
    public AdminAuditAccessServiceTests()
    {
        var unit = new Mock<IUnitOfWork>(); unit.SetupGet(x => x.AuditLogRepository).Returns(_repo.Object);
        _service = new(unit.Object, NullLogger<AdminAuditLogService>.Instance);
    }

    [Fact]
    public async Task List_UsesGroupCountsAndClinicDateBounds_NotRawRowCounts()
    {
        _repo.Setup(x => x.GetAuditAccessPageAsync(It.IsAny<string[]?>(), 4, 9, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 20))
            .ReturnsAsync(([new AuditAccessReadModel { Id = 88, UserId = 4, RequestId = "req-1", EntityName = "medical_records", Action = "VIEW_RECORD_HISTORY", EntryCount = 4, EntityCount = 4 }], 2));
        var response = await _service.GetAccessesAsync(new() { Entity = "medical_records", UserId = 4, EntityId = 9, From = new(2026, 10, 14), To = new(2026, 10, 14) });
        Assert.Equal(200, response.StatusCode); Assert.Equal(2, response.Content!.TotalItems);
        var access = Assert.Single(response.Content.Items); Assert.Equal(4, access.EntityCount); Assert.Equal(4, access.EntryCount);
        _repo.Verify(x => x.GetAuditAccessPageAsync(It.Is<string[]>(values => values.Contains("MedicalRecord")), 4, 9,
            new DateTime(2026, 10, 13, 17, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 14, 17, 0, 0, DateTimeKind.Utc), 1, 20), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task InvalidPageSize_IsRejectedBeforeQuery(int size)
    {
        Assert.Equal(400, (await _service.GetAccessesAsync(new() { PageSize = size })).StatusCode);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Detail_ReturnsAllOriginalTargetsAndSafeRequestFields()
    {
        const string json = "{\"source\":\"VC24_MEDICAL_ACCESS\",\"requestId\":\"req-1\",\"method\":\"GET\",\"endpoint\":\"api/patients/me/records\",\"statusCode\":200,\"returnedCount\":4}";
        _repo.Setup(x => x.GetAuditDetailAsync(88)).ReturnsAsync(new AuditLogReadModel { Id = 88, UserId = 4, Action = "VIEW_RECORD_HISTORY", EntityName = "medical_records", Details = json });
        _repo.Setup(x => x.GetAuditAccessEntriesAsync(88)).ReturnsAsync(new[] { 1, 8, 9, 10 }.Select((id, n) => new AuditLogReadModel { Id = 85 + n, EntityId = id, EntityName = "medical_records" }).ToList());
        var access = (await _service.GetAccessByIdAsync(88)).Content!;
        Assert.Equal("req-1", access.RequestId); Assert.Equal(4, access.EntryCount); Assert.Equal(4, access.EntityCount);
        Assert.Equal(new int?[] { 1, 8, 9, 10 }, access.Entries.Select(x => x.EntityId));
        Assert.Equal("GET", access.Metadata!.Method); Assert.Equal(200, access.Metadata.StatusCode); Assert.Equal(json, access.Details);
        Assert.Null(access.EntityId);
    }

    [Theory]
    [InlineData("old plain metadata")]
    [InlineData("{broken")]
    [InlineData("{\"requestId\":\"should-not-group\"}")]
    [InlineData("[]")]
    public async Task LegacyOrMalformedDetails_RemainAnIndividualEvent(string details)
    {
        _repo.Setup(x => x.GetAuditDetailAsync(7)).ReturnsAsync(new AuditLogReadModel { Id = 7, EntityId = 9, Details = details });
        _repo.Setup(x => x.GetAuditAccessEntriesAsync(7)).ReturnsAsync([new() { Id = 7, EntityId = 9 }]);
        var response = await _service.GetAccessByIdAsync(7);
        Assert.Equal(200, response.StatusCode); Assert.Equal(1, response.Content!.EntryCount); Assert.Equal(9, response.Content.EntityId);
        Assert.Null(response.Content.Metadata); Assert.Null(response.Content.RequestId); Assert.Equal(details, response.Content.Details);
    }

    [Fact]
    public async Task FileDownload_CountsTheLoggedTarget_EvenWhenJsonReturnedCountIsZero()
    {
        _repo.Setup(x => x.GetAuditDetailAsync(7)).ReturnsAsync(new AuditLogReadModel { Id = 7, EntityName = "attachments", EntityId = 65,
            Details = "{\"source\":\"VC24_MEDICAL_ACCESS\",\"requestId\":\"download\",\"statusCode\":200,\"returnedCount\":0}" });
        _repo.Setup(x => x.GetAuditAccessEntriesAsync(7)).ReturnsAsync([new() { Id = 7, EntityName = "attachments", EntityId = 65 }]);
        var result = (await _service.GetAccessByIdAsync(7)).Content!;
        Assert.Equal(1, result.EntityCount); Assert.Equal(65, result.EntityId); Assert.Equal(1, result.EntryCount);
    }

    [Theory]
    [InlineData(-1, 400)]
    [InlineData(999, 404)]
    public async Task MissingOrInvalidAnchor_IsRejected(int id, int expected)
        => Assert.Equal(expected, (await _service.GetAccessByIdAsync(id)).StatusCode);

    [Fact]
    public async Task DatabaseFailure_IsSanitized()
    {
        _repo.Setup(x => x.GetAuditAccessPageAsync(It.IsAny<string[]?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 20))
            .ThrowsAsync(new InvalidOperationException("secret SQL"));
        var result = await _service.GetAccessesAsync(new()); Assert.Equal(500, result.StatusCode); Assert.DoesNotContain("SQL", result.Message);
    }
}
