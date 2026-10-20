using System.ComponentModel.DataAnnotations;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Doctor;
using ClinicManagementSystem.Application.Helpers;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Application.Services;

public interface IDoctorMedicalRecordService
{
    Task<HttpResponseData<PagedResult<DoctorMedicalRecordDTO>>> GetMyRecordsAsync(DoctorMedicalRecordQueryDTO query, int userId);
}

public sealed class DoctorMedicalRecordService(IUnitOfWork uow, ILogger<DoctorMedicalRecordService> logger) : IDoctorMedicalRecordService
{
    public async Task<HttpResponseData<PagedResult<DoctorMedicalRecordDTO>>> GetMyRecordsAsync(DoctorMedicalRecordQueryDTO query, int userId)
    {
        if (userId <= 0) return Result(401, "Vui lòng đăng nhập lại.");
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(query, new ValidationContext(query), errors, true))
            return Result(400, errors[0].ErrorMessage ?? "Bộ lọc không hợp lệ.");
        try
        {
            var doctor = await uow.DoctorRepository.GetActiveByUserIdAsync(userId);
            if (doctor == null) return Result(403, "Tài khoản chưa có hồ sơ bác sĩ đang hoạt động.");
            var (items, total) = await uow.MedicalRecordRepository.GetDoctorPageAsync(doctor.Id,
                query.Keyword?.Trim(), query.Status, query.From, query.To, query.Page, query.PageSize);
            var result = new PagedResult<DoctorMedicalRecordDTO>
            {
                Page = query.Page, PageSize = query.PageSize, TotalItems = total,
                TotalPages = (int)Math.Ceiling((double)total / query.PageSize),
                Items = items.Select(m =>
                {
                    var reason = MedicalRecordEditPolicy.ReadOnlyReason(m.Status, m.Appointment.Status);
                    return new DoctorMedicalRecordDTO
                    {
                        Id = m.Id, AppointmentId = m.AppointmentId, PatientId = m.PatientId,
                        PatientCode = m.Patient.PatientCode, PatientName = m.Patient.FullName,
                        AppointmentStartTime = m.Appointment.StartTime, AppointmentStatus = m.Appointment.Status,
                        Status = m.Status, CreatedAt = m.CreatedAt, FinalizedAt = m.FinalizedAt,
                        CanContinue = reason == null, ReadOnlyReason = reason
                    };
                }).ToList()
            };
            return Result(200, "Lấy danh sách bệnh án thành công.", result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not load own medical records for user {UserId}", userId);
            return Result(500, "Không thể tải danh sách bệnh án. Vui lòng thử lại.");
        }
    }

    private static HttpResponseData<PagedResult<DoctorMedicalRecordDTO>> Result(int status, string message,
        PagedResult<DoctorMedicalRecordDTO>? content = null) => new() { StatusCode = status, Message = message, Content = content };
}
