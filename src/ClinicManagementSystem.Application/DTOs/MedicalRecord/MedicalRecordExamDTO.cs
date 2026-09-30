namespace ClinicManagementSystem.Application.DTOs.MedicalRecord;

// =====================================================
// MEDICAL RECORD EXAM DTO
//
// Dùng cho màn hình Doctor Exam.
//
// Bao gồm:
// - Medical Record
// - Patient Vital
// =====================================================

public class MedicalRecordExamDTO
{
    public int Id { get; set; }

    public int AppointmentId { get; set; }

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

    // =================================================
    // PATIENT VITAL
    // =================================================

    public PatientVitalDTO? Vital { get; set; }

    public List<MedicalRecordServiceDTO> Services { get; set; } = new(); // for vc 14 only
}