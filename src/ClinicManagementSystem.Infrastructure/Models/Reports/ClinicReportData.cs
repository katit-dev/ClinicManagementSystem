namespace ClinicManagementSystem.Infrastructure.Models.Reports;

// Read projections only; these classes are not EF entities or database tables.
public class ClinicReportData
{
    public List<ClinicReportVisitData> Visits { get; set; } = [];
    public List<ClinicReportInvoiceData> Invoices { get; set; } = [];
}

public class ClinicReportVisitData
{
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public int CompletedCount { get; set; }
    public int NoShowCount { get; set; }
}

public class ClinicReportInvoiceData
{
    public int Id { get; set; }
    public int? DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public bool HasClinicalLink { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte Status { get; set; }
    public DateTime? CancelledAt { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal InsuranceAmount { get; set; }
    public decimal CollectedInPeriod { get; set; }
    public decimal RefundedInPeriod { get; set; }
    public decimal PaidThroughEnd { get; set; }
    public List<ClinicReportInvoiceItemData> Items { get; set; } = [];
}

public class ClinicReportInvoiceItemData
{
    public int Id { get; set; }
    public int? MedicineId { get; set; }
    public int? ServiceId { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }
}
