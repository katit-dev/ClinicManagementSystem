using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IMedicalRecordRepository : IRepositoryBase<MedicalRecord>
{
    Task<(List<MedicalRecord> Items, int Total)> GetDoctorPageAsync(int doctorId, string? keyword,
        byte? status, DateOnly? from, DateOnly? to, int page, int pageSize);
}

public class MedicalRecordRepository : RepositoryBase<MedicalRecord>, IMedicalRecordRepository
{
    private readonly ClinicManagementDbContext _context;
    public MedicalRecordRepository(ClinicManagementDbContext context) : base(context) => _context = context;

    public async Task<(List<MedicalRecord> Items, int Total)> GetDoctorPageAsync(int doctorId, string? keyword,
        byte? status, DateOnly? from, DateOnly? to, int page, int pageSize)
    {
        var query = FilterDoctorRecords(_context.MedicalRecords.AsNoTracking(), doctorId, keyword, status, from, to);
        var total = await query.CountAsync();
        var skip = ((long)page - 1) * pageSize;
        var items = skip >= total ? [] : await query.Include(m => m.Patient).Include(m => m.Appointment)
            .Skip((int)skip).Take(pageSize).ToListAsync();
        return (items, total);
    }

    internal static IQueryable<MedicalRecord> FilterDoctorRecords(IQueryable<MedicalRecord> source, int doctorId,
        string? keyword, byte? status, DateOnly? from, DateOnly? to)
    {
        var query = source.Where(m => m.DoctorId == doctorId);
        if (status.HasValue) query = query.Where(m => m.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.Trim();
            var recordId = int.TryParse(term.TrimStart('#'), out var id) ? id : -1;
            query = query.Where(m => m.Id == recordId || m.Patient.PatientCode.Contains(term) || m.Patient.FullName.Contains(term));
        }
        if (from.HasValue)
        {
            var start = from.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(m => m.Appointment.StartTime >= start);
        }
        if (to.HasValue)
        {
            var end = to.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(m => m.Appointment.StartTime <= end);
        }
        return query.OrderByDescending(m => m.Appointment.StartTime).ThenByDescending(m => m.Id);
    }
}
