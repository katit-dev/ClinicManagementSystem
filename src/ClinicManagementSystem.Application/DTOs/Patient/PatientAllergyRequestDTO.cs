using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Application.DTOs.Patient;

public class PatientAllergyRequestDTO
{
    [Required]
    [MaxLength(255)]
    public string Allergen { get; set; } = string.Empty;

    public byte? Severity { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}