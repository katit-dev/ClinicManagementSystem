namespace ClinicManagementSystem.Application.DTOs.Pharmacy;

// =====================================================
// PHARMACY PRESCRIPTION BATCH DTO
// =====================================================
//
// Lô thuốc dùng cho màn hình Pharmacy.
//
// Các lô được trả theo FEFO:
// expiry_date tăng dần.
// =====================================================

public class PharmacyPrescriptionBatchDTO
{
    public int BatchId { get; set; }

    public string BatchNo { get; set; } = string.Empty;

    public DateOnly ExpiryDate { get; set; }

    public int Quantity { get; set; }
}