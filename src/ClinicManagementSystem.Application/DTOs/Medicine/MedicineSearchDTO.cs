namespace ClinicManagementSystem.Application.DTOs.Medicine;

// =====================================================
// MEDICINE SEARCH RESPONSE DTO
// =====================================================

public class MedicineSearchDTO
{
    // =================================================
    // MEDICINE
    // =================================================

    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Unit { get; set; }

    public decimal Price { get; set; }

    public string? ActiveIngredient { get; set; }

    public string? Concentration { get; set; }


    // =================================================
    // AVAILABLE STOCK
    // =================================================

    public int AvailableQuantity { get; set; }


    // =================================================
    // BATCH
    //
    // Hiển thị lô gần hết hạn nhất còn hàng.
    // =================================================

    public string? BatchNo { get; set; }

    public DateOnly? ExpiryDate { get; set; }


    // =================================================
    // ALLERGY
    // =================================================

    public bool HasAllergyWarning { get; set; }

    public string? AllergyWarning { get; set; }
}