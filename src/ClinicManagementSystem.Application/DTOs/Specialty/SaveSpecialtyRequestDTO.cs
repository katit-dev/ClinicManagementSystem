using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Application.DTOs.Specialty;

public class SaveSpecialtyRequestDTO
{
    [Required(ErrorMessage = "Vui lòng nhập mã chuyên khoa.")]
    [StringLength(20, ErrorMessage = "Mã chuyên khoa tối đa 20 ký tự.")]
    [RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9_-]*$",
        ErrorMessage = "Mã chỉ gồm chữ không dấu, số, dấu gạch ngang hoặc gạch dưới.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên chuyên khoa.")]
    [StringLength(255, ErrorMessage = "Tên chuyên khoa tối đa 255 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự.")]
    public string? Description { get; set; }
}

public class SetSpecialtyStatusRequestDTO
{
    [Required(ErrorMessage = "Vui lòng chọn trạng thái hoạt động.")]
    public bool? IsActive { get; set; }
}
