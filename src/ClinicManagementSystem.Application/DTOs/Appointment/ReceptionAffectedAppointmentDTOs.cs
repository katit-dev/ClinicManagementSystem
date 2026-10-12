using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Application.DTOs.Appointment;

public class ReceptionAffectedQueryDTO : IValidatableObject
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    [Range(1, int.MaxValue)] public int? DoctorId { get; set; }
    [Range(1, int.MaxValue)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 10;

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (From == DateOnly.MinValue || To == DateOnly.MaxValue || (To.HasValue && To.Value.DayNumber < 30) || (From.HasValue && To < From))
            yield return new("Khoảng ngày lọc không hợp lệ.");
        if (From.HasValue && To.HasValue && To.Value.DayNumber - From.Value.DayNumber > 90)
            yield return new("Chỉ lọc tối đa 91 ngày mỗi lần.");
    }
}

public class ReceptionAffectedAppointmentDTO
{
    public int Id { get; set; }
    public string AppointmentCode { get; set; } = string.Empty;
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string? PatientPhone { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public byte Status { get; set; }
    public List<string> AffectedReasons { get; set; } = new();
    public bool CanProcess { get; set; }
    public string? CannotProcessReason { get; set; }
    // Opaque snapshot: return unchanged with mutations to reject an out-of-date form.
    public string Version { get; set; } = string.Empty;
}

public class ReceptionCancelAppointmentRequestDTO
{
    [Required, StringLength(64, MinimumLength = 64)] public string ExpectedVersion { get; set; } = string.Empty;
    [Required, StringLength(500)] public string Reason { get; set; } = string.Empty;
}

public class ReceptionRescheduleAppointmentRequestDTO : ReceptionCancelAppointmentRequestDTO, IValidatableObject
{
    [Required] public DateTime? NewStartTime { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (NewStartTime.HasValue && (NewStartTime.Value.Kind != DateTimeKind.Unspecified
            || NewStartTime == DateTime.MinValue || NewStartTime.Value.Date == DateTime.MaxValue.Date))
            yield return new("Dùng giờ phòng khám UTC+7, không gửi Z hoặc offset.", new[] { nameof(NewStartTime) });
    }
}
