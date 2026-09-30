using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Application.DTOs.Prescription;

// =====================================================
// PRESCRIPTION ITEM REQUEST DTO
// =====================================================

public class PrescriptionItemRequestDTO
{
    // =================================================
    // MEDICINE
    // =================================================

    [Required]
    public int MedicineId { get; set; }


    // =================================================
    // QUANTITY
    // =================================================

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }


    // =================================================
    // DOSAGE
    // =================================================

    [Required]
    public string Dosage { get; set; } = string.Empty;


    // =================================================
    // INSTRUCTION
    // =================================================

    public string? Instruction { get; set; }


    // =================================================
    // DURATION
    // =================================================

    public int? DurationDays { get; set; }


    // =================================================
    // ALLERGY
    // =================================================

    public bool AllergyConfirmed { get; set; }
}