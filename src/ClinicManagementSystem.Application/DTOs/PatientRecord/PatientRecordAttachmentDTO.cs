namespace ClinicManagementSystem.Application.DTOs.PatientRecord;


// =====================================================
// PATIENT RECORD ATTACHMENT DTO
// =====================================================

public class PatientRecordAttachmentDTO
{
    public int Id { get; set; }

    public string? FileType { get; set; }

    public DateTime UploadedAt { get; set; }
}