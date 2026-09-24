using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Application.DTOs.Patient;

public class PatientRequestDTO
{
    [Required]
    [MaxLength(255)]
    public string FullName { get; set; } = string.Empty;

    [Range(0, 2)]
    public byte? Gender { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [Required]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(30)]
    public string? NationalId { get; set; }

    [MaxLength(50)]
    public string? InsuranceNumber { get; set; }

    [MaxLength(10)]
    public string? BloodType { get; set; }
}