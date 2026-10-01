namespace ClinicManagementSystem.Application.DTOs.MedicalRecord;

// =====================================================
// MEDICAL RECORD DRAFT RESPONSE DTO
// =====================================================

public class MedicalRecordDraftResponseDTO
{
    // =====================================================
    // MEDICAL RECORD
    // =====================================================

    public int Id { get; set; }

    public int AppointmentId { get; set; }

    public int PatientId { get; set; }

    public string DoctorName { get; set; } = string.Empty;

    public string? Symptoms { get; set; }

    public string? Diagnosis { get; set; }

    public string? Icd10Code { get; set; }

    public string? TreatmentPlan { get; set; }

    public string? Note { get; set; }

    public DateOnly? FollowUpDate { get; set; }

    public byte Status { get; set; }

    public DateTime? FinalizedAt { get; set; }

    public DateTime CreatedAt { get; set; }


    // =====================================================
    // PATIENT VITALS
    // =====================================================

    public PatientVitalResponseDTO? Vitals { get; set; }
}