using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IDoctorScheduleRepository : IRepositoryBase<DoctorSchedule>
{
    Task<(List<DoctorSchedule> Items, int Total)> GetAdminPageAsync(int? doctorId, DateOnly? date, bool? active, int page, int pageSize);
    Task<List<DoctorSchedule>> GetForDoctorDayAsync(int doctorId, byte dayOfWeek);
    Task<List<DoctorSchedule>> GetEffectiveAsync(int doctorId, DateOnly date);
    Task<List<DoctorSchedule>> GetInRangeAsync(int doctorId, DateOnly from, DateOnly to);
    Task AcquireAdminLockAsync();
}

public class DoctorScheduleRepository : RepositoryBase<DoctorSchedule>, IDoctorScheduleRepository
{
    private readonly ClinicManagementDbContext _context;
    public DoctorScheduleRepository(ClinicManagementDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<(List<DoctorSchedule> Items, int Total)> GetAdminPageAsync(int? doctorId, DateOnly? date, bool? active, int page, int pageSize)
    {
        var query = _context.DoctorSchedules.AsNoTracking().Include(s => s.Doctor).AsQueryable();
        if (doctorId.HasValue) query = query.Where(s => s.DoctorId == doctorId);
        if (active.HasValue) query = query.Where(s => s.IsActive == active);
        if (date.HasValue) query = query.Where(s => s.DayOfWeek == (byte)date.Value.DayOfWeek && s.EffectiveFrom <= date && (s.EffectiveTo == null || s.EffectiveTo >= date));
        var total = await query.CountAsync();
        var skip = ((long)page - 1) * pageSize;
        var items = skip >= total ? new List<DoctorSchedule>() : await query.OrderBy(s => s.Doctor.FullName)
            .ThenBy(s => s.DoctorId).ThenBy(s => s.DayOfWeek).ThenBy(s => s.StartTime).ThenByDescending(s => s.EffectiveFrom)
            .ThenBy(s => s.Id).Skip((int)skip).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public Task<List<DoctorSchedule>> GetForDoctorDayAsync(int doctorId, byte dayOfWeek)
        => _context.DoctorSchedules.AsNoTracking().Where(s => s.DoctorId == doctorId && s.DayOfWeek == dayOfWeek).ToListAsync();

    public Task<List<DoctorSchedule>> GetEffectiveAsync(int doctorId, DateOnly date)
        => _context.DoctorSchedules.AsNoTracking().Where(s => s.DoctorId == doctorId && s.DayOfWeek == (byte)date.DayOfWeek
            && s.IsActive && s.EffectiveFrom <= date && (s.EffectiveTo == null || s.EffectiveTo >= date))
            .OrderBy(s => s.StartTime).ToListAsync();

    public Task<List<DoctorSchedule>> GetInRangeAsync(int doctorId, DateOnly from, DateOnly to)
        => _context.DoctorSchedules.AsNoTracking().Where(s => s.DoctorId == doctorId && s.IsActive
            && s.EffectiveFrom <= to && (s.EffectiveTo == null || s.EffectiveTo >= from))
            .OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime).ThenByDescending(s => s.EffectiveFrom).ThenBy(s => s.Id).ToListAsync();

    public async Task AcquireAdminLockAsync()
    {
        // Transaction-scoped lock serializes admin overlap checks and writes without schema changes.
        await _context.Database.ExecuteSqlRawAsync("""
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock @Resource = N'ClinicManagement:AdminScheduling',
                @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
            IF @result < 0 THROW 50020, 'Could not acquire the scheduling administration lock.', 1;
            """);
    }
}
