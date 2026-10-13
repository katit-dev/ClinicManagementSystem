using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ClinicManagementSystem.Application.DTOs.User;

public class AdminResetPasswordRequestDTO : IValidatableObject
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự.")]
    [MaxLength(128, ErrorMessage = "Mật khẩu tối đa 128 ký tự.")]
    public string NewPassword { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (NewPassword != null && Encoding.UTF8.GetByteCount(NewPassword) > 72)
            yield return new("Mật khẩu không được vượt quá 72 bytes UTF-8 (BCrypt).", new[] { nameof(NewPassword) });
    }
}
