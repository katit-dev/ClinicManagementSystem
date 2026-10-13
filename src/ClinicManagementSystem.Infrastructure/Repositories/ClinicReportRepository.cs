using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.Models.Reports;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IClinicReportRepository : IRepositoryBase<Invoice>
{
    Task<bool> DoctorExistsAsync(int doctorId);
    Task<ClinicReportData> GetDataAsync(DateTime from, DateTime endExclusive, int? doctorId);
}

public class ClinicReportRepository : RepositoryBase<Invoice>, IClinicReportRepository
{
    private readonly ClinicManagementDbContext context;
    public ClinicReportRepository(ClinicManagementDbContext context) : base(context) => this.context = context;

    public Task<bool> DoctorExistsAsync(int doctorId)
        => context.Doctors.AsNoTracking().AnyAsync(d => d.Id == doctorId);

    public async Task<ClinicReportData> GetDataAsync(DateTime from, DateTime endExclusive, int? doctorId)
    {
        // Group appointments separately: joining them to payments/items would multiply visits.
        var visits = await VisitQuery(from, endExclusive, doctorId).ToListAsync();
        var invoices = await InvoiceQuery(from, endExclusive, doctorId).ToListAsync();
        return new() { Visits = visits, Invoices = invoices };
    }

    private IQueryable<ClinicReportVisitData> VisitQuery(DateTime from, DateTime endExclusive, int? doctorId)
        => context.Appointments.AsNoTracking()
            .Where(a => a.StartTime >= from && a.StartTime < endExclusive
                && (!doctorId.HasValue || a.DoctorId == doctorId.Value)
                && (a.Status == 3 || a.Status == 5)) // Completed / NoShow
            .GroupBy(a => new { a.DoctorId, a.Doctor.FullName })
            .Select(g => new ClinicReportVisitData
            {
                DoctorId = g.Key.DoctorId, DoctorName = g.Key.FullName,
                CompletedCount = g.Count(a => a.Status == 3), NoShowCount = g.Count(a => a.Status == 5)
            });

    private IQueryable<ClinicReportInvoiceData> InvoiceQuery(DateTime from, DateTime endExclusive, int? doctorId)
        => context.Invoices.AsNoTracking()
            .Where(i => (i.CreatedAt >= from && i.CreatedAt < endExclusive)
                || i.Payments.Any(p => p.PaidAt >= from && p.PaidAt < endExclusive))
            .Where(i => !doctorId.HasValue
                || (i.MedicalRecord != null ? (int?)i.MedicalRecord.DoctorId
                    : i.Appointment != null ? (int?)i.Appointment.DoctorId : null) == doctorId.Value)
            .OrderBy(i => i.Id)
            .Select(i => new ClinicReportInvoiceData
            {
                Id = i.Id,
                DoctorId = i.MedicalRecord != null ? (int?)i.MedicalRecord.DoctorId
                    : i.Appointment != null ? (int?)i.Appointment.DoctorId : null,
                DoctorName = i.MedicalRecord != null ? i.MedicalRecord.Doctor.FullName
                    : i.Appointment != null ? i.Appointment.Doctor.FullName : "Chưa gắn bác sĩ",
                HasClinicalLink = i.MedicalRecordId.HasValue || i.AppointmentId.HasValue,
                CreatedAt = i.CreatedAt, Status = i.Status, CancelledAt = i.CancelledAt,
                TotalAmount = i.TotalAmount, DiscountAmount = i.DiscountAmount,
                TaxAmount = i.TaxAmount, InsuranceAmount = i.InsuranceAmount,
                CollectedInPeriod = i.Payments.Where(p => p.PaidAt >= from && p.PaidAt < endExclusive && !p.IsRefund && p.Amount > 0)
                    .Sum(p => (decimal?)p.Amount) ?? 0,
                // Existing InvoiceService stores refunds as negative amounts. Abs also supports legacy positive refund rows.
                RefundedInPeriod = i.Payments.Where(p => p.PaidAt >= from && p.PaidAt < endExclusive && (p.IsRefund || p.Amount < 0))
                    .Sum(p => (decimal?)Math.Abs(p.Amount)) ?? 0,
                PaidThroughEnd = i.Payments.Where(p => p.PaidAt < endExclusive)
                    .Sum(p => (decimal?)(p.IsRefund ? -Math.Abs(p.Amount) : p.Amount)) ?? 0,
                Items = i.InvoiceItems.OrderBy(item => item.Id).Select(item => new ClinicReportInvoiceItemData
                {
                    Id = item.Id, MedicineId = item.MedicineId,
                    ServiceId = item.MedicalRecordService != null ? (int?)item.MedicalRecordService.ServiceId : null,
                    Description = item.Description, Amount = item.Amount
                }).ToList()
            });
}
