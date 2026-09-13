using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Application.DTOs.Auth;

public class ResetPasswordRequestDTO
{
    [Required(ErrorMessage = "Email không được để trống")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mã OTP không được để trống")]
    [RegularExpression(@"^\d{6}$",ErrorMessage = "Mã OTP phải gồm 6 chữ số")]
    public string Otp { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu mới không được để trống")]
    [MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự")]
    [MaxLength(128, ErrorMessage = "Mật khẩu không được vượt quá 128 ký tự")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu xác nhận không được để trống")]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp")]
    public string ConfirmPassword { get; set; } = string.Empty;
}