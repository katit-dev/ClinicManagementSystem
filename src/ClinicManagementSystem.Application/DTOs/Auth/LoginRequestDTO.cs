using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Application.DTOs.Auth;

public class LoginRequestDTO
{
    [Required(ErrorMessage = "Tên đăng nhập hoặc email không được để trống")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu không được để trống")]
    public string Password { get; set; } = string.Empty;
}