using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IAuditLogRepository : IRepositoryBase<AuditLog>
{
    Task<(List<AuditLogReadModel> Items, int TotalItems)> GetAuditPageAsync(string[]? entities, int? userId,
        int? entityId, DateTime? fromUtc, DateTime? endUtcExclusive, int page, int pageSize);
    Task<AuditLogReadModel?> GetAuditDetailAsync(int id);
    Task<(List<AuditUserReadModel> Items, int TotalItems)> GetAuditUsersAsync(string? keyword, int page, int pageSize);
    Task<(List<AuditAccessReadModel> Items, int TotalItems)> GetAuditAccessPageAsync(string[]? entities, int? userId,
        int? entityId, DateTime? fromUtc, DateTime? endUtcExclusive, int page, int pageSize);
    Task<List<AuditLogReadModel>> GetAuditAccessEntriesAsync(int anchorId);
}

// Read projections deliberately exclude passwords and patient contents. Details are only selected for a single log.
public class AuditLogReadModel
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string? Username { get; set; }
    public string? FullName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string? IpAddress { get; set; }
    public bool Succeeded { get; set; }
    public DateTime OccurredAt { get; set; }
    public bool IsUtcTimestamp { get; set; }
    public string? Details { get; set; }
}

public class AuditUserReadModel
{
    public int Id { get; set; }
    public string? Username { get; set; }
    public string? FullName { get; set; }
}

public class AuditAccessReadModel : AuditLogReadModel
{
    public string? RequestId { get; set; }
    public int EntryCount { get; set; }
    public int EntityCount { get; set; }
}

public class AuditAccessSourceReadModel
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string? IpAddress { get; set; }
    public bool Succeeded { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? RequestId { get; set; }
    public bool IsUtcTimestamp { get; set; }
}

public class AuditLogRepository : RepositoryBase<AuditLog>, IAuditLogRepository
{
    private readonly ClinicManagementDbContext context;
    public AuditLogRepository(ClinicManagementDbContext context) : base(context) => this.context = context;

