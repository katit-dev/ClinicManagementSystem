namespace ClinicManagementSystem.Application.DTOs.MedicalRecord;

public class AttachmentDTO
{
    public int Id { get; set; }

    public int MedicalRecordId { get; set; }

    public string FileUrl { get; set; } = string.Empty;

    public string? FileType { get; set; }

    public DateTime UploadedAt { get; set; }

    public int? UploadedBy { get; set; }
}