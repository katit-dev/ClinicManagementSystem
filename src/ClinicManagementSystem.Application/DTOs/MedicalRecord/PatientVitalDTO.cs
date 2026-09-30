namespace ClinicManagementSystem.Application.DTOs.MedicalRecord;

// =====================================================
// PATIENT VITAL DTO
// =====================================================

public class PatientVitalDTO
{
    public int Id { get; set; }

    public int MedicalRecordId { get; set; }

    public decimal? Temperature { get; set; }

    public int? Pulse { get; set; }

    public string? BloodPressure { get; set; }

    public decimal? Weight { get; set; }

    public decimal? Height { get; set; }
}