using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Application.DTOs.Auth;

public class UserRegisterDTO
{
    [Required(ErrorMessage = "Họ tên không được để trống")]
    [MaxLength(255)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số điện thoại không được để trống")]
    [RegularExpression(
        @"^0[0-9]{9}$",
        ErrorMessage = "Số điện thoại phải có 10 chữ số và bắt đầu bằng 0")]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [MaxLength(255)]
    public string? Email { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Mật khẩu không được để trống")]
    [MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự")]
    [MaxLength(128)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp")]
    public string ConfirmPassword { get; set; } = string.Empty;
}