using System.Reflection;
using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.UnitTests.Infrastructure.Audit;

public class AuditAccessQueryTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(9)]
    public void GroupedPagination_TranslatesToSqlBeforePaging_AndParsesJsonSafely(int? entityId)
    {
        using var db = new ClinicManagementDbContext(new DbContextOptionsBuilder<ClinicManagementDbContext>()
            .UseSqlServer("Server=localhost;Database=UnusedAuditAccessSql;Integrated Security=true;TrustServerCertificate=true").Options);
        var repository = new AuditLogRepository(db);
        var query = (IQueryable<AuditAccessReadModel>)typeof(AuditLogRepository).GetMethod("AccessQuery", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(repository, [new[] { "medical_records", "MedicalRecord" }, 4, entityId, null, null])!;
        var sql = query.OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id).Skip(20).Take(20).ToQueryString();
        Assert.Contains("JSON_VALUE", sql); Assert.Contains("ISJSON", sql); Assert.Contains("CASE", sql);
        Assert.Contains("GROUP BY", sql); Assert.Contains("LEFT JOIN", sql); Assert.Contains("OFFSET", sql);
        Assert.Contains("requestId", sql); Assert.Contains("COUNT", sql); Assert.DoesNotContain("password_hash", sql);
        if (entityId.HasValue) Assert.Contains("HAVING", sql);
        Assert.Empty(db.ChangeTracker.Entries());
    }
}
