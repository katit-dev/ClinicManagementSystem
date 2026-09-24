using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Application.DTOs.Patient;


// =====================================================
// PATIENT REQUEST DTO
// =====================================================

public class PatientRequestDTO
{

    [Required]
    [MaxLength(255)]
    public string FullName { get; set; } = string.Empty;


    // =================================================
    // GENDER
    //
    // 0 = Unknown
    // 1 = Male
    // 2 = Female
    // =================================================

    [Range(0, 2)]
    public byte? Gender { get; set; }

    public DateOnly? DateOfBirth { get; set; }
    
    [Required]
    public string Phone { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? NationalId { get; set; }

    public string? InsuranceNumber { get; set; }

    public string? BloodType { get; set; }
}