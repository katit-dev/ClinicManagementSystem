using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IServiceRepository : IRepositoryBase<Service>
{
    Task<(List<Service> Items, int TotalItems)> GetAdminPageAsync(string? keyword, bool? active, int? specialtyId, int page, int pageSize);
    Task<Service?> GetAdminDetailAsync(int id, bool tracking = false);
    Task<bool> CodeExistsAsync(string code, int? excludedId);
    Task AcquireAdminLockAsync();
}

public class ServiceRepository : RepositoryBase<Service>, IServiceRepository
{
    private readonly ClinicManagementDbContext _context;
    public ServiceRepository(ClinicManagementDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<(List<Service> Items, int TotalItems)> GetAdminPageAsync(string? keyword, bool? active, int? specialtyId, int page, int pageSize)
    {
        var query = _context.Services.AsNoTracking().Include(s => s.Specialty).AsQueryable();
        if (active.HasValue) query = query.Where(s => s.IsActive == active.Value);
        if (specialtyId.HasValue) query = query.Where(s => s.SpecialtyId == specialtyId.Value);
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(s => s.Name.Contains(keyword) || (s.Code != null && s.Code.Contains(keyword)));
        var total = await query.CountAsync();
        var skip = ((long)page - 1) * pageSize;
        var items = skip >= total ? new List<Service>() : await query.OrderBy(s => s.Name).ThenBy(s => s.Id)
            .Skip((int)skip).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public Task<Service?> GetAdminDetailAsync(int id, bool tracking = false)
    {
        var query = _context.Services.Include(s => s.Specialty).AsQueryable();
        if (!tracking) query = query.AsNoTracking();
        return query.FirstOrDefaultAsync(s => s.Id == id);
    }

    public Task<bool> CodeExistsAsync(string code, int? excludedId)
        => _context.Services.AnyAsync(s => s.Code != null && s.Code.ToUpper() == code
            && (!excludedId.HasValue || s.Id != excludedId.Value));

    public async Task AcquireAdminLockAsync()
    {
        // Serialize catalog mutations without adding columns or changing the schema.
        await _context.Database.ExecuteSqlRawAsync("""
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock @Resource = N'ClinicManagement:ServiceCatalog',
                @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
            IF @result < 0 THROW 50021, 'Could not acquire the service catalog lock.', 1;
            """);
    }
}
