namespace ClinicManagementSystem.Application.DTOs.Invoice;


// =====================================================
// PATIENT INVOICE DTO
// =====================================================

public class PatientInvoiceDTO
{

    public int Id { get; set; }

    public string InvoiceNo { get; set; } = string.Empty;

    public int? AppointmentId { get; set; }

    public int? MedicalRecordId { get; set; }

    public string? PatientName { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal InsuranceAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public byte Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? CancelReason { get; set; }

    public List<PatientInvoiceItemDTO> Items { get; set; } = new();

    public List<PatientPaymentDTO> Payments { get; set; } = new();
}