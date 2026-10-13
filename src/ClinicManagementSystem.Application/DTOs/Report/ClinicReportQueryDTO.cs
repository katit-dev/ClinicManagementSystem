using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Application.DTOs.Report;

public class ClinicReportQueryDTO : IValidatableObject
{
    [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu.")]
    public DateOnly? From { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc.")]
    public DateOnly? To { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Id bác sĩ phải lớn hơn 0.")]
    public int? DoctorId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (From.HasValue && To.HasValue)
        {
            if (To.Value < From.Value)
                yield return new("Ngày kết thúc không được trước ngày bắt đầu.", [nameof(To)]);
            else if (To.Value.DayNumber - From.Value.DayNumber > 365)
                yield return new("Mỗi báo cáo tối đa 366 ngày. Vui lòng thu hẹp khoảng ngày.", [nameof(To)]);
            if (To.Value == DateOnly.MaxValue)
                yield return new("Ngày kết thúc nằm ngoài phạm vi hỗ trợ.", [nameof(To)]);
        }
    }
}
