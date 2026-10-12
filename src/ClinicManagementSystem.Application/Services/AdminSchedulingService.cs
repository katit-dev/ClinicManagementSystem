using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Scheduling;
using ClinicManagementSystem.Application.Helpers;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Application.Services;

public interface IAdminSchedulingService
{
    Task<HttpResponseData<List<AdminDoctorOptionDTO>>> GetDoctorsAsync();
    Task<HttpResponseData<PagedResult<AdminDoctorScheduleDTO>>> GetSchedulesAsync(AdminScheduleQueryDTO query);
    Task<HttpResponseData<ScheduleChangeResultDTO>> SaveScheduleAsync(int? id, ScheduleRequestDTO request, int actorId);
    Task<HttpResponseData<PagedResult<AdminDoctorTimeOffDTO>>> GetTimeOffsAsync(AdminTimeOffQueryDTO query);
    Task<HttpResponseData<TimeOffChangeResultDTO>> CreateTimeOffAsync(TimeOffRequestDTO request, int actorId);
    Task<HttpResponseData<AdminDoctorTimeOffDTO>> DeleteTimeOffAsync(int id, int actorId);
}

public class AdminSchedulingService : IAdminSchedulingService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<AdminSchedulingService> _logger;
    private readonly TimeProvider _clock;
    public AdminSchedulingService(IUnitOfWork uow, ILogger<AdminSchedulingService> logger, TimeProvider? clock = null)
    {
        _uow = uow;
        _logger = logger;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<HttpResponseData<List<AdminDoctorOptionDTO>>> GetDoctorsAsync()
    {
        try
        {
            var doctors = await _uow.DoctorRepository.GetAdminOptionsAsync();
            return Response(200, "Danh sách bác sĩ.", doctors.Select(d => new AdminDoctorOptionDTO
                { Id = d.Id, FullName = d.FullName, IsActive = d.IsActive }).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list scheduling doctors.");
            return Response<List<AdminDoctorOptionDTO>>(500, "Không thể tải danh sách bác sĩ.");
        }
    }

    public async Task<HttpResponseData<PagedResult<AdminDoctorScheduleDTO>>> GetSchedulesAsync(AdminScheduleQueryDTO query)
    {
        var error = Validate(query);
        if (error != null) return Response<PagedResult<AdminDoctorScheduleDTO>>(400, error);
        try
        {
            var (items, total) = await _uow.DoctorScheduleRepository.GetAdminPageAsync(query.DoctorId, query.Date, query.Active, query.Page, query.PageSize);
            return Response(200, "Danh sách lịch làm việc.", Page(items.Select(Map).ToList(), total, query.Page, query.PageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list doctor schedules.");
            return Response<PagedResult<AdminDoctorScheduleDTO>>(500, "Không thể tải lịch làm việc.");
        }
    }

    public async Task<HttpResponseData<PagedResult<AdminDoctorTimeOffDTO>>> GetTimeOffsAsync(AdminTimeOffQueryDTO query)
    {
        var error = Validate(query);
        if (error != null) return Response<PagedResult<AdminDoctorTimeOffDTO>>(400, error);
        try
        {
            var (items, total) = await _uow.DoctorTimeOffRepository.GetAdminPageAsync(query.DoctorId, query.ClinicWideOnly,
                query.From?.ToDateTime(TimeOnly.MinValue), query.To?.AddDays(1).ToDateTime(TimeOnly.MinValue), query.Page, query.PageSize);
            return Response(200, "Danh sách nghỉ phép.", Page(items.Select(Map).ToList(), total, query.Page, query.PageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list doctor time off.");
            return Response<PagedResult<AdminDoctorTimeOffDTO>>(500, "Không thể tải danh sách nghỉ phép.");
        }
    }

    public Task<HttpResponseData<ScheduleChangeResultDTO>> SaveScheduleAsync(int? id, ScheduleRequestDTO request, int actorId)
    {
        var error = Validate(request);
        if (error != null) return Task.FromResult(Response<ScheduleChangeResultDTO>(400, error));
        if (actorId <= 0) return Task.FromResult(Response<ScheduleChangeResultDTO>(401, "Người dùng không hợp lệ."));
        return MutateAsync(async () =>
        {
            var doctor = await _uow.DoctorRepository.GetByIdAsync(request.DoctorId);
            if (doctor == null) return Response<ScheduleChangeResultDTO>(404, "Không tìm thấy bác sĩ.");
            if (!doctor.IsActive) return Response<ScheduleChangeResultDTO>(409, "Bác sĩ đã ngừng hoạt động.");
            var old = id.HasValue ? await _uow.DoctorScheduleRepository.GetByIdAsync(id.Value) : null;
            if (id.HasValue && old == null) return Response<ScheduleChangeResultDTO>(404, "Không tìm thấy lịch làm việc.");
            var now = DoctorSlotGenerator.ClinicNow(_clock);
            var today = DateOnly.FromDateTime(now);
            var from = request.EffectiveFrom!.Value;
            if (old != null && (old.DoctorId != request.DoctorId || old.DayOfWeek != request.DayOfWeek))
                return Response<ScheduleChangeResultDTO>(400, "Không đổi bác sĩ hoặc thứ của lịch hiện có; hãy tạo lịch riêng.");
            if (old != null && (!old.IsActive || from < old.EffectiveFrom || from < today))
                return Response<ScheduleChangeResultDTO>(409, "Không ghi đè lịch quá khứ hoặc lịch đã ngừng. Chọn ngày hiệu lực mới từ hôm nay.");
            var version = old != null && from > old.EffectiveFrom;
            var schedule = new DoctorSchedule
            {
                Id = old != null && !version ? old.Id : 0,
                DoctorId = doctor.Id, Doctor = doctor, DayOfWeek = request.DayOfWeek!.Value,
                StartTime = request.StartTime!.Value, EndTime = request.EndTime!.Value,
                BreakStart = request.BreakStart, BreakEnd = request.BreakEnd,
                SlotMinutes = request.SlotMinutes, EffectiveFrom = from, EffectiveTo = request.EffectiveTo, IsActive = true
            };
            var existing = await _uow.DoctorScheduleRepository.GetForDoctorDayAsync(doctor.Id, schedule.DayOfWeek);
            if (existing.Any(s => s.Id != schedule.Id && s.StartTime == schedule.StartTime && s.EffectiveFrom == from))
                return Response<ScheduleChangeResultDTO>(409, "Đã có phiên bản lịch cùng bác sĩ, thứ, giờ bắt đầu và ngày hiệu lực.");

            var proposed = existing.Where(s => s.Id != old?.Id).Select(Clone).ToList();
            DoctorSchedule? closed = null;
            if (version)
            {
                closed = Clone(old!);
                if (!closed.EffectiveTo.HasValue || closed.EffectiveTo >= from)
                    closed.EffectiveTo = from.AddDays(-1);
                proposed.Add(closed);
            }
            if (proposed.Any(s => s.IsActive && DatesOverlap(s, schedule) && s.StartTime < schedule.EndTime && s.EndTime > schedule.StartTime))
                return Response<ScheduleChangeResultDTO>(409, "Ca làm bị trùng giờ trong cùng giai đoạn hiệu lực.");
            proposed.Add(schedule);

            var rangeFrom = old != null && old.EffectiveFrom < from ? old.EffectiveFrom : from;
            var rangeTo = old == null ? schedule.EffectiveTo : MaxEnd(old.EffectiveTo, schedule.EffectiveTo);
            var lower = rangeFrom.ToDateTime(TimeOnly.MinValue);
            if (lower < now) lower = now;
            var upper = EndExclusive(rangeTo);
            var appointments = upper <= lower ? new List<Appointment>() : await _uow.AppointmentRepository.GetUpcomingInRangeAsync(doctor.Id, lower, upper);
            var affected = appointments.Where(a =>
            {
                if (a.Status >= 4 || a.EndTime <= now) return false;
                var date = DateOnly.FromDateTime(a.StartTime);
                if ((byte)date.DayOfWeek != schedule.DayOfWeek) return false;
                var touchesOld = old != null && Applies(old, date) && Touches(old, a);
                var touchesNew = Applies(schedule, date) && Touches(schedule, a);
                if (!touchesOld && !touchesNew) return false;
                var stillFits = DoctorSlotGenerator.Generate(date, proposed, Array.Empty<DoctorTimeOff>(), Array.Empty<Appointment>(), DateTime.MinValue)
                    .Any(s => s.StartTime == a.StartTime && s.EndTime == a.EndTime);
                return !stillFits || (old != null && old.SlotMinutes != schedule.SlotMinutes && (touchesOld || touchesNew));
            }).ToList();
            var result = new ScheduleChangeResultDTO
            {
                Schedule = Map(schedule), PreviousVersionId = version ? old!.Id : null,
                AffectedAppointments = affected.Select(Map).ToList(), RequiresConfirmation = affected.Count > 0 && !request.ConfirmAffectedAppointments
            };
            if (result.RequiresConfirmation)
                return Response(409, "Lịch thay đổi ảnh hưởng lịch hẹn đã đặt. Kiểm tra danh sách và gửi lại với confirmAffectedAppointments=true để xác nhận.", result);

            var before = old != null ? Map(old) : null;
            if (version && closed!.EffectiveTo != old!.EffectiveTo)
            {
                old.EffectiveTo = closed.EffectiveTo;
                await _uow.DoctorScheduleRepository.UpdateAsync(old);
            }
            if (old != null && !version)
            {
                // Preserve the tracked entity; never update existing appointment times.
                old.StartTime = schedule.StartTime; old.EndTime = schedule.EndTime;
                old.BreakStart = schedule.BreakStart; old.BreakEnd = schedule.BreakEnd;
                old.SlotMinutes = schedule.SlotMinutes; old.EffectiveTo = schedule.EffectiveTo;
                old.Doctor = doctor;
                schedule = old;
                await _uow.DoctorScheduleRepository.UpdateAsync(old);
            }
            else await _uow.DoctorScheduleRepository.AddAsync(schedule);
            await _uow.SaveChangesAsync();
            result.Schedule = Map(schedule);
            await AuditAsync(actorId, old == null ? "CreateDoctorSchedule" : version ? "VersionDoctorSchedule" : "UpdateDoctorSchedule",
                "DoctorSchedule", schedule.Id, new { Before = before, After = result.Schedule, result.PreviousVersionId, result.AffectedAppointments });
            result.NotificationsCreated = await NotifyAsync(affected, "Lịch làm việc thay đổi", "Lịch bác sĩ đã thay đổi; vui lòng kiểm tra và liên hệ đổi lịch hẹn.");
            return Response(old == null || version ? 201 : 200, "Đã lưu lịch làm việc. Lịch hẹn đã đặt được giữ nguyên.", result);
        });
    }

    public Task<HttpResponseData<TimeOffChangeResultDTO>> CreateTimeOffAsync(TimeOffRequestDTO request, int actorId)
    {
        var error = Validate(request);
        if (error != null) return Task.FromResult(Response<TimeOffChangeResultDTO>(400, error));
        if (actorId <= 0) return Task.FromResult(Response<TimeOffChangeResultDTO>(401, "Người dùng không hợp lệ."));
        return MutateAsync(async () =>
        {
            Doctor? doctor = null;
            if (request.DoctorId.HasValue)
            {
                doctor = await _uow.DoctorRepository.GetByIdAsync(request.DoctorId.Value);
                if (doctor == null) return Response<TimeOffChangeResultDTO>(404, "Không tìm thấy bác sĩ.");
                if (!doctor.IsActive) return Response<TimeOffChangeResultDTO>(409, "Bác sĩ đã ngừng hoạt động.");
            }
            var start = DateTime.SpecifyKind(request.StartAt!.Value, DateTimeKind.Unspecified);
            var end = DateTime.SpecifyKind(request.EndAt!.Value, DateTimeKind.Unspecified);
            if (await _uow.DoctorTimeOffRepository.HasOverlapAsync(request.DoctorId, start, end))
                return Response<TimeOffChangeResultDTO>(409, "Khoảng nghỉ trùng lịch nghỉ của bác sĩ hoặc toàn phòng khám.");
            var now = DoctorSlotGenerator.ClinicNow(_clock);
            var lower = start < now ? now : start;
            var affected = end <= lower ? new List<Appointment>() : await _uow.AppointmentRepository.GetUpcomingInRangeAsync(request.DoctorId, lower, end);
            affected = affected.Where(a => a.Status < 4 && a.EndTime > lower && a.StartTime < end).ToList();
            var leave = new DoctorTimeOff
            {
                DoctorId = request.DoctorId, Doctor = doctor, StartAt = start, EndAt = end,
                Type = request.Type, Reason = request.Reason.Trim(), IsFullDay = request.IsFullDay,
                ApprovedBy = actorId, CreatedAt = _clock.GetUtcNow().UtcDateTime
            };
            var result = new TimeOffChangeResultDTO
            {
                TimeOff = Map(leave), AffectedAppointments = affected.Select(Map).ToList(),
                RequiresConfirmation = affected.Count > 0 && !request.ConfirmAffectedAppointments
            };
            if (result.RequiresConfirmation)
                return Response(409, "Có lịch hẹn trong khoảng nghỉ. Kiểm tra danh sách và gửi lại với confirmAffectedAppointments=true để xác nhận.", result);
            await _uow.DoctorTimeOffRepository.AddAsync(leave);
            await _uow.SaveChangesAsync();
            result.TimeOff = Map(leave);
            await AuditAsync(actorId, "CreateDoctorTimeOff", "DoctorTimeOff", leave.Id, new { After = result.TimeOff, result.AffectedAppointments });
            result.NotificationsCreated = await NotifyAsync(affected, "Lịch nghỉ ảnh hưởng lịch hẹn", "Bác sĩ hoặc phòng khám có lịch nghỉ; vui lòng liên hệ để đổi lịch hẹn.");
            return Response(201, "Đã đăng ký nghỉ. Lịch hẹn đã đặt được giữ nguyên để lễ tân xử lý.", result);
        });
    }

    public Task<HttpResponseData<AdminDoctorTimeOffDTO>> DeleteTimeOffAsync(int id, int actorId)
    {
        if (id <= 0) return Task.FromResult(Response<AdminDoctorTimeOffDTO>(400, "Id lịch nghỉ không hợp lệ."));
        if (actorId <= 0) return Task.FromResult(Response<AdminDoctorTimeOffDTO>(401, "Người dùng không hợp lệ."));
        return MutateAsync(async () =>
        {
            var leave = await _uow.DoctorTimeOffRepository.GetByIdAsync(id);
            if (leave == null) return Response<AdminDoctorTimeOffDTO>(404, "Không tìm thấy lịch nghỉ.");
            if (leave.DoctorId.HasValue) leave.Doctor = await _uow.DoctorRepository.GetByIdAsync(leave.DoctorId.Value);
            var before = Map(leave);
            await _uow.DoctorTimeOffRepository.DeleteAsync(id);
            await AuditAsync(actorId, "DeleteDoctorTimeOff", "DoctorTimeOff", id, new { Before = before });
            return Response(200, "Đã xóa lịch nghỉ; không thay đổi lịch hẹn đã đặt.", before);
        });
    }

    private async Task<HttpResponseData<T>> MutateAsync<T>(Func<Task<HttpResponseData<T>>> action)
    {
        var started = false;
        try
        {
            await _uow.BeginTransactionAsync();
            started = true;
            await _uow.DoctorScheduleRepository.AcquireAdminLockAsync();
            var result = await action();
            if (result.StatusCode >= 400) await _uow.RollbackTransactionAsync();
            else
            {
                await _uow.SaveChangesAsync();
                await _uow.CommitTransactionAsync();
            }
            started = false;
            return result;
        }
        catch (Exception ex)
        {
            if (started)
            {
                try { await _uow.RollbackTransactionAsync(); }
                catch (Exception rollback) { _logger.LogError(rollback, "Scheduling transaction rollback failed."); }
            }
            _logger.LogError(ex, "Scheduling administration failed.");
            return ex is DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } }
                ? Response<T>(409, "Lịch bị trùng; tải lại danh sách và kiểm tra.")
                : Response<T>(500, "Không thể lưu thay đổi lịch. Vui lòng thử lại.");
        }
    }

    private async Task AuditAsync(int actorId, string action, string entity, int id, object details)
        => await _uow.AuditLogRepository.AddAsync(new AuditLog
        {
            UserId = actorId, Action = action, EntityName = entity, EntityId = id,
            Details = JsonSerializer.Serialize(details), Succeeded = true, OccurredAt = _clock.GetUtcNow().UtcDateTime
        });

    private async Task<int> NotifyAsync(List<Appointment> affected, string title, string message)
    {
        if (affected.Count == 0) return 0;
        var receptionists = await _uow.UserRoleRepository.GetActiveReceptionistUserIdsAsync();
        var notices = new List<Notification>();
        foreach (var appointment in affected)
        {
            var recipients = receptionists.Concat(appointment.Doctor?.UserId > 0 ? new[] { appointment.Doctor.UserId } : Array.Empty<int>()).Distinct();
            foreach (var userId in recipients)
                notices.Add(new Notification
                {
                    AppointmentId = appointment.Id, UserId = userId, Channel = "InApp", Title = title,
                    Content = $"{message} Mã lịch: {appointment.AppointmentCode}; {appointment.StartTime:HH:mm dd/MM/yyyy}.",
                    Status = 1, CreatedAt = _clock.GetUtcNow().UtcDateTime, SentAt = _clock.GetUtcNow().UtcDateTime
                });
        }
        if (notices.Count > 0) await _uow.NotificationRepository.AddListItemsAsync(notices);
        return notices.Count;
    }

    private static bool Applies(DoctorSchedule s, DateOnly d) => s.IsActive && s.EffectiveFrom <= d && (!s.EffectiveTo.HasValue || s.EffectiveTo >= d);
    private static bool Touches(DoctorSchedule s, Appointment a) => a.StartTime < a.StartTime.Date.Add(s.EndTime.ToTimeSpan()) && a.EndTime > a.StartTime.Date.Add(s.StartTime.ToTimeSpan());
    private static bool DatesOverlap(DoctorSchedule a, DoctorSchedule b) => a.EffectiveFrom <= (b.EffectiveTo ?? DateOnly.MaxValue) && b.EffectiveFrom <= (a.EffectiveTo ?? DateOnly.MaxValue);
    private static DateOnly? MaxEnd(DateOnly? a, DateOnly? b) => a == null || b == null ? null : a > b ? a : b;
    private static DateTime EndExclusive(DateOnly? date) => date == null || date == DateOnly.MaxValue ? DateTime.MaxValue : date.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
    private static DoctorSchedule Clone(DoctorSchedule s) => new()
    {
        Id = s.Id, DoctorId = s.DoctorId, DayOfWeek = s.DayOfWeek, StartTime = s.StartTime, EndTime = s.EndTime,
        BreakStart = s.BreakStart, BreakEnd = s.BreakEnd, SlotMinutes = s.SlotMinutes,
        EffectiveFrom = s.EffectiveFrom, EffectiveTo = s.EffectiveTo, IsActive = s.IsActive
    };
    private static string? Validate(object request)
    {
        var errors = new List<ValidationResult>();
        return Validator.TryValidateObject(request, new ValidationContext(request), errors, true) ? null : string.Join(" ", errors.Select(e => e.ErrorMessage));
    }
    private static PagedResult<T> Page<T>(List<T> items, int total, int page, int pageSize)
        => new() { Items = items, TotalItems = total, Page = page, PageSize = pageSize, TotalPages = (int)Math.Ceiling(total / (double)pageSize) };
    private static HttpResponseData<T> Response<T>(int code, string message, T? content = default)
        => new() { StatusCode = code, Message = message, Content = content };
    private static AdminDoctorScheduleDTO Map(DoctorSchedule s) => new()
    {
        Id = s.Id, DoctorId = s.DoctorId, DoctorName = s.Doctor?.FullName ?? string.Empty, DayOfWeek = s.DayOfWeek,
        StartTime = s.StartTime, EndTime = s.EndTime, BreakStart = s.BreakStart, BreakEnd = s.BreakEnd,
        SlotMinutes = s.SlotMinutes, EffectiveFrom = s.EffectiveFrom, EffectiveTo = s.EffectiveTo, IsActive = s.IsActive
    };
    private static AdminDoctorTimeOffDTO Map(DoctorTimeOff t) => new()
    {
        Id = t.Id, DoctorId = t.DoctorId, DoctorName = t.DoctorId == null ? "Cả phòng khám" : t.Doctor?.FullName ?? string.Empty,
        StartAt = t.StartAt, EndAt = t.EndAt, Type = t.Type, Reason = t.Reason, IsFullDay = t.IsFullDay, ApprovedBy = t.ApprovedBy
    };
    private static AffectedAppointmentDTO Map(Appointment a) => new()
    {
        Id = a.Id, AppointmentCode = a.AppointmentCode, DoctorId = a.DoctorId, DoctorName = a.Doctor?.FullName ?? string.Empty,
        PatientName = a.Patient?.FullName ?? string.Empty, StartTime = a.StartTime, EndTime = a.EndTime, Status = a.Status
    };
}