    public async Task<(List<AuditLogReadModel> Items, int TotalItems)> GetAuditPageAsync(string[]? entities,
        int? userId, int? entityId, DateTime? fromUtc, DateTime? endUtcExclusive, int page, int pageSize)
    {
        var query = FilterQuery(entities, userId, entityId, fromUtc, endUtcExclusive);
        var total = await query.CountAsync();
        var skip = ((long)page - 1) * pageSize;
        if (skip >= total) return ([], total);
        var rows = await Project(query.OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id), false)
            .Skip((int)skip).Take(pageSize).ToListAsync();
        return (rows, total);
    }

    public Task<AuditLogReadModel?> GetAuditDetailAsync(int id)
        => Project(context.AuditLogs.AsNoTracking().Where(x => x.Id == id), true).SingleOrDefaultAsync();

    public async Task<(List<AuditAccessReadModel> Items, int TotalItems)> GetAuditAccessPageAsync(string[]? entities,
        int? userId, int? entityId, DateTime? fromUtc, DateTime? endUtcExclusive, int page, int pageSize)
    {
        var query = AccessQuery(entities, userId, entityId, fromUtc, endUtcExclusive);
        var total = await query.CountAsync();
        var skip = ((long)page - 1) * pageSize;
        if (skip >= total) return ([], total);
        var rows = await query.OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id)
            .Skip((int)skip).Take(pageSize).ToListAsync();
        return (rows, total);
    }

    public async Task<List<AuditLogReadModel>> GetAuditAccessEntriesAsync(int anchorId)
    {
        var source = AccessSource(context.AuditLogs.AsNoTracking());
        var anchor = await source.SingleOrDefaultAsync(x => x.Id == anchorId);
        if (anchor == null) return [];
        // Unknown/legacy metadata remains an individual original event.
        if (string.IsNullOrWhiteSpace(anchor.RequestId))
            return await Project(context.AuditLogs.AsNoTracking().Where(x => x.Id == anchorId), false).ToListAsync();

        // Exact persisted time is shared by the batch. It also prevents merging
        // requests from different app instances that happen to reuse a trace ID.
        var ids = source.Where(x => x.RequestId == anchor.RequestId && x.UserId == anchor.UserId
            && x.Action == anchor.Action && x.EntityName == anchor.EntityName && x.IpAddress == anchor.IpAddress
            && x.Succeeded == anchor.Succeeded && x.OccurredAt == anchor.OccurredAt).Select(x => x.Id);
        return await Project(context.AuditLogs.AsNoTracking().Where(x => ids.Contains(x.Id)), false)
            .OrderBy(x => x.EntityId).ThenBy(x => x.Id).ToListAsync();
    }

    private IQueryable<AuditAccessReadModel> AccessQuery(string[]? entities, int? userId, int? entityId,
        DateTime? fromUtc, DateTime? endUtcExclusive)
    {
        // Filter target IDs after grouping: a matching access still shows every
        // target returned by that request, not just the matching audit row.
        var source = AccessSource(FilterQuery(entities, userId, null, fromUtc, endUtcExclusive));
        var groups = source.GroupBy(x => new
        {
            x.RequestId, SingletonId = x.RequestId == null || x.RequestId.Trim() == "" ? (int?)x.Id : null,
            x.UserId, x.Action, x.EntityName, x.IpAddress, x.Succeeded, x.OccurredAt, x.IsUtcTimestamp
        });
        if (entityId.HasValue) groups = groups.Where(g => g.Any(x => x.EntityId == entityId.Value));
        var summaries = groups.Select(g => new
        {
            Id = g.Max(x => x.Id), g.Key.UserId, g.Key.Action, g.Key.EntityName, g.Key.IpAddress,
            g.Key.Succeeded, g.Key.OccurredAt, g.Key.IsUtcTimestamp, g.Key.RequestId,
            EntityId = g.Min(x => x.EntityId), EntryCount = g.Count(),
            EntityCount = g.Select(x => x.EntityId).Where(id => id.HasValue).Distinct().Count()
        });
        return from item in summaries
               join user in context.Users.IgnoreQueryFilters().AsNoTracking() on item.UserId equals (int?)user.Id into users
               from user in users.DefaultIfEmpty()
               select new AuditAccessReadModel
               {
                   Id = item.Id, UserId = item.UserId, Username = user == null ? null : user.Username,
                   FullName = user == null ? null : user.FullName, Action = item.Action, EntityName = item.EntityName,
                   IpAddress = item.IpAddress, Succeeded = item.Succeeded, OccurredAt = item.OccurredAt,
                   IsUtcTimestamp = item.IsUtcTimestamp, RequestId = item.RequestId,
                   EntityId = item.EntityCount == 1 ? item.EntityId : null,
                   EntryCount = item.EntryCount, EntityCount = item.EntityCount
               };
    }

    private static IQueryable<AuditAccessSourceReadModel> AccessSource(IQueryable<AuditLog> query)
        => query.Select(x => new AuditAccessSourceReadModel
        {
            Id = x.Id, UserId = x.UserId, Action = x.Action, EntityName = x.EntityName, EntityId = x.EntityId,
            IpAddress = x.IpAddress, Succeeded = x.Succeeded, OccurredAt = x.OccurredAt,
            IsUtcTimestamp = ClinicManagementDbContext.AuditJsonValue(
                ClinicManagementDbContext.AuditIsJson(x.Details) == 1 ? x.Details : "{}", "$.source") == "VC24_MEDICAL_ACCESS",
            RequestId = ClinicManagementDbContext.AuditJsonValue(
                ClinicManagementDbContext.AuditIsJson(x.Details) == 1 ? x.Details : "{}", "$.source") == "VC24_MEDICAL_ACCESS"
                ? ClinicManagementDbContext.AuditJsonValue(
                    ClinicManagementDbContext.AuditIsJson(x.Details) == 1 ? x.Details : "{}", "$.requestId") : null
        });

    public async Task<(List<AuditUserReadModel> Items, int TotalItems)> GetAuditUsersAsync(string? keyword, int page, int pageSize)
    {
        var query = UserQuery(keyword);
        var total = await query.CountAsync();
        var skip = ((long)page - 1) * pageSize;
        if (skip >= total) return ([], total);
        return (await query.OrderBy(x => x.Username).ThenBy(x => x.Id).Skip((int)skip).Take(pageSize).ToListAsync(), total);
    }

    private IQueryable<AuditLog> FilterQuery(string[]? entities, int? userId, int? entityId, DateTime? fromUtc, DateTime? endUtcExclusive)
    {
        var query = context.AuditLogs.AsNoTracking();
        if (entities is { Length: > 0 }) query = query.Where(x => entities.Contains(x.EntityName));
        if (userId.HasValue) query = query.Where(x => x.UserId == userId.Value);
        if (entityId.HasValue) query = query.Where(x => x.EntityId == entityId.Value);
        if (fromUtc.HasValue) query = query.Where(x => x.OccurredAt >= fromUtc.Value);
        if (endUtcExclusive.HasValue) query = query.Where(x => x.OccurredAt < endUtcExclusive.Value);
        return query;
    }

    private static IQueryable<AuditLogReadModel> Project(IQueryable<AuditLog> query, bool details)
        => query.Select(x => new AuditLogReadModel
        {
            Id = x.Id, UserId = x.UserId, Username = x.User == null ? null : x.User.Username,
            FullName = x.User == null ? null : x.User.FullName, Action = x.Action, EntityName = x.EntityName,
            EntityId = x.EntityId, IpAddress = x.IpAddress, Succeeded = x.Succeeded, OccurredAt = x.OccurredAt,
            Details = details ? x.Details : null,
            IsUtcTimestamp = x.Details != null && x.Details.Contains("\"source\":\"VC24_MEDICAL_ACCESS\"")
        });

    private IQueryable<AuditUserReadModel> UserQuery(string? keyword)
    {
        var userIds = context.AuditLogs.AsNoTracking().Where(x => x.UserId.HasValue).Select(x => x.UserId!.Value).Distinct();
        var query = from id in userIds
                    join user in context.Users.IgnoreQueryFilters().AsNoTracking() on id equals user.Id into users
                    from user in users.DefaultIfEmpty()
                    select new AuditUserReadModel { Id = id, Username = user == null ? null : user.Username, FullName = user == null ? null : user.FullName };
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.Trim();
            var numericId = int.TryParse(term, out var parsed) ? parsed : 0;
            query = query.Where(x => (x.Username != null && x.Username.Contains(term))
                || (x.FullName != null && x.FullName.Contains(term)) || x.Id == numericId);
        }
        return query;
    }
}
