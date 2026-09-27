using ClinicManagementSystem.Application.Enums;
using ClinicManagementSystem.Application.DTOs.Payment;

namespace ClinicManagementSystem.Application.DTOs.Invoice;

public class InvoiceDTO
{
    // =====================================================
    // INVOICE
    // =====================================================

    public int Id { get; set; }

    public string InvoiceNo { get; set; }
        = string.Empty;


    // =====================================================
    // PATIENT
    // =====================================================

    public int PatientId { get; set; }

    public string? PatientName { get; set; }


    // =====================================================
    // REFERENCES
    // =====================================================

    public int? AppointmentId { get; set; }

    public int? MedicalRecordId { get; set; }


    // =====================================================
    // AMOUNTS
    // =====================================================

    public decimal TotalAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal InsuranceAmount { get; set; }

    public decimal PaidAmount { get; set; }


    // =====================================================
    // REMAINING
    // =====================================================

    public decimal RemainingAmount { get; set; }


    // =====================================================
    // STATUS
    // =====================================================

    public byte Status { get; set; }


    // =====================================================
    // TIME
    // =====================================================

    public DateTime CreatedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? CancelReason { get; set; }


    // =====================================================
    // ITEMS
    // =====================================================

    public List<InvoiceItemDTO> Items { get; set; }
        = new();


    // =====================================================
    // PAYMENTS
    // =====================================================

    public List<PaymentDTO> Payments { get; set; }
        = new();
}