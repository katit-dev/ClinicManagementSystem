namespace ClinicManagementSystem.Application.DTOs.Pharmacy;

// =====================================================
// PHARMACY PRESCRIPTION DETAIL ITEM DTO
// =====================================================
//
// Một thuốc trong đơn thuốc.
//
// Dùng cho:
// GET /api/pharmacy/prescriptions/{id}
// =====================================================

public class PharmacyPrescriptionDetailItemDTO
{
    // =================================================
    // PRESCRIPTION ITEM
    // =================================================

    public int PrescriptionItemId { get; set; }

    public int MedicineId { get; set; }

    public string MedicineName { get; set; } = string.Empty;

    public int Quantity { get; set; }


    // =================================================
    // DOSAGE
    // =================================================

    public string Dosage { get; set; } = string.Empty;

    public string? Instruction { get; set; }

    public int? DurationDays { get; set; }

    public string? Frequency { get; set; }


    // =================================================
    // STOCK
    // =================================================

    public int AvailableQuantity { get; set; }


    // =================================================
    // FEFO BATCHES
    // =================================================

    public List<PharmacyPrescriptionBatchDTO> Batches { get; set; }
        = new();
}