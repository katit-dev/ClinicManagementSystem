namespace ClinicManagementSystem.Application.DTOs.PatientRecord;


// =====================================================
// PATIENT RECORD DTO
// =====================================================

public class PatientRecordDTO
{
    // =================================================
    // MEDICAL RECORD
    // =================================================

    public int Id { get; set; }

    public int AppointmentId { get; set; }

    public string DoctorName { get; set; } = string.Empty;

    public string? Diagnosis { get; set; }

    public string? Icd10Code { get; set; }

    public DateOnly? FollowUpDate { get; set; }

    public byte Status { get; set; }

    public DateTime? FinalizedAt { get; set; }

    public DateTime CreatedAt { get; set; }


    // =================================================
    // PRESCRIPTION
    // =================================================

    public PatientRecordPrescriptionDTO? Prescription { get; set; }


    // =================================================
    // LAB RESULTS
    // =================================================

    public List<PatientRecordLabResultDTO> LabResults { get; set; } = new();


    // =================================================
    // ATTACHMENTS
    // =================================================

    public List<PatientRecordAttachmentDTO> Attachments { get; set; } = new();
}