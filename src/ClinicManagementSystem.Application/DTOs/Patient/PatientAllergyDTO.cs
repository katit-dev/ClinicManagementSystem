namespace ClinicManagementSystem.Application.DTOs.Patient;

public class PatientAllergyDTO
{
    public int Id { get; set; }
    public int PatientId { get; set; }

    public string Allergen { get; set; } = string.Empty;
    public byte? Severity { get; set; }
    public string? Note { get; set; }
}