using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Application.DTOs.Patient;

public class PatientLookupRequestDTO
{
    [Required(ErrorMessage = "Số điện thoại không được để trống")]
    [RegularExpression(
        @"^0[0-9]{9}$",
        ErrorMessage = "Số điện thoại phải có 10 chữ số và bắt đầu bằng 0")]
    public string Phone { get; set; } = string.Empty;
}