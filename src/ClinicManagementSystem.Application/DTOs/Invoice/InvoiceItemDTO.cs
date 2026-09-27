namespace ClinicManagementSystem.Application.DTOs.Invoice;

public class InvoiceItemDTO
{
    // =====================================================
    // ITEM
    // =====================================================

    public int Id { get; set; }

    public int InvoiceId { get; set; }


    // =====================================================
    // SOURCE
    // =====================================================

    public int? MedicineId { get; set; }

    public int? MedicalRecordServiceId { get; set; }


    // =====================================================
    // DESCRIPTION
    // =====================================================

    public string? Description { get; set; }


    // =====================================================
    // QUANTITY
    // =====================================================

    public int Quantity { get; set; }


    // =====================================================
    // PRICE
    // =====================================================

    public decimal UnitPrice { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal Amount { get; set; }
}