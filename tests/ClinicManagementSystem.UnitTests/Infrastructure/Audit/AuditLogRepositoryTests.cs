using System.Reflection;
using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;

namespace ClinicManagementSystem.UnitTests.Infrastructure.Audit;

public class AuditLogRepositoryTests
{
    private static ClinicManagementDbContext Context() => new(new DbContextOptionsBuilder<ClinicManagementDbContext>()
        .UseSqlServer("Server=localhost;Database=UnusedAuditTranslation;Integrated Security=true;TrustServerCertificate=true").Options);

    [Fact]
    public void ProductionListDetailAndUserQueries_TranslateToSqlServerWithoutConnection()
    {
        using var db = Context(); var repository = new AuditLogRepository(db);
        var filter = (IQueryable<AuditLog>)typeof(AuditLogRepository).GetMethod("FilterQuery", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(repository, [new[] { "patients", "Patient" }, 7, 12, new DateTime(2026, 10, 12, 17, 0, 0), new DateTime(2026, 10, 13, 17, 0, 0)])!;
        var project = typeof(AuditLogRepository).GetMethod("Project", BindingFlags.NonPublic | BindingFlags.Static)!;
        var list = (IQueryable<AuditLogReadModel>)project.Invoke(null, [filter.OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id), false])!;
        var detail = (IQueryable<AuditLogReadModel>)project.Invoke(null, [db.AuditLogs.Where(x => x.Id == 7), true])!;
        var users = (IQueryable<AuditUserReadModel>)typeof(AuditLogRepository).GetMethod("UserQuery", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(repository, ["doctor"])!;
        var listSql = list.Skip(20).Take(20).ToQueryString(); var detailSql = detail.ToQueryString(); var usersSql = users.ToQueryString();
        Assert.Contains("[auth].[audit_logs]", listSql); Assert.Contains("ORDER BY", listSql); Assert.Contains("OFFSET", listSql);
        Assert.True(listSql.Contains("LIKE") || listSql.Contains("CHARINDEX")); Assert.Contains("[details]", detailSql); Assert.Contains("LEFT JOIN", usersSql);
        Assert.Contains("DISTINCT", usersSql); Assert.DoesNotContain("password_hash", listSql + detailSql + usersSql);
        Assert.DoesNotContain("is_active", usersSql); Assert.DoesNotContain("is_deleted", usersSql);
    }

    [Fact]
    public async Task List_UsesStableNewestOrderAndDoesNotReturnDetails()
    {
        var time = new DateTime(2026, 10, 13, 1, 0, 0);
        var user = new User { Id = 7, Username = "doctor", FullName = "Hà", IsActive = false, IsDeleted = true };
        using var db = MockContext([
            new() { Id = 1, UserId = 7, User = user, OccurredAt = time, EntityName = "Patient", Details = "legacy sensitive" },
            new() { Id = 2, UserId = 7, User = user, OccurredAt = time, EntityName = "patients", Details = "{\"source\":\"VC24_MEDICAL_ACCESS\",\"statusCode\":200}" },
            new() { Id = 3, OccurredAt = time.AddMinutes(-1), EntityName = "Patient" }
        ], [user]);
        var repository = new AuditLogRepository(db);
        var first = await repository.GetAuditPageAsync(null, null, null, null, null, 1, 2);
        Assert.Equal(3, first.TotalItems); Assert.Equal(new[] { 2, 1 }, first.Items.Select(x => x.Id));
        Assert.All(first.Items, x => Assert.Null(x.Details)); Assert.Equal("doctor", first.Items[0].Username);
        Assert.True(first.Items[0].IsUtcTimestamp); Assert.False(first.Items[1].IsUtcTimestamp);
        var second = await repository.GetAuditPageAsync(null, null, null, null, null, 2, 2);
        Assert.Equal(3, Assert.Single(second.Items).Id); Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Filters_IncludeStartExcludeNextDay_AndMatchEntityUserAndTarget()
    {
        var start = new DateTime(2026, 10, 12, 17, 0, 0); var end = start.AddDays(1);
        var rows = new List<AuditLog>();
        void Add(int id, DateTime time, string entity = "patients", int? user = 7, int? target = 12)
            => rows.Add(new() { Id = id, OccurredAt = time, UserId = user, EntityName = entity, EntityId = target });
        Add(1, start.AddTicks(-1)); Add(2, start); Add(3, end.AddTicks(-1), "Patient"); Add(4, end);
        Add(5, start, "MedicalRecord"); Add(6, start, user: 8); Add(7, start, target: 13); Add(8, start, user: null);
        using var db = MockContext(rows, []);
        var result = await new AuditLogRepository(db).GetAuditPageAsync(["patients", "Patient"], 7, 12, start, end, 1, 20);
        Assert.Equal(2, result.TotalItems); Assert.Equal(new[] { 3, 2 }, result.Items.Select(x => x.Id));
    }

    [Fact]
    public async Task HugePage_ReturnsEmptyWithoutSkipOverflow()
    {
        using var db = MockContext([new() { Id = 1 }], [new() { Id = 7 }]);
        var repository = new AuditLogRepository(db);
        Assert.Empty((await repository.GetAuditPageAsync(null, null, null, null, null, int.MaxValue, 100)).Items);
        Assert.Empty((await repository.GetAuditUsersAsync(null, int.MaxValue, 100)).Items);
    }

    [Fact]
    public async Task Detail_ReturnsOnlyRequestedRow_WithLegacyDetailsAndNullActor()
    {
        using var db = MockContext([new() { Id = 1, UserId = null, EntityName = "patients", Details = "legacy" }, new() { Id = 2 }], []);
        var repository = new AuditLogRepository(db);
        var detail = await repository.GetAuditDetailAsync(1);
        Assert.Equal("legacy", detail!.Details); Assert.Null(detail.Username); Assert.Null(detail.UserId);
        Assert.Null(await repository.GetAuditDetailAsync(99));
    }

    [Theory]
    [InlineData(null, 3)]
    [InlineData(" khóa ", 1)]
    [InlineData("doctor", 1)]
    [InlineData("19", 1)]
    [InlineData("not found", 0)]
    public async Task UserOptions_OnlyUsersWithLogs_IncludeLockedDeletedAndMissingUsers(string? keyword, int count)
    {
        using var db = MockContext([
            new() { UserId = 7 }, new() { UserId = 7 }, new() { UserId = 8 }, new() { UserId = 19 }, new() { UserId = null }
        ], [new() { Id = 7, Username = "doctor", FullName = "Hà", IsDeleted = true },
            new() { Id = 8, Username = "locked", FullName = "Đã khóa", IsActive = false }, new() { Id = 9, Username = "no-logs" }]);
        var result = await new AuditLogRepository(db).GetAuditUsersAsync(keyword, 1, 50);
        Assert.Equal(count, result.TotalItems); Assert.Equal(count, result.Items.Count);
        Assert.DoesNotContain(result.Items, x => x.Id == 9);
        if (keyword == null || keyword == "19") Assert.Null(result.Items.Single(x => x.Id == 19).Username);
    }

    private static ClinicManagementDbContext MockContext(List<AuditLog> logs, List<User> users)
    {
        var context = Context(); context.AuditLogs = logs.BuildMockDbSet().Object; context.Users = users.BuildMockDbSet().Object;
        return context;
    }
}
