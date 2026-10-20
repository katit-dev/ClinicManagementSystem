using System.ComponentModel.DataAnnotations;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Doctor;
using ClinicManagementSystem.Application.Helpers;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Application.Services;

public interface IDoctorScheduleService
{
    Task<HttpResponseData<DoctorScheduleDTO>> GetMyScheduleAsync(DoctorScheduleQueryDTO query, int userId);
    Task<HttpResponseData<PagedResult<DoctorTimeOffDTO>>> GetMyTimeOffAsync(DoctorTimeOffQueryDTO query, int userId);
}

public sealed class DoctorScheduleService(IUnitOfWork uow, ILogger<DoctorScheduleService> logger, TimeProvider? clock = null) : IDoctorScheduleService
{
    public async Task<HttpResponseData<DoctorScheduleDTO>> GetMyScheduleAsync(DoctorScheduleQueryDTO query, int userId)
    {
        if (userId <= 0) return Result<DoctorScheduleDTO>(401, "Vui lòng đăng nhập lại.");
        var error = Validate(query);
        if (error != null) return Result<DoctorScheduleDTO>(400, error);
        try
        {
            var doctor = await uow.DoctorRepository.GetActiveByUserIdAsync(userId);
            if (doctor == null) return Result<DoctorScheduleDTO>(403, "Tài khoản chưa có hồ sơ bác sĩ đang hoạt động.");
            var now = DoctorSlotGenerator.ClinicNow(clock);
            var today = DateOnly.FromDateTime(now);
            var from = query.From ?? today.AddDays(-((int)today.DayOfWeek + 6) % 7);
            var to = query.To ?? from.AddDays(6);
            var schedules = await uow.DoctorScheduleRepository.GetInRangeAsync(doctor.Id, from, to);
            var timeOff = await uow.DoctorTimeOffRepository.GetOverlappingAsync(doctor.Id, from.ToDateTime(TimeOnly.MinValue), to.AddDays(1).ToDateTime(TimeOnly.MinValue));
            return Result(200, "Đã tải lịch làm việc.", new DoctorScheduleDTO
            {
                From = from, To = to, ClinicNow = now, DoctorName = doctor.FullName,
                Days = DoctorScheduleTimeline.Build(doctor.Id, from, to, schedules, timeOff, now)
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read schedule for user {UserId}", userId);
            return Result<DoctorScheduleDTO>(500, "Không thể tải lịch làm việc. Vui lòng thử lại.");
        }
    }

    public async Task<HttpResponseData<PagedResult<DoctorTimeOffDTO>>> GetMyTimeOffAsync(DoctorTimeOffQueryDTO query, int userId)
    {
        if (userId <= 0) return Result<PagedResult<DoctorTimeOffDTO>>(401, "Vui lòng đăng nhập lại.");
        var error = Validate(query);
        if (error != null) return Result<PagedResult<DoctorTimeOffDTO>>(400, error);
        try
        {
            var doctor = await uow.DoctorRepository.GetActiveByUserIdAsync(userId);
            if (doctor == null) return Result<PagedResult<DoctorTimeOffDTO>>(403, "Tài khoản chưa có hồ sơ bác sĩ đang hoạt động.");
            var now = DoctorSlotGenerator.ClinicNow(clock);
            var (items, total) = await uow.DoctorTimeOffRepository.GetDoctorPageAsync(doctor.Id,
                query.From?.ToDateTime(TimeOnly.MinValue), query.To?.AddDays(1).ToDateTime(TimeOnly.MinValue),
                query.Scope, query.Type, query.Page, query.PageSize);
            return Result(200, "Đã tải lịch nghỉ.", new PagedResult<DoctorTimeOffDTO>
            {
                Page = query.Page, PageSize = query.PageSize, TotalItems = total,
                TotalPages = (int)Math.Ceiling((double)total / query.PageSize),
                Items = items.Select(t => DoctorScheduleTimeline.Map(t, now)).ToList()
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read time off for user {UserId}", userId);
            return Result<PagedResult<DoctorTimeOffDTO>>(500, "Không thể tải lịch nghỉ. Vui lòng thử lại.");
        }
    }

    private static string? Validate(object query)
    {
        var errors = new List<ValidationResult>();
        return Validator.TryValidateObject(query, new ValidationContext(query), errors, true) ? null : errors[0].ErrorMessage ?? "Bộ lọc không hợp lệ.";
    }
    private static HttpResponseData<T> Result<T>(int code, string message, T? content = default)
        => new() { StatusCode = code, Message = message, Content = content };
}
