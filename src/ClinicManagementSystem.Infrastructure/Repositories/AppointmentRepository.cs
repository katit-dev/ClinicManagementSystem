using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IAppointmentRepository : IRepositoryBase<Appointment>
{
    Task<List<Appointment>> GetUpcomingInRangeAsync(int? doctorId, DateTime start, DateTime end);
    Task<List<Appointment>> GetBlockingInRangeAsync(int doctorId, DateTime start, DateTime end);
}

public class AppointmentRepository : RepositoryBase<Appointment>, IAppointmentRepository
{
    private readonly ClinicManagementDbContext _context;
    public AppointmentRepository(ClinicManagementDbContext context)
        : base(context)
    {
        _context = context;
    }

    public Task<List<Appointment>> GetUpcomingInRangeAsync(int? doctorId, DateTime start, DateTime end)
        => _context.Appointments.AsNoTracking().Include(a => a.Doctor).Include(a => a.Patient)
            .Where(a => (!doctorId.HasValue || a.DoctorId == doctorId) && a.Status < 4 && a.EndTime > start && a.StartTime < end)
            .OrderBy(a => a.StartTime).ThenBy(a => a.Id).ToListAsync();

    public Task<List<Appointment>> GetBlockingInRangeAsync(int doctorId, DateTime start, DateTime end)
        => _context.Appointments.AsNoTracking().Where(a => a.DoctorId == doctorId && a.Status < 4 && a.StartTime < end && a.EndTime > start).ToListAsync();
}
