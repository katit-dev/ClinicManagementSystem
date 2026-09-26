namespace ClinicManagementSystem.Application.DTOs.Patient;


public class PatientLookupDTO
{
    // PATIENT ID
    public int Id { get; set; }

    public string PatientCode { get; set; } = string.Empty;


    public string Phone { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    public bool HasAccount { get; set; }
}