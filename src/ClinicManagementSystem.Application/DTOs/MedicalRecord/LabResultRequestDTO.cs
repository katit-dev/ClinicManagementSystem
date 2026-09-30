namespace ClinicManagementSystem.Application.DTOs.MedicalRecord;

public class LabResultRequestDTO
{
    public string ResultValue { get; set; } = string.Empty;

    public string? ReferenceRange { get; set; }

    public string? Conclusion { get; set; }

    public DateTime ResultedAt { get; set; }
}