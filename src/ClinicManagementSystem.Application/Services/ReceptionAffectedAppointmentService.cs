using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Appointment;
using ClinicManagementSystem.Application.Enums;
using ClinicManagementSystem.Application.Helpers;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Application.Services;

public interface IReceptionAffectedAppointmentService
{
    Task<HttpResponseData<PagedResult<ReceptionAffectedAppointmentDTO>>> GetAffectedAsync(ReceptionAffectedQueryDTO query);
    Task<HttpResponseData<ReceptionAffectedAppointmentDTO>> RescheduleAsync(int id, ReceptionRescheduleAppointmentRequestDTO request, int actorId);
    Task<HttpResponseData<ReceptionAffectedAppointmentDTO>> CancelAsync(int id, ReceptionCancelAppointmentRequestDTO request, int actorId);
}

public class ReceptionAffectedAppointmentService : IReceptionAffectedAppointmentService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ReceptionAffectedAppointmentService> _logger;
    private readonly TimeProvider _clock;
    private DateTime Now => DoctorSlotGenerator.ClinicNow(_clock);
    public ReceptionAffectedAppointmentService(IUnitOfWork uow, ILogger<ReceptionAffectedAppointmentService> logger, TimeProvider? clock = null)
    { _uow = uow; _logger = logger; _clock = clock ?? TimeProvider.System; }

    public async Task<HttpResponseData<PagedResult<ReceptionAffectedAppointmentDTO>>> GetAffectedAsync(ReceptionAffectedQueryDTO query)
    {
        var error = Validate(query);
        if (error != null) return Response<PagedResult<ReceptionAffectedAppointmentDTO>>(400, error);
        var today = DateOnly.FromDateTime(Now);
        var from = query.From ?? (query.To?.AddDays(-30) ?? today.AddDays(-7));
        if (from.DayNumber > DateOnly.MaxValue.DayNumber - 90)
            return Response<PagedResult<ReceptionAffectedAppointmentDTO>>(400, "Khoảng ngày lọc không hợp lệ.");
        var to = query.To ?? from.AddDays(37);
        if (to < from || to.DayNumber - from.DayNumber > 90)
            return Response<PagedResult<ReceptionAffectedAppointmentDTO>>(400, "Chỉ lọc tối đa 91 ngày, ngày kết thúc phải từ ngày bắt đầu.");
        try
        {
            // Recompute current conflicts. Historical notifications are not an actionable queue.
            var appointments = await _uow.AppointmentRepository.GetUpcomingInRangeAsync(query.DoctorId,
                from.ToDateTime(TimeOnly.MinValue), to.AddDays(1).ToDateTime(TimeOnly.MinValue));
            var items = new List<ReceptionAffectedAppointmentDTO>();
            foreach (var group in appointments.Where(a => a.Status <= (byte)AppointmentStatus.CheckedIn)
                .GroupBy(a => (a.DoctorId, Date: DateOnly.FromDateTime(a.StartTime))))
            {
                var schedules = await _uow.DoctorScheduleRepository.GetEffectiveAsync(group.Key.DoctorId, group.Key.Date);
                var day = group.Key.Date.ToDateTime(TimeOnly.MinValue);
                var leave = await _uow.DoctorTimeOffRepository.GetOverlappingAsync(group.Key.DoctorId, day, day.AddDays(1));
                foreach (var appointment in group)
                {
                    var reasons = Reasons(appointment, schedules, leave);
                    if (reasons.Count > 0) items.Add(Map(appointment, reasons));
                }
            }
            items = items.OrderBy(a => a.StartTime).ThenBy(a => a.Id).ToList();
            var total = items.Count;
            var skip = ((long)query.Page - 1) * query.PageSize;
            return Response(200, "Danh sách lịch hẹn cần xử lý.", new PagedResult<ReceptionAffectedAppointmentDTO>
            {
                Items = skip >= total ? new() : items.Skip((int)skip).Take(query.PageSize).ToList(),
                Page = query.Page, PageSize = query.PageSize, TotalItems = total,
                TotalPages = (int)Math.Ceiling(total / (double)query.PageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list affected appointments.");
            return Response<PagedResult<ReceptionAffectedAppointmentDTO>>(500, "Không thể tải lịch hẹn bị ảnh hưởng.");
        }
    }

    public Task<HttpResponseData<ReceptionAffectedAppointmentDTO>> RescheduleAsync(int id, ReceptionRescheduleAppointmentRequestDTO request, int actorId)
        => ProcessAsync(id, request, actorId, request.NewStartTime);

    public Task<HttpResponseData<ReceptionAffectedAppointmentDTO>> CancelAsync(int id, ReceptionCancelAppointmentRequestDTO request, int actorId)
        => ProcessAsync(id, request, actorId, null);

    private async Task<HttpResponseData<ReceptionAffectedAppointmentDTO>> ProcessAsync(int id, ReceptionCancelAppointmentRequestDTO request, int actorId, DateTime? newStart)
    {
        if (actorId <= 0) return Response<ReceptionAffectedAppointmentDTO>(401, "Người dùng không hợp lệ.");
        var error = Validate(request);
        if (id <= 0 || error != null) return Response<ReceptionAffectedAppointmentDTO>(400, error ?? "Id lịch hẹn không hợp lệ.");
        var reschedule = request is ReceptionRescheduleAppointmentRequestDTO;
        if (reschedule && (!newStart.HasValue || newStart <= Now))
            return Response<ReceptionAffectedAppointmentDTO>(400, "Giờ mới phải nằm trong tương lai.");
        var started = false;
        try
        {
            await _uow.BeginTransactionAsync();
            started = true;
            // Same lock as VC20. Booking writers also recheck slots under this lock.
            await _uow.DoctorScheduleRepository.AcquireAdminLockAsync();
            var appointment = await _uow.AppointmentRepository.GetReceptionDetailAsync(id);
            if (appointment == null) return await Reject(404, "Không tìm thấy lịch hẹn.");
            if (!string.Equals(request.ExpectedVersion, Version(appointment), StringComparison.Ordinal))
                return await Reject(409, "Lịch hẹn đã được người khác cập nhật. Tải lại danh sách trước khi xử lý.");
            var cannotProcess = CannotProcess(appointment);
            if (cannotProcess != null) return await Reject(409, cannotProcess);
            var date = DateOnly.FromDateTime(appointment.StartTime);
            var schedules = await _uow.DoctorScheduleRepository.GetEffectiveAsync(appointment.DoctorId, date);
            var leave = await _uow.DoctorTimeOffRepository.GetOverlappingAsync(appointment.DoctorId, appointment.StartTime, appointment.EndTime);
            if (Reasons(appointment, schedules, leave).Count == 0)
                return await Reject(409, "Lịch hẹn không còn bị ảnh hưởng. Tải lại danh sách.");
            var before = Map(appointment, new());
            if (reschedule)
            {
                if (appointment.Doctor == null || !appointment.Doctor.IsActive || appointment.Patient?.IsActive != true)
                    return await Reject(409, "Bác sĩ hoặc bệnh nhân đã ngừng hoạt động.");
                if (newStart == appointment.StartTime) return await Reject(400, "Chọn giờ mới khác lịch hiện tại.");
                var newDate = DateOnly.FromDateTime(newStart!.Value);
                var newDay = newDate.ToDateTime(TimeOnly.MinValue);
                var newSchedules = await _uow.DoctorScheduleRepository.GetEffectiveAsync(appointment.DoctorId, newDate);
                var newLeave = await _uow.DoctorTimeOffRepository.GetOverlappingAsync(appointment.DoctorId, newDay, newDay.AddDays(1));
                var booked = await _uow.AppointmentRepository.GetBlockingInRangeAsync(appointment.DoctorId, newDay, newDay.AddDays(1));
                var slot = DoctorSlotGenerator.Generate(newDate, newSchedules, newLeave, booked.Where(a => a.Id != id), Now)
                    .FirstOrDefault(s => s.StartTime == newStart);
                if (slot == null) return await Reject(409, "Slot không còn khả dụng. Chọn lại ngày và giờ.");
                appointment.StartTime = slot.StartTime;
                appointment.EndTime = slot.EndTime;
            }
            else
            {
                // This feature does not issue refunds or alter invoices.
                if (appointment.Invoices.Any(i => i.PaidAmount > 0))
                    return await Reject(409, "Lịch hẹn có thanh toán. Cần xử lý thanh toán trước khi hủy.");
                appointment.Status = (byte)AppointmentStatus.Cancelled;
                appointment.CancelReason = request.Reason.Trim();
                await _uow.AppointmentStatusHistoryRepository.AddAsync(new AppointmentStatusHistory
                {
                    AppointmentId = id, FromStatus = before.Status, ToStatus = appointment.Status,
                    ChangedBy = actorId, Reason = request.Reason.Trim(), ChangedAt = Now
                });
            }
            appointment.UpdatedAt = Now;
            // Keep sent/history notices. Adjust only unsent reminders created by the booking flow.
            foreach (var reminder in appointment.Notifications.Where(n => n.Status == 0 && n.SentAt == null
                && n.ScheduledAt.HasValue && n.Title == "Nhắc lịch khám"))
            {
                if (reschedule)
                {
                    reminder.ScheduledAt = appointment.StartTime.AddHours(-24);
                    reminder.Content = $"Bạn có lịch khám vào {appointment.StartTime:HH:mm dd/MM/yyyy}.";
                }
                else reminder.Status = 3; // Suppressed: it must not be sent for a cancelled appointment.
            }
            var after = Map(appointment, new());
            await _uow.AuditLogRepository.AddAsync(new AuditLog
            {
                UserId = actorId, Action = reschedule ? "ReceptionRescheduleAppointment" : "ReceptionCancelAppointment",
                EntityName = "Appointment", EntityId = id, Succeeded = true, OccurredAt = _clock.GetUtcNow().UtcDateTime,
                Details = JsonSerializer.Serialize(new { Before = before, After = after, Reason = request.Reason.Trim() })
            });
            await NotifyAsync(appointment, before.StartTime, request.Reason.Trim(), reschedule);
            await _uow.SaveChangesAsync();
            await _uow.CommitTransactionAsync();
            started = false;
            return Response(200, reschedule ? "Đã đổi lịch hẹn." : "Đã hủy lịch hẹn.", after);
        }
        catch (Exception ex)
        {
            if (started) await RollbackSafely();
            _logger.LogError(ex, "Failed to process affected appointment {AppointmentId}.", id);
            return ex is DbUpdateConcurrencyException || ex is DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } }
                ? Response<ReceptionAffectedAppointmentDTO>(409, "Lịch hẹn hoặc slot đã thay đổi. Tải lại danh sách.")
                : Response<ReceptionAffectedAppointmentDTO>(500, "Không thể xử lý lịch hẹn. Vui lòng thử lại.");
        }

        async Task<HttpResponseData<ReceptionAffectedAppointmentDTO>> Reject(int status, string message)
        {
            await _uow.RollbackTransactionAsync();
            started = false;
            return Response<ReceptionAffectedAppointmentDTO>(status, message);
        }
    }

    private async Task RollbackSafely()
    {
        try { await _uow.RollbackTransactionAsync(); }
        catch (Exception ex) { _logger.LogError(ex, "Failed to roll back reception appointment processing."); }
    }

    private async Task NotifyAsync(Appointment appointment, DateTime oldStart, string reason, bool rescheduled)
    {
        var users = new[] { appointment.Patient?.UserId, appointment.Doctor?.UserId }
            .Where(id => id > 0).Select(id => id!.Value).Distinct();
        var content = rescheduled
            ? $"Lịch {appointment.AppointmentCode} đã đổi từ {oldStart:HH:mm dd/MM/yyyy} sang {appointment.StartTime:HH:mm dd/MM/yyyy}. Lý do: {reason}"
            : $"Lịch {appointment.AppointmentCode} lúc {oldStart:HH:mm dd/MM/yyyy} đã hủy. Lý do: {reason}";
        var utc = _clock.GetUtcNow().UtcDateTime;
        await _uow.NotificationRepository.AddListItemsAsync(users.Select(userId => new Notification
        {
            AppointmentId = appointment.Id, UserId = userId, Channel = "InApp", Title = rescheduled ? "Lịch hẹn đã thay đổi" : "Lịch hẹn đã hủy",
            Content = content, Status = 1, CreatedAt = utc, SentAt = utc
        }).ToList());
    }

    private static List<string> Reasons(Appointment appointment, List<DoctorSchedule> schedules, List<DoctorTimeOff> leave)
    {
        var reasons = new List<string>();
        if (appointment.Doctor?.IsActive != true) reasons.Add("Bác sĩ đã ngừng hoạt động.");
        if (leave.Any(t => (t.DoctorId == null || t.DoctorId == appointment.DoctorId) && t.StartAt < appointment.EndTime && t.EndAt > appointment.StartTime))
            reasons.Add("Lịch hẹn nằm trong thời gian nghỉ của bác sĩ hoặc phòng khám.");
        var slots = DoctorSlotGenerator.Generate(DateOnly.FromDateTime(appointment.StartTime), schedules,
            Array.Empty<DoctorTimeOff>(), Array.Empty<Appointment>(), DateTime.MinValue);
        if (!slots.Any(s => s.StartTime == appointment.StartTime && s.EndTime == appointment.EndTime))
            reasons.Add("Lịch hẹn không còn khớp ca làm và thời lượng slot đang có hiệu lực.");
        return reasons;
    }

    private string? CannotProcess(Appointment appointment)
        => appointment.Status == (byte)AppointmentStatus.CheckedIn ? "Bệnh nhân đã check-in; cần xử lý với bác sĩ."
            : appointment.Status != (byte)AppointmentStatus.Pending && appointment.Status != (byte)AppointmentStatus.Confirmed
                ? "Trạng thái lịch hẹn không cho phép đổi hoặc hủy."
            : appointment.StartTime <= Now ? "Lịch hẹn đã đến hoặc quá giờ; cần xử lý riêng."
            : null;

    private ReceptionAffectedAppointmentDTO Map(Appointment appointment, List<string> reasons) => new()
    {
        Id = appointment.Id, AppointmentCode = appointment.AppointmentCode, DoctorId = appointment.DoctorId,
        DoctorName = appointment.Doctor?.FullName ?? string.Empty, PatientId = appointment.PatientId,
        PatientName = appointment.Patient?.FullName ?? string.Empty, PatientPhone = appointment.Patient?.Phone,
        StartTime = appointment.StartTime, EndTime = appointment.EndTime, Status = appointment.Status,
        AffectedReasons = reasons, CanProcess = CannotProcess(appointment) == null,
        CannotProcessReason = CannotProcess(appointment), Version = Version(appointment)
    };

    private static string Version(Appointment appointment)
    {
        var text = FormattableString.Invariant($"{appointment.Id}|{appointment.StartTime.Ticks}|{appointment.EndTime.Ticks}|{appointment.Status}|{appointment.UpdatedAt?.Ticks}");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }

    private static string? Validate(object value)
    {
        var results = new List<ValidationResult>();
        return Validator.TryValidateObject(value, new ValidationContext(value), results, true) ? null
            : string.Join(" ", results.Select(r => r.ErrorMessage));
    }
    private static HttpResponseData<T> Response<T>(int status, string message, T? content = default)
        => new() { StatusCode = status, Message = message, Content = content };
}
