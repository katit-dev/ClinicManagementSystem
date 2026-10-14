using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ClinicManagementSystem.UnitTests.Infrastructure.Audit;

public class AuditLogAppendOnlyTests
{
    public static TheoryData<string> SaveOverloads => new()
    {
        "sync", "sync-true", "sync-false", "async", "async-true", "async-false"
    };

    [Theory]
    [MemberData(nameof(SaveOverloads))]
    public async Task EverySaveOverload_RejectsModifiedAuditBeforeDatabaseSave(string overload)
    {
        var interceptor = new SuppressSaveInterceptor();
        await using var context = Context(interceptor);
        var audit = ExistingAudit();
        context.Attach(audit);
        audit.Details = "Attempt to replace the original access event";

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Save(context, overload));

        Assert.Contains("append-only", error.Message);
        Assert.Equal(EntityState.Modified, context.Entry(audit).State);
        Assert.Equal(0, interceptor.SaveCalls);
    }

    [Theory]
    [MemberData(nameof(SaveOverloads))]
    public async Task EverySaveOverload_RejectsDeletedAuditBeforeDatabaseSave(string overload)
    {
        var interceptor = new SuppressSaveInterceptor();
        await using var context = Context(interceptor);
        var audit = ExistingAudit();
        context.Attach(audit);
        context.Remove(audit);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Save(context, overload));

        Assert.Contains("cannot be modified or deleted", error.Message);
        Assert.Equal(EntityState.Deleted, context.Entry(audit).State);
        Assert.Equal(0, interceptor.SaveCalls);
    }

    [Theory]
    [MemberData(nameof(SaveOverloads))]
    public async Task EverySaveOverload_AllowsAppendingNewAudit(string overload)
    {
        var interceptor = new SuppressSaveInterceptor();
        await using var context = Context(interceptor);
        var audit = ExistingAudit();
        audit.Id = 0;
        context.Add(audit);

        Assert.Equal(1, await Save(context, overload));
        Assert.Equal(1, interceptor.SaveCalls);
        Assert.Equal(EntityState.Added, context.Entry(audit).State);
    }

    [Theory]
    [MemberData(nameof(SaveOverloads))]
    public async Task EverySaveOverload_DetectsPropertyMutationWithAutomaticDetectionDisabled(string overload)
    {
        var interceptor = new SuppressSaveInterceptor();
        await using var context = Context(interceptor);
        var audit = ExistingAudit();
        context.Attach(audit);
        context.ChangeTracker.AutoDetectChangesEnabled = false;
        audit.Action = "FORGED_ACTION";
        Assert.Equal(EntityState.Unchanged, context.Entry(audit).State);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Save(context, overload));

        Assert.Equal(EntityState.Modified, context.Entry(audit).State);
        Assert.Equal(0, interceptor.SaveCalls);
    }

    [Theory]
    [MemberData(nameof(SaveOverloads))]
    public async Task EverySaveOverload_AllowsOtherEntityChangesAlongsideUnchangedAudit(string overload)
    {
        var interceptor = new SuppressSaveInterceptor();
        await using var context = Context(interceptor);
        var audit = ExistingAudit();
        var user = new User { Id = 7, Username = "doctor", PasswordHash = "unused-test-hash", IsActive = true };
        context.Attach(audit);
        context.Attach(user);
        user.FullName = "Updated display name";

        Assert.Equal(1, await Save(context, overload));
        Assert.Equal(1, interceptor.SaveCalls);
        Assert.Equal(EntityState.Unchanged, context.Entry(audit).State);
        Assert.Equal(EntityState.Modified, context.Entry(user).State);
    }

    [Fact]
    public async Task InvalidAuditMutation_BlocksEntireBatchIncludingAnAppend()
    {
        var interceptor = new SuppressSaveInterceptor();
        await using var context = Context(interceptor);
        var existing = ExistingAudit();
        context.Attach(existing);
        existing.IpAddress = "192.0.2.4";
        var append = ExistingAudit();
        append.Id = 0;
        context.Add(append);

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());

        Assert.Equal(0, interceptor.SaveCalls);
        Assert.Equal(EntityState.Added, context.Entry(append).State);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AsyncSave_ForwardsCancellationTokenToEf(bool explicitBooleanOverload)
    {
        var interceptor = new SuppressSaveInterceptor();
        await using var context = Context(interceptor);
        using var cancellation = new CancellationTokenSource();
        var audit = ExistingAudit();
        audit.Id = 0;
        context.Add(audit);

        var saved = explicitBooleanOverload
            ? await context.SaveChangesAsync(false, cancellation.Token)
            : await context.SaveChangesAsync(cancellation.Token);

        Assert.Equal(1, saved);
        Assert.Equal(cancellation.Token, interceptor.CancellationToken);
    }

    private static ClinicManagementDbContext Context(SuppressSaveInterceptor interceptor)
        => new(new DbContextOptionsBuilder<ClinicManagementDbContext>()
            .UseSqlServer("Server=localhost;Database=UnusedAuditProtection;Integrated Security=true;TrustServerCertificate=true")
            .AddInterceptors(interceptor).Options);

    private static AuditLog ExistingAudit() => new()
    {
        Id = 71, UserId = 7, Action = "VIEW_RECORD", EntityName = "medical_records", EntityId = 1204,
        Details = "Original metadata", IpAddress = "192.0.2.3", Succeeded = true,
        OccurredAt = new DateTime(2026, 10, 13, 3, 0, 0, DateTimeKind.Utc)
    };

    private static Task<int> Save(ClinicManagementDbContext context, string overload)
        => overload switch
        {
            "sync" => Task.FromResult(context.SaveChanges()),
            "sync-true" => Task.FromResult(context.SaveChanges(true)),
            "sync-false" => Task.FromResult(context.SaveChanges(false)),
            "async" => context.SaveChangesAsync(),
            "async-true" => context.SaveChangesAsync(true),
            "async-false" => context.SaveChangesAsync(false),
            _ => throw new ArgumentOutOfRangeException(nameof(overload))
        };

    private sealed class SuppressSaveInterceptor : SaveChangesInterceptor
    {
        public int SaveCalls { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        // Suppress EF persistence so these tests verify the real context/ChangeTracker
        // without opening any SQL connection or modifying a database.
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            SaveCalls++;
            return InterceptionResult<int>.SuppressWithResult(1);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            CancellationToken = cancellationToken;
            return ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(1));
        }
    }
}
