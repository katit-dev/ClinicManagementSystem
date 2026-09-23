namespace ClinicManagementSystem.Application.DTOs.PatientRecord;


// =====================================================
// PATIENT RECORD LAB RESULT DTO
// =====================================================

public class PatientRecordLabResultDTO
{
    public int Id { get; set; }

    public int MedicalRecordServiceId { get; set; }

    public int ServiceId { get; set; }

    public string ServiceName { get; set; } = string.Empty;

    public string? ServiceCode { get; set; }

    public string? ResultValue { get; set; }

    public string? ReferenceRange { get; set; }

    public string? Conclusion { get; set; }

    public DateTime? ResultedAt { get; set; }
}