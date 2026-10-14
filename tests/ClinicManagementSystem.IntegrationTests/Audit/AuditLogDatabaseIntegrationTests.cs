using ClinicManagementSystem.Application.DTOs.Audit;
using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.Repositories;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClinicManagementSystem.IntegrationTests.Audit;

// Docker SQL Server only. Never connects to a developer/shared/production DB.
public class AuditLogDatabaseIntegrationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task GroupedQuery_PaginatesRequests_KeepsAllTargets_AndDoesNotMergeSeparateActorsOrLegacyEvents()
    {
        await using var db = await Database();
        var one = new User { Username = "access-one", PasswordHash = "test-only" };
        var two = new User { Username = "access-two", PasswordHash = "test-only" };
        db.Users.AddRange(one, two); await db.SaveChangesAsync();
        var time = new DateTime(2026, 10, 14, 15, 51, 54, DateTimeKind.Utc);
        string Meta(string request) => $"{{\"source\":\"VC24_MEDICAL_ACCESS\",\"requestId\":\"{request}\",\"method\":\"GET\",\"statusCode\":200}}";
        foreach (var request in new[] { "request-one", "request-two" })
            foreach (var id in new[] { 1, 8, 9, 10 }) db.AuditLogs.Add(Row(one.Id, id, time, "medical_records", Meta(request)));
        db.AuditLogs.AddRange(Row(two.Id, 42, time, "medical_records", Meta("request-one")),
            Row(one.Id, 1, time.AddSeconds(1), "medical_records", Meta("request-one")),
            Row(one.Id, 1, time, "medical_records", "legacy text"),
            Row(one.Id, 1, time, "medical_records", "{bad legacy JSON"));
        var failure = Row(one.Id, 1, time, "medical_records", Meta("request-one")); failure.Succeeded = false; db.AuditLogs.Add(failure);
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var repository = new AuditLogRepository(db);
        var all = await repository.GetAuditAccessPageAsync(null, null, null, null, null, 1, 20);
        Assert.Equal(7, all.TotalItems); Assert.Equal(13, all.Items.Sum(x => x.EntryCount));
        var page = await repository.GetAuditAccessPageAsync(["medical_records"], one.Id, 9, null, null, 1, 1);
        Assert.Equal(2, page.TotalItems); var first = Assert.Single(page.Items);
        Assert.Equal(4, first.EntryCount); Assert.Equal(4, first.EntityCount);
        Assert.Equal(new int?[] { 1, 8, 9, 10 }, (await repository.GetAuditAccessEntriesAsync(first.Id)).Select(x => x.EntityId));
        var second = Assert.Single((await repository.GetAuditAccessPageAsync(["medical_records"], one.Id, 9, null, null, 2, 1)).Items);
        Assert.NotEqual(first.RequestId, second.RequestId); Assert.Equal(4, second.EntryCount);
        Assert.Equal(13, await db.AuditLogs.CountAsync()); Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task WriterAndQuery_UseRealSql_LeftJoinUsersAndClinicDateBounds()
    {
        await using var db = await Database();
        var user = new User { Username = "vc24-doctor", FullName = "Bác sĩ test", PasswordHash = "test-only", IsActive = false, IsDeleted = true };
        db.Users.Add(user); await db.SaveChangesAsync();
        await using var unit = new UnitOfWork(db);
        var writer = new MedicalAuditWriter(unit);
        var start = new DateTime(2026, 10, 12, 17, 0, 0, DateTimeKind.Utc);
        await writer.WriteAsync([
            Row(user.Id, 1204, start, "medical_records", "{\"source\":\"VC24_MEDICAL_ACCESS\"}"),
            Row(user.Id, 1205, start, "MedicalRecord", "old metadata"),
            Row(null, 1206, start.AddDays(1).AddSeconds(-1), "medical_records", null),
            Row(user.Id, 1207, start.AddSeconds(-1), "medical_records", null),
            Row(user.Id, 1208, start.AddDays(1), "medical_records", null)
        ]);
        db.ChangeTracker.Clear();
        var service = new AdminAuditLogService(unit, NullLogger<AdminAuditLogService>.Instance);
        var result = await service.GetListAsync(new() { Entity = "medical_records", From = new(2026, 10, 13), To = new(2026, 10, 13), PageSize = 2 });
        Assert.Equal(200, result.StatusCode); Assert.Equal(3, result.Content!.TotalItems);
        Assert.Equal(new int?[] { 1206, 1205 }, result.Content.Items.Select(x => x.EntityId));
        var second = (await service.GetListAsync(new() { Entity = "medical_records", From = new(2026, 10, 13), To = new(2026, 10, 13), Page = 2, PageSize = 2 })).Content!;
        Assert.Equal(1204, Assert.Single(second.Items).EntityId); Assert.True(second.Items[0].IsUtcTimestamp);
        var options = (await service.GetUsersAsync(new())).Content!;
        Assert.Equal(user.Id, Assert.Single(options.Items).Id);
        var detail = (await service.GetByIdAsync(second.Items[0].Id)).Content!;
        Assert.Equal("vc24-doctor", detail.Username); Assert.Contains("VC24_MEDICAL_ACCESS", detail.Details!);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Guard_RejectsUpdateAndDelete_OriginalRowStillExists(bool delete)
    {
        await using var db = await Database();
        var row = Row(null, 1204, DateTime.UtcNow, "medical_records", "original");
        db.AuditLogs.Add(row); await db.SaveChangesAsync();
        if (delete) db.AuditLogs.Remove(row); else row.Details = "changed";
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        Assert.Equal("original", (await db.AuditLogs.SingleAsync(x => x.Id == row.Id)).Details);
    }

    private async Task<ClinicManagementDbContext> Database()
    {
        var connection = new SqlConnectionStringBuilder(factory.ConnectionString) { InitialCatalog = "VC24_" + Guid.NewGuid().ToString("N") };
        var db = new ClinicManagementDbContext(new DbContextOptionsBuilder<ClinicManagementDbContext>().UseSqlServer(connection.ConnectionString).Options);
        await db.Database.EnsureCreatedAsync(); return db;
    }
    private static AuditLog Row(int? user, int entityId, DateTime at, string entity, string? details)
        => new() { UserId = user, EntityId = entityId, EntityName = entity, Action = "VIEW_RECORD", Succeeded = true, OccurredAt = at, Details = details };
}
