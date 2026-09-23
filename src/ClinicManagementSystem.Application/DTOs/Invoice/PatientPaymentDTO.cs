namespace ClinicManagementSystem.Application.DTOs.Invoice;


// =====================================================
// PATIENT PAYMENT DTO
// =====================================================

public class PatientPaymentDTO
{
    public int Id { get; set; }

    public decimal Amount { get; set; }

    public byte Method { get; set; }

    public DateTime PaidAt { get; set; }

    public string? ReferenceCode { get; set; }

    public string? Note { get; set; }

    public bool IsRefund { get; set; }
}