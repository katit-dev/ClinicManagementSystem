using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Application.DTOs.Doctor;

public sealed class DoctorMedicalRecordQueryDTO : IValidatableObject
{
    [StringLength(100, ErrorMessage = "Từ khóa tối đa 100 ký tự.")]
    public string? Keyword { get; set; }
    [Range(0, 1, ErrorMessage = "Trạng thái bệnh án không hợp lệ.")]
    public byte? Status { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Trang phải lớn hơn 0.")]
    public int Page { get; set; } = 1;
    [Range(1, 100, ErrorMessage = "Số dòng mỗi trang từ 1 đến 100.")]
    public int PageSize { get; set; } = 20;

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (From.HasValue != To.HasValue)
            yield return new("Vui lòng nhập đủ từ ngày và đến ngày.", [nameof(From), nameof(To)]);
        if (From.HasValue && To.HasValue && (From > To || To.Value.DayNumber - From.Value.DayNumber > 365))
            yield return new("Khoảng ngày khám phải hợp lệ và không quá 366 ngày.", [nameof(From), nameof(To)]);
    }
}
