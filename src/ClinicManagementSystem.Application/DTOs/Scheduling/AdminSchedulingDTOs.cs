using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Application.DTOs.Scheduling;

public class AdminScheduleQueryDTO : IValidatableObject
{
    [Range(1, int.MaxValue)] public int? DoctorId { get; set; }
    public DateOnly? Date { get; set; }
    public bool? Active { get; set; }
    [Range(1, int.MaxValue)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 10;

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Date == DateOnly.MinValue)
            yield return new("Ngày lọc không hợp lệ.", new[] { nameof(Date) });
    }
}

public class AdminTimeOffQueryDTO : IValidatableObject
{
    [Range(1, int.MaxValue)] public int? DoctorId { get; set; }
    public bool ClinicWideOnly { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    [Range(1, int.MaxValue)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 10;

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (From == DateOnly.MinValue || To == DateOnly.MaxValue || (From.HasValue && To < From))
            yield return new("Khoảng ngày lọc không hợp lệ.");
        if (ClinicWideOnly && DoctorId.HasValue)
            yield return new("Không lọc đồng thời một bác sĩ và chỉ nghỉ toàn phòng khám.");
    }
}

public class ScheduleRequestDTO : IValidatableObject
{
    [Range(1, int.MaxValue)] public int DoctorId { get; set; }
    [Required, Range(0, 6)] public byte? DayOfWeek { get; set; }
    [Required] public TimeOnly? StartTime { get; set; }
    [Required] public TimeOnly? EndTime { get; set; }
    public TimeOnly? BreakStart { get; set; }
    public TimeOnly? BreakEnd { get; set; }
    [Range(5, 120)] public int SlotMinutes { get; set; }
    [Required] public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool ConfirmAffectedAppointments { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (StartTime.HasValue && EndTime.HasValue && StartTime >= EndTime)
            yield return new("Giờ kết thúc phải sau giờ bắt đầu; ca làm không qua đêm.", new[] { nameof(EndTime) });
        if (BreakStart.HasValue != BreakEnd.HasValue)
            yield return new("Phải nhập đủ hai giờ nghỉ trưa hoặc bỏ trống cả hai.", new[] { nameof(BreakStart), nameof(BreakEnd) });
        else if (BreakStart.HasValue && (BreakStart >= BreakEnd || BreakStart < StartTime || BreakEnd > EndTime))
            yield return new("Khoảng nghỉ phải nằm trong ca làm và có giờ kết thúc sau giờ bắt đầu.", new[] { nameof(BreakStart), nameof(BreakEnd) });
        if (EffectiveFrom == DateOnly.MinValue || EffectiveTo == DateOnly.MaxValue || (EffectiveTo.HasValue && EffectiveTo < EffectiveFrom))
            yield return new("Ngày hiệu lực không hợp lệ.", new[] { nameof(EffectiveFrom), nameof(EffectiveTo) });
        if (StartTime.HasValue && EndTime.HasValue && (EndTime.Value.ToTimeSpan() - StartTime.Value.ToTimeSpan()).TotalMinutes < SlotMinutes)
            yield return new("Ca làm phải đủ dài để có ít nhất một slot.", new[] { nameof(SlotMinutes) });
        if (StartTime.HasValue && EndTime.HasValue && BreakStart.HasValue && BreakEnd.HasValue
            && (BreakStart.Value.ToTimeSpan() - StartTime.Value.ToTimeSpan()).TotalMinutes < SlotMinutes
            && (EndTime.Value.ToTimeSpan() - BreakEnd.Value.ToTimeSpan()).TotalMinutes < SlotMinutes)
            yield return new("Khoảng làm việc ngoài giờ nghỉ phải đủ cho ít nhất một slot.", new[] { nameof(BreakStart), nameof(BreakEnd) });
    }
}

// 0 = nghỉ phép, 1 = nghỉ lễ, 2 = nghỉ khác. EndAt is exclusive.
public class TimeOffRequestDTO : IValidatableObject
{
    [Range(1, int.MaxValue)] public int? DoctorId { get; set; }
    [Required] public DateTime? StartAt { get; set; }
    [Required] public DateTime? EndAt { get; set; }
    [Required, Range(0, 2)] public byte? Type { get; set; }
    [Required, StringLength(255)] public string Reason { get; set; } = string.Empty;
    public bool IsFullDay { get; set; }
    public bool ConfirmAffectedAppointments { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (StartAt.HasValue && EndAt.HasValue && (StartAt >= EndAt || StartAt == DateTime.MinValue || EndAt == DateTime.MaxValue))
            yield return new("Thời gian kết thúc nghỉ phải sau thời gian bắt đầu.", new[] { nameof(EndAt) });
        if ((StartAt.HasValue && StartAt.Value.Kind != DateTimeKind.Unspecified) || (EndAt.HasValue && EndAt.Value.Kind != DateTimeKind.Unspecified))
            yield return new("Nhập giờ địa phương phòng khám (UTC+7), không kèm hậu tố Z hoặc offset múi giờ.");
        if (IsFullDay && ((StartAt.HasValue && StartAt.Value.TimeOfDay != TimeSpan.Zero) || (EndAt.HasValue && EndAt.Value.TimeOfDay != TimeSpan.Zero)))
            yield return new("Nghỉ cả ngày: nhập từ 00:00 ngày đầu đến 00:00 ngày kế tiếp sau ngày cuối.");
    }
}

public class AdminDoctorOptionDTO
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class AdminDoctorScheduleDTO
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public byte DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public TimeOnly? BreakStart { get; set; }
    public TimeOnly? BreakEnd { get; set; }
    public int SlotMinutes { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
}

public class AdminDoctorTimeOffDTO
{
    public int Id { get; set; }
    public int? DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public byte? Type { get; set; }
    public string? Reason { get; set; }
    public bool IsFullDay { get; set; }
    public int? ApprovedBy { get; set; }
}

public class AffectedAppointmentDTO
{
    public int Id { get; set; }
    public string AppointmentCode { get; set; } = string.Empty;
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public byte Status { get; set; }
}

public class ScheduleChangeResultDTO
{
    public AdminDoctorScheduleDTO? Schedule { get; set; }
    public int? PreviousVersionId { get; set; }
    public bool RequiresConfirmation { get; set; }
    public List<AffectedAppointmentDTO> AffectedAppointments { get; set; } = new();
    public int NotificationsCreated { get; set; }
}

public class TimeOffChangeResultDTO
{
    public AdminDoctorTimeOffDTO? TimeOff { get; set; }
    public bool RequiresConfirmation { get; set; }
    public List<AffectedAppointmentDTO> AffectedAppointments { get; set; } = new();
    public int NotificationsCreated { get; set; }
}
