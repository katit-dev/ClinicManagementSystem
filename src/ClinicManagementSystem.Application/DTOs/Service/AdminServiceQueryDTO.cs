using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Application.DTOs.Service;

public class AdminServiceQueryDTO
{
    [StringLength(255, ErrorMessage = "Từ khóa tối đa 255 ký tự.")]
    public string? Keyword { get; set; }
    public bool? Active { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Chuyên khoa không hợp lệ.")]
    public int? SpecialtyId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Trang phải lớn hơn hoặc bằng 1.")]
    public int Page { get; set; } = 1;
    [Range(1, 100, ErrorMessage = "Số dòng mỗi trang phải từ 1 đến 100.")]
    public int PageSize { get; set; } = 10;
}
