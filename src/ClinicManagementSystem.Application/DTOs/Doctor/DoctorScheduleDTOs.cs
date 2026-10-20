using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Application.DTOs.Doctor;

public sealed class DoctorScheduleQueryDTO : IValidatableObject
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (From.HasValue != To.HasValue)
            yield return new("Vui lòng chọn đủ từ ngày và đến ngày.");
        if (From.HasValue && To.HasValue && (From > To || To == DateOnly.MaxValue || To.Value.DayNumber - From.Value.DayNumber > 41))
            yield return new("Khoảng lịch phải hợp lệ và không quá 42 ngày.");
    }
}

public sealed class DoctorTimeOffQueryDTO : IValidatableObject
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    // null = all; 0 = personal; 1 = clinic-wide.
    [Range(0, 1)] public byte? Scope { get; set; }
    [Range(0, 2)] public byte? Type { get; set; }
    [Range(1, int.MaxValue)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 10;
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (From.HasValue != To.HasValue)
            yield return new("Vui lòng chọn đủ từ ngày và đến ngày.");
        if (From.HasValue && To.HasValue && (From > To || To == DateOnly.MaxValue || To.Value.DayNumber - From.Value.DayNumber > 365))
            yield return new("Khoảng ngày nghỉ phải hợp lệ và không quá 366 ngày.");
    }
}

public sealed class DoctorScheduleDTO
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public DateTime ClinicNow { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public List<DoctorScheduleDayDTO> Days { get; set; } = [];
}

public sealed class DoctorScheduleDayDTO
{
    public DateOnly Date { get; set; }
    public bool IsToday { get; set; }
    public List<DoctorShiftDTO> Shifts { get; set; } = [];
    public List<DoctorScheduleBlockDTO> Blocks { get; set; } = [];
    public List<DoctorTimeOffDTO> TimeOffs { get; set; } = [];
}

public sealed class DoctorShiftDTO
{
    public int Id { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public TimeOnly? BreakStart { get; set; }
    public TimeOnly? BreakEnd { get; set; }
    public int SlotMinutes { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
}

public sealed class DoctorScheduleBlockDTO
{
    // work, break, leave, closure. Blocks use clinic wall time and exclusive EndAt.
    public string Kind { get; set; } = "work";
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int? ShiftId { get; set; }
    public int? TimeOffId { get; set; }
}

public sealed class DoctorTimeOffDTO
{
    public int Id { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public byte? Type { get; set; }
    public string? Reason { get; set; }
    public bool IsFullDay { get; set; }
    public bool IsClinicWide { get; set; }
    public string PeriodStatus { get; set; } = "upcoming";
}
