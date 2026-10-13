
using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IUserRepository : IRepositoryBase<User>
{
    Task<(List<User> Items, int TotalItems)> GetAdminPageAsync(string? keyword, string? role, bool? active, int page, int pageSize);
    Task<User?> GetAdminDetailAsync(int id, bool tracking = false);
    Task<int> CountActiveAdminsAsync();
    Task AcquireAccessLockAsync();
}

public class UserRepository
    : RepositoryBase<User>, IUserRepository
{
    private readonly ClinicManagementDbContext _context;
    public UserRepository(ClinicManagementDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<(List<User> Items, int TotalItems)> GetAdminPageAsync(string? keyword, string? role, bool? active, int page, int pageSize)
    {
        var query = _context.Users.AsNoTracking().Where(u => !u.IsDeleted);
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(u => u.Username.Contains(keyword) || (u.FullName != null && u.FullName.Contains(keyword))
                || (u.Email != null && u.Email.Contains(keyword)) || (u.Phone != null && u.Phone.Contains(keyword)));
        if (!string.IsNullOrWhiteSpace(role)) query = query.Where(u => u.UserRoleUsers.Any(r => r.Role.Name == role));
        if (active.HasValue) query = query.Where(u => u.IsActive == active.Value);
        var total = await query.CountAsync();
        var skip = ((long)page - 1) * pageSize;
        var items = skip >= total ? new List<User>() : await query.Include(u => u.UserRoleUsers).ThenInclude(r => r.Role)
            .OrderBy(u => u.Username).ThenBy(u => u.Id).Skip((int)skip).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public Task<User?> GetAdminDetailAsync(int id, bool tracking = false)
    {
        var query = _context.Users.Include(u => u.UserRoleUsers).ThenInclude(r => r.Role)
            .Include(u => u.Doctor).Include(u => u.Patient).AsQueryable();
        if (!tracking) query = query.AsNoTracking();
        return query.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
    }

    public Task<int> CountActiveAdminsAsync() => _context.Users.CountAsync(u => u.IsActive && !u.IsDeleted
        && u.UserRoleUsers.Any(r => r.Role.Name == "Admin"));

    // Shared by administrative mutations and session issuance to prevent stale refresh-token races.
    public Task AcquireAccessLockAsync() => _context.Database.ExecuteSqlRawAsync("""
        DECLARE @result int;
        EXEC @result = sys.sp_getapplock @Resource = N'ClinicManagement:UserAccess',
            @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
        IF @result < 0 THROW 50022, 'Could not acquire the user access lock.', 1;
        """);
}
