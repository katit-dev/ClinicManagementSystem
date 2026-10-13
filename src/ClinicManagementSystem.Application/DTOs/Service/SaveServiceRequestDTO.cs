using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Application.DTOs.Service;

public class SaveServiceRequestDTO : IValidatableObject
{
    [Required(ErrorMessage = "Vui lòng nhập mã dịch vụ.")]
    [StringLength(30, ErrorMessage = "Mã dịch vụ tối đa 30 ký tự.")]
    [RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9_-]*$", ErrorMessage = "Mã chỉ gồm chữ không dấu, số, dấu gạch ngang hoặc gạch dưới.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên dịch vụ.")]
    [StringLength(255, ErrorMessage = "Tên dịch vụ tối đa 255 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự.")]
    public string? Description { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Chuyên khoa không hợp lệ.")]
    public int? SpecialtyId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập giá dịch vụ.")]
    [Range(typeof(decimal), "0", "9999999999.99", ErrorMessage = "Giá phải từ 0 đến 9.999.999.999,99.")]
    public decimal? Price { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập thời lượng dịch vụ.")]
    [Range(1, int.MaxValue, ErrorMessage = "Thời lượng phải là số phút nguyên lớn hơn 0.")]
    public int? DurationMinutes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Price.HasValue && Price.Value != decimal.Round(Price.Value, 2))
            yield return new("Giá chỉ được có tối đa 2 chữ số thập phân.", new[] { nameof(Price) });
    }
}
