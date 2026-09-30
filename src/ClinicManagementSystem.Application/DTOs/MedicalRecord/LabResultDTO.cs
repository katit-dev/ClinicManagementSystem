namespace ClinicManagementSystem.Application.DTOs.MedicalRecord;

public class LabResultDTO
{
    public int Id { get; set; }

    public string ResultValue { get; set; } = string.Empty;

    public string? ReferenceRange { get; set; }

    public string? Conclusion { get; set; }

    public DateTime ResultedAt { get; set; }
}