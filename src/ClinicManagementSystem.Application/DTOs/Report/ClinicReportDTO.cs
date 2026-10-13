namespace ClinicManagementSystem.Application.DTOs.Report;

public class ClinicReportDTO
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public int? DoctorId { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public int TotalVisits { get; set; }
    public int NoShowCount { get; set; }
    public int ConcludedAppointments { get; set; }
    public decimal NoShowRate { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal RefundedAmount { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal OutstandingDebt { get; set; }
    public int OutstandingInvoiceCount { get; set; }
    public decimal ConsultationRevenue { get; set; }
    public decimal ServiceRevenue { get; set; }
    public decimal MedicineRevenue { get; set; }
    public decimal OtherRevenue { get; set; }
    public List<DoctorReportDTO> Doctors { get; set; } = [];
    public List<RevenueItemReportDTO> RevenueItems { get; set; } = [];
}

public class DoctorReportDTO
{
    // null is the explicit bucket for invoices without a linked doctor.
    public int? DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public int TotalVisits { get; set; }
    public int NoShowCount { get; set; }
    public int ConcludedAppointments { get; set; }
    public decimal NoShowRate { get; set; }
    public decimal ConsultationRevenue { get; set; }
    public decimal ServiceRevenue { get; set; }
    public decimal MedicineRevenue { get; set; }
    public decimal OtherRevenue { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal OutstandingDebt { get; set; }
}

public class RevenueItemReportDTO
{
    public string Category { get; set; } = string.Empty;
    public int? SourceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
}

public class ClinicReportExportDTO
{
    public byte[] Bytes { get; set; } = [];
    public string FileName { get; set; } = string.Empty;
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}
