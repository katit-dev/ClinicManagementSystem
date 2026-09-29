namespace ClinicManagementSystem.Application.DTOs.MedicalRecord;

// =====================================================
// MEDICAL RECORD DRAFT REQUEST DTO
// =====================================================

public class MedicalRecordDraftRequestDTO
{
    // =====================================================
    // CLINICAL INFORMATION
    // =====================================================

    public string? Symptoms { get; set; }

    public string? Diagnosis { get; set; }

    public string? Icd10Code { get; set; }

    public string? TreatmentPlan { get; set; }

    public string? Note { get; set; }


    // =====================================================
    // FOLLOW UP
    // =====================================================

    public DateOnly? FollowUpDate { get; set; }


    // =====================================================
    // PATIENT VITALS
    // =====================================================

    public PatientVitalRequestDTO? Vitals { get; set; }
}