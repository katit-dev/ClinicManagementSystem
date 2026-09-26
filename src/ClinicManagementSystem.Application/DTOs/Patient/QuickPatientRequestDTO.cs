using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Application.DTOs.Patient;


public class QuickPatientRequestDTO
{
    [Required]
    [MaxLength(255)]
    public string FullName { get; set; }
        = string.Empty;


    [Required]
    [MaxLength(20)]
    public string Phone { get; set; }
        = string.Empty;
}