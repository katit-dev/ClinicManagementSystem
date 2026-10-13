using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Application.DTOs.User;

public class SetUserLockRequestDTO
{
    [Required(ErrorMessage = "Vui lòng gửi trạng thái tài khoản.")]
    public bool? IsActive { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập lý do khóa hoặc mở khóa.")]
    [StringLength(500, ErrorMessage = "Lý do tối đa 500 ký tự.")]
    public string Reason { get; set; } = string.Empty;
}
