namespace ClinicManagementSystem.Application.DTOs.MedicalRecord;

// =====================================================
// PATIENT VITAL REQUEST DTO
// =====================================================

public class PatientVitalRequestDTO
{
    // =====================================================
    // VITALS
    // =====================================================

    public decimal? Temperature { get; set; }

    public int? Pulse { get; set; }

    public string? BloodPressure { get; set; }

    public decimal? Weight { get; set; }

    public decimal? Height { get; set; }
}