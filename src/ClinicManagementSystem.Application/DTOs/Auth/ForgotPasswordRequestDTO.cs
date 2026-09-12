using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Application.DTOs.Auth;

public class ForgotPasswordRequestDTO
{
    [Required(ErrorMessage = "Email không được để trống")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;
}