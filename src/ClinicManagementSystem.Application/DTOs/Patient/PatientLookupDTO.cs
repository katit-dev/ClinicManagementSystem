namespace ClinicManagementSystem.Application.DTOs.Patient;

public class PatientLookupDTO
{
    public string FullName { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    public bool HasAccount { get; set; }
}