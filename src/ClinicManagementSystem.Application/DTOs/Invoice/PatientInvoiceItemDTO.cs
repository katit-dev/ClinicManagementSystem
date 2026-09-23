namespace ClinicManagementSystem.Application.DTOs.Invoice;


// =====================================================
// PATIENT INVOICE ITEM DTO
// =====================================================

public class PatientInvoiceItemDTO
{
    public int Id { get; set; }

    public string? Description { get; set; }

    public int? MedicineId { get; set; }

    public int? MedicalRecordServiceId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal Amount { get; set; }
}