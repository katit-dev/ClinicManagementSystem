namespace ClinicManagementSystem.Application.DTOs.MedicalRecord;

// =====================================================
// PATIENT VITAL RESPONSE DTO
// =====================================================

public class PatientVitalResponseDTO
{
    // =====================================================
    // VITAL
    // =====================================================

    public int Id { get; set; }

    public int MedicalRecordId { get; set; }

    public decimal? Temperature { get; set; }

    public int? Pulse { get; set; }

    public string? BloodPressure { get; set; }

    public decimal? Weight { get; set; }

    public decimal? Height { get; set; }
}