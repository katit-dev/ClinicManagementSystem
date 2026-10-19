using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IAppointmentRepository : IRepositoryBase<Appointment>
{
    Task<byte?> GetMedicalRecordAppointmentStatusAsync(int appointmentId);
    Task<List<Appointment>> GetUpcomingInRangeAsync(int? doctorId, DateTime start, DateTime end);
    Task<List<Appointment>> GetBlockingInRangeAsync(int doctorId, DateTime start, DateTime end);
    Task<Appointment?> GetReceptionDetailAsync(int id);
}

public class AppointmentRepository : RepositoryBase<Appointment>, IAppointmentRepository
{
    public Task<byte?> GetMedicalRecordAppointmentStatusAsync(int appointmentId)
        => _context.Appointments.AsNoTracking().Where(a => a.Id == appointmentId)
            .Select(a => (byte?)a.Status).FirstOrDefaultAsync();

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

    public Task<Appointment?> GetReceptionDetailAsync(int id)
        => _context.Appointments.Include(a => a.Doctor).Include(a => a.Patient).Include(a => a.Invoices).Include(a => a.Notifications)
            .SingleOrDefaultAsync(a => a.Id == id);
}
