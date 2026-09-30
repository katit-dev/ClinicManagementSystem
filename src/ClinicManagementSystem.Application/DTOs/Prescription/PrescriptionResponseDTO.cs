namespace ClinicManagementSystem.Application.DTOs.Prescription;

// =====================================================
// PRESCRIPTION RESPONSE DTO
// =====================================================

public class PrescriptionResponseDTO
{
    // =================================================
    // PRESCRIPTION
    // =================================================

    public int Id { get; set; }

    public int MedicalRecordId { get; set; }


    // =================================================
    // NOTE
    // =================================================

    public string? Note { get; set; }


    // =================================================
    // STATUS
    // =================================================

    public byte Status { get; set; }


    // =================================================
    // CREATED
    // =================================================

    public DateTime CreatedAt { get; set; }


    // =================================================
    // DISPENSED
    // =================================================

    public DateTime? DispensedAt { get; set; }


    // =================================================
    // ITEMS
    // =================================================

    public List<PrescriptionItemResponseDTO> Items { get; set; }
        = new();
}