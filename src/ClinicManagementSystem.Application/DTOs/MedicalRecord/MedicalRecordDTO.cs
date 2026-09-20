namespace ClinicManagementSystem.Application.DTOs.MedicalRecord;


// =====================================================
// MEDICAL RECORD DTO
// =====================================================

public class MedicalRecordDTO
{
    // =================================================
    // MEDICAL RECORD
    // =================================================

    public int Id { get; set; }

    public int AppointmentId { get; set; }


    // =================================================
    // DOCTOR
    // =================================================

    public string DoctorName { get; set; }
        = string.Empty;


    // =================================================
    // CLINICAL INFORMATION
    // =================================================

    public string? Symptoms { get; set; }

    public string? Diagnosis { get; set; }

    public string? Icd10Code { get; set; }

    public string? TreatmentPlan { get; set; }

    public string? Note { get; set; }


    // =================================================
    // FOLLOW UP
    // =================================================

    public DateOnly? FollowUpDate { get; set; }


    // =================================================
    // STATUS
    // =================================================

    public byte Status { get; set; }

    public DateTime? FinalizedAt { get; set; }


    // =================================================
    // CREATED
    // =================================================

    public DateTime CreatedAt { get; set; }
}