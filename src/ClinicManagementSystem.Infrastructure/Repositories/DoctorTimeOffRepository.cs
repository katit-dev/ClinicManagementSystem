using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IDoctorTimeOffRepository : IRepositoryBase<DoctorTimeOff>
{
    Task<(List<DoctorTimeOff> Items, int Total)> GetAdminPageAsync(int? doctorId, bool clinicWideOnly, DateTime? from, DateTime? toExclusive, int page, int pageSize);
    Task<bool> HasOverlapAsync(int? doctorId, DateTime start, DateTime end);
    Task<List<DoctorTimeOff>> GetOverlappingAsync(int doctorId, DateTime start, DateTime end);
    Task<(List<DoctorTimeOff> Items, int Total)> GetDoctorPageAsync(int doctorId, DateTime? from, DateTime? toExclusive,
        byte? scope, byte? type, int page, int pageSize);
}

public class DoctorTimeOffRepository : RepositoryBase<DoctorTimeOff>, IDoctorTimeOffRepository
{
    private readonly ClinicManagementDbContext _context;
    public DoctorTimeOffRepository(ClinicManagementDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<(List<DoctorTimeOff> Items, int Total)> GetAdminPageAsync(int? doctorId, bool clinicWideOnly, DateTime? from, DateTime? toExclusive, int page, int pageSize)
    {
        var query = _context.DoctorTimeOffs.AsNoTracking().Include(t => t.Doctor).AsQueryable();
        if (clinicWideOnly) query = query.Where(t => t.DoctorId == null);
        else if (doctorId.HasValue) query = query.Where(t => t.DoctorId == doctorId || t.DoctorId == null);
        if (from.HasValue) query = query.Where(t => t.EndAt > from);
        if (toExclusive.HasValue) query = query.Where(t => t.StartAt < toExclusive);
        var total = await query.CountAsync();
        var skip = ((long)page - 1) * pageSize;
        var items = skip >= total ? new List<DoctorTimeOff>() : await query.OrderByDescending(t => t.StartAt).ThenByDescending(t => t.Id)
            .Skip((int)skip).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public async Task<(List<DoctorTimeOff> Items, int Total)> GetDoctorPageAsync(int doctorId, DateTime? from, DateTime? toExclusive,
        byte? scope, byte? type, int page, int pageSize)
    {
        var query = FilterDoctorTimeOff(_context.DoctorTimeOffs.AsNoTracking(), doctorId, from, toExclusive, scope, type);
        var total = await query.CountAsync();
        var skip = ((long)page - 1) * pageSize;
        var items = skip >= total ? new List<DoctorTimeOff>() : await query.Skip((int)skip).Take(pageSize).ToListAsync();
        return (items, total);
    }

    internal static IOrderedQueryable<DoctorTimeOff> FilterDoctorTimeOff(IQueryable<DoctorTimeOff> query, int doctorId,
        DateTime? from, DateTime? toExclusive, byte? scope, byte? type)
    {
        query = query.Where(t => t.DoctorId == doctorId || t.DoctorId == null);
        if (scope == 0) query = query.Where(t => t.DoctorId == doctorId);
        if (scope == 1) query = query.Where(t => t.DoctorId == null);
        if (type.HasValue) query = query.Where(t => t.Type == type);
        if (from.HasValue) query = query.Where(t => t.EndAt > from);
        if (toExclusive.HasValue) query = query.Where(t => t.StartAt < toExclusive);
        return query.OrderByDescending(t => t.StartAt).ThenByDescending(t => t.Id);
    }

    public Task<bool> HasOverlapAsync(int? doctorId, DateTime start, DateTime end)
        => _context.DoctorTimeOffs.AnyAsync(t => (doctorId == null || t.DoctorId == null || t.DoctorId == doctorId)
            && t.StartAt < end && t.EndAt > start);

    public Task<List<DoctorTimeOff>> GetOverlappingAsync(int doctorId, DateTime start, DateTime end)
        => _context.DoctorTimeOffs.AsNoTracking().Where(t => (t.DoctorId == doctorId || t.DoctorId == null)
            && t.StartAt < end && t.EndAt > start).ToListAsync();
}
