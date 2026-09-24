namespace ClinicManagementSystem.Application.DTOs.Patient;


// =====================================================
// PATIENT DTO
// =====================================================

public class PatientDTO
{
    // =================================================
    // IDENTITY
    // =================================================

    public int Id { get; set; }

    public string PatientCode { get; set; } = string.Empty;


    // =================================================
    // PERSONAL INFORMATION
    // =================================================

    public string FullName { get; set; } = string.Empty;

    public byte? Gender { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public string Phone { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? NationalId { get; set; }

    public string? InsuranceNumber { get; set; }

    public string? BloodType { get; set; }
}