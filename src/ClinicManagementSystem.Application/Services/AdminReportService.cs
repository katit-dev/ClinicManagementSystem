using System.ComponentModel.DataAnnotations;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Report;
using ClinicManagementSystem.Application.Enums;
using ClinicManagementSystem.Application.Helpers;
using ClinicManagementSystem.Infrastructure.Models.Reports;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Application.Services;

public interface IAdminReportService
{
    Task<HttpResponseData<ClinicReportDTO>> GetReportAsync(ClinicReportQueryDTO query);
    Task<HttpResponseData<ClinicReportExportDTO>> ExportAsync(ClinicReportQueryDTO query);
}

public class AdminReportService(IUnitOfWork unitOfWork, ILogger<AdminReportService> logger, TimeProvider? clock = null)
    : IAdminReportService
{
    public async Task<HttpResponseData<ClinicReportDTO>> GetReportAsync(ClinicReportQueryDTO query)
    {
        if (query == null) return Response<ClinicReportDTO>(400, "Vui lòng chọn khoảng ngày báo cáo.");
        var validation = new List<ValidationResult>();
        if (!Validator.TryValidateObject(query, new ValidationContext(query), validation, true))
            return Response<ClinicReportDTO>(400, string.Join(" ", validation.Select(v => v.ErrorMessage)));
        try
        {
            if (query.DoctorId.HasValue && !await unitOfWork.ClinicReportRepository.DoctorExistsAsync(query.DoctorId.Value))
                return Response<ClinicReportDTO>(404, "Không tìm thấy bác sĩ.");
            var from = query.From!.Value.ToDateTime(TimeOnly.MinValue);
            var endExclusive = query.To!.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            var data = await unitOfWork.ClinicReportRepository.GetDataAsync(from, endExclusive, query.DoctorId);
            return Response(200, "Tải báo cáo thành công.", BuildReport(query, data, from, endExclusive));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to build clinic report for {From} to {To}, doctor {DoctorId}.", query.From, query.To, query.DoctorId);
            return Response<ClinicReportDTO>(500, "Không thể tải báo cáo. Vui lòng thử lại.");
        }
    }

    public async Task<HttpResponseData<ClinicReportExportDTO>> ExportAsync(ClinicReportQueryDTO query)
    {
        var report = await GetReportAsync(query);
        if (report.StatusCode != 200 || report.Content == null)
            return Response<ClinicReportExportDTO>(report.StatusCode, report.Message);
        try
        {
            return Response(200, "Xuất báo cáo thành công.", new ClinicReportExportDTO
            {
                Bytes = ClinicReportExcelWriter.Create(report.Content),
                FileName = $"clinic-report_{report.Content.From:yyyy-MM-dd}_{report.Content.To:yyyy-MM-dd}"
                    + (query.DoctorId.HasValue ? $"_doctor-{query.DoctorId.Value}" : "_all-doctors") + ".xlsx"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to export clinic report.");
            return Response<ClinicReportExportDTO>(500, "Không thể xuất báo cáo. Vui lòng thử lại.");
        }
    }

    private ClinicReportDTO BuildReport(ClinicReportQueryDTO query, ClinicReportData data, DateTime from, DateTime endExclusive)
    {
        var report = new ClinicReportDTO
        {
            From = query.From!.Value, To = query.To!.Value, DoctorId = query.DoctorId,
            GeneratedAtUtc = (clock ?? TimeProvider.System).GetUtcNow().UtcDateTime
        };
        var doctors = new Dictionary<int, DoctorReportDTO>();
        var revenueItems = new Dictionary<(string Category, int? SourceId, string Description), RevenueItemReportDTO>();
        DoctorReportDTO Doctor(int? id, string name)
        {
            var key = id ?? 0;
            if (!doctors.TryGetValue(key, out var row))
                doctors[key] = row = new() { DoctorId = id, DoctorName = name };
            return row;
        }

        foreach (var visit in data.Visits)
        {
            var row = Doctor(visit.DoctorId, visit.DoctorName);
            row.TotalVisits += visit.CompletedCount;
            row.NoShowCount += visit.NoShowCount;
        }

        foreach (var invoice in data.Invoices)
        {
            var netRevenue = invoice.CollectedInPeriod - invoice.RefundedInPeriod;
            // Cohort debt: invoices issued in the selected period, with cash paid up to its end.
            // A later cancellation must not erase the debt from a historical period.
            var cancelledByEnd = invoice.Status == (byte)InvoiceStatus.Cancelled
                && (!invoice.CancelledAt.HasValue || invoice.CancelledAt.Value < endExclusive);
            var debt = invoice.CreatedAt >= from && invoice.CreatedAt < endExclusive && !cancelledByEnd
                ? Math.Max(0, invoice.TotalAmount + invoice.TaxAmount - invoice.DiscountAmount - invoice.InsuranceAmount - invoice.PaidThroughEnd)
                : 0;
            report.CollectedAmount += invoice.CollectedInPeriod;
            report.RefundedAmount += invoice.RefundedInPeriod;
            report.TotalRevenue += netRevenue;
            report.OutstandingDebt += debt;
            if (debt > 0) report.OutstandingInvoiceCount++;
            if (invoice.CollectedInPeriod == 0 && invoice.RefundedInPeriod == 0 && debt == 0) continue;

            var doctor = Doctor(invoice.DoctorId, invoice.DoctorName);
            doctor.TotalRevenue += netRevenue;
            doctor.OutstandingDebt += debt;
            foreach (var part in Allocate(invoice, netRevenue))
            {
                var key = (part.Category, part.SourceId, part.Description);
                if (!revenueItems.TryGetValue(key, out var item))
                    revenueItems[key] = item = new() { Category = part.Category, SourceId = part.SourceId, Description = part.Description };
                item.Revenue += part.Amount;
                switch (part.Category)
                {
                    case "Consultation": doctor.ConsultationRevenue += part.Amount; report.ConsultationRevenue += part.Amount; break;
                    case "Service": doctor.ServiceRevenue += part.Amount; report.ServiceRevenue += part.Amount; break;
                    case "Medicine": doctor.MedicineRevenue += part.Amount; report.MedicineRevenue += part.Amount; break;
                    default: doctor.OtherRevenue += part.Amount; report.OtherRevenue += part.Amount; break;
                }
            }
        }

        foreach (var doctor in doctors.Values)
        {
            doctor.ConcludedAppointments = doctor.TotalVisits + doctor.NoShowCount;
            doctor.NoShowRate = Rate(doctor.NoShowCount, doctor.ConcludedAppointments);
        }
        report.TotalVisits = doctors.Values.Sum(d => d.TotalVisits);
        report.NoShowCount = doctors.Values.Sum(d => d.NoShowCount);
        report.ConcludedAppointments = report.TotalVisits + report.NoShowCount;
        report.NoShowRate = Rate(report.NoShowCount, report.ConcludedAppointments);
        report.Doctors = doctors.Values.OrderBy(d => !d.DoctorId.HasValue).ThenBy(d => d.DoctorName).ThenBy(d => d.DoctorId).ToList();
        report.RevenueItems = revenueItems.Values.OrderBy(i => i.Category).ThenBy(i => i.Description).ThenBy(i => i.SourceId).ToList();
        return report;
    }

    private record RevenuePart(string Category, int? SourceId, string Description, decimal Amount);

    private static List<RevenuePart> Allocate(ClinicReportInvoiceData invoice, decimal netRevenue)
    {
        if (netRevenue == 0) return [];
        var basis = invoice.Items.Where(i => i.Amount > 0).Select(i => i.MedicineId.HasValue
            ? new RevenuePart("Medicine", i.MedicineId, i.Description ?? $"Thuốc #{i.MedicineId}", i.Amount)
            : i.ServiceId.HasValue
                ? new RevenuePart("Service", i.ServiceId, i.Description ?? $"Dịch vụ #{i.ServiceId}", i.Amount)
                : new RevenuePart("Other", null, i.Description ?? "Khoản khác", i.Amount)).ToList();
        // Finalization stores consultation fee in Invoice.TotalAmount, not an InvoiceItem.
        var remainder = Math.Max(0, invoice.TotalAmount - invoice.Items.Sum(i => i.Amount));
        if (remainder > 0)
            basis.Add(invoice.HasClinicalLink ? new("Consultation", null, "Phí khám", remainder) : new("Other", null, "Khoản khác", remainder));
        if (basis.Count == 0) basis.Add(new("Other", null, "Khoản khác", 1));
        var total = basis.Sum(p => p.Amount);
        var allocated = 0m;
        var result = new List<RevenuePart>();
        for (var index = 0; index < basis.Count; index++)
        {
            // Tax/discount/insurance affect the cash total and are shared proportionally.
            // Put the rounding residue in the final part so categories reconcile exactly.
            var amount = index == basis.Count - 1 ? netRevenue - allocated
                : Math.Round(netRevenue * basis[index].Amount / total, 2, MidpointRounding.AwayFromZero);
            allocated += amount;
            result.Add(basis[index] with { Amount = amount });
        }
        return result;
    }

    private static decimal Rate(int noShow, int concluded) => concluded == 0 ? 0 : Math.Round(noShow * 100m / concluded, 2, MidpointRounding.AwayFromZero);
    private static HttpResponseData<T> Response<T>(int status, string message, T? content = default)
        => new() { StatusCode = status, Message = message, Content = content };
}
