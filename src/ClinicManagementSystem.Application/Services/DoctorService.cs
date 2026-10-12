using ClinicManagementSystem.Application.Helpers;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Appointment;
using ClinicManagementSystem.Application.DTOs.Doctor;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Application.Services;


public interface IDoctorService
{
    Task<HttpResponseData<List<DoctorDTO>>>
        GetDoctorsBySpecialtyAsync(
            int specialtyId);

    Task<HttpResponseData<List<SlotDTO>>>
        GetAvailableSlotsAsync(
            int doctorId,
            DateOnly date);

    // GET CURRENT DOCTOR
    Task<HttpResponseData<DoctorDTO>>
    GetCurrentDoctorAsync(
        int currentUserId);

}


public class DoctorService : IDoctorService
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly ILogger<DoctorService> _logger;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public DoctorService(
        IUnitOfWork unitOfWork,
        ILogger<DoctorService> logger)
    {
        _unitOfWork = unitOfWork;

        _logger = logger;
    }

    // =====================================================
    // GET CURRENT DOCTOR
    //
    // Lấy Doctor tương ứng với User đang đăng nhập.
    //
    // currentUserId:
    // ClaimTypes.NameIdentifier
    //
    // Quan hệ:
    // User.Id
    //     ↓
    // Doctor.UserId
    //     ↓
    // Doctor.Id
    // =====================================================

    public async Task<
        HttpResponseData<DoctorDTO>>
        GetCurrentDoctorAsync(
            int currentUserId)
    {
        try
        {
            // =================================================
            // VALIDATE USER ID
            // =================================================

            if (currentUserId <= 0)
            {
                return new HttpResponseData<DoctorDTO>
                {
                    StatusCode = 400,

                    Message =
                        DoctorResponseMessageDTO
                            .DoctorNotFound,

                    Content = null
                };
            }


            // =================================================
            // GET CURRENT DOCTOR
            // =================================================

            var doctor =
                await _unitOfWork
                    .DoctorRepository
                    .WhereSql(
                        d =>
                            d.UserId == currentUserId &&
                            d.IsActive
                    )
                    .Select(
                        d =>
                            new DoctorDTO
                            {
                                Id =
                                    d.Id,

                                SpecialtyId =
                                    d.SpecialtyId,

                                FullName =
                                    d.FullName,

                                Title =
                                    d.Title,

                                Room =
                                    d.Room,

                                ConsultationFee =
                                    d.ConsultationFee,

                                AvatarUrl =
                                    d.AvatarUrl,

                                ExperienceYears =
                                    d.ExperienceYears
                            }
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // DOCTOR NOT FOUND
            // =================================================

            if (doctor == null)
            {
                return new HttpResponseData<DoctorDTO>
                {
                    StatusCode = 404,

                    Message =
                        DoctorResponseMessageDTO
                            .DoctorNotFound,

                    Content = null
                };
            }


            // =================================================
            // SUCCESS
            // =================================================

            return new HttpResponseData<DoctorDTO>
            {
                StatusCode = 200,

                Message =
                    DoctorResponseMessageDTO
                        .GetCurrentDoctorSuccess,

                Content = doctor
            };
        }
        catch (Exception ex)
        {
            // =================================================
            // LOG ERROR
            // =================================================

            _logger.LogError(
                ex,
                "Failed to get current doctor. " +
                "UserId: {UserId}",
                currentUserId
            );


            // =================================================
            // FAILED
            // =================================================

            return new HttpResponseData<DoctorDTO>
            {
                StatusCode = 500,

                Message =
                    DoctorResponseMessageDTO
                        .GetCurrentDoctorFailed,

                Content = null
            };
        }
    }


    // =====================================================
    // GET AVAILABLE SLOTS
    // =====================================================

    public async Task<HttpResponseData<List<SlotDTO>>> GetAvailableSlotsAsync(int doctorId, DateOnly date)
    {
        try
        {
            if (doctorId <= 0)
                return SlotResponse(400, DoctorResponseMessageDTO.DoctorNotFound);
            var now = DoctorSlotGenerator.ClinicNow();
            if (date < DateOnly.FromDateTime(now) || date == DateOnly.MaxValue)
                return SlotResponse(400, DoctorResponseMessageDTO.InvalidAppointmentDate);
            var doctor = await _unitOfWork.DoctorRepository.GetByIdAsync(doctorId);
            if (doctor == null || !doctor.IsActive)
                return SlotResponse(404, DoctorResponseMessageDTO.DoctorNotFound);
            var schedules = await _unitOfWork.DoctorScheduleRepository.GetEffectiveAsync(doctorId, date);
            if (schedules.Count == 0)
                return SlotResponse(200, DoctorResponseMessageDTO.GetAvailableSlotsSuccess, new List<SlotDTO>());
            var start = date.ToDateTime(TimeOnly.MinValue);
            var end = date.AddDays(1).ToDateTime(TimeOnly.MinValue);
            var timeOffs = await _unitOfWork.DoctorTimeOffRepository.GetOverlappingAsync(doctorId, start, end);
            var booked = await _unitOfWork.AppointmentRepository.GetBlockingInRangeAsync(doctorId, start, end);
            var slots = DoctorSlotGenerator.Generate(date, schedules, timeOffs, booked, now);
            return SlotResponse(200, DoctorResponseMessageDTO.GetAvailableSlotsSuccess, slots);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available slots. DoctorId: {DoctorId}, Date: {Date}", doctorId, date);
            return SlotResponse(500, DoctorResponseMessageDTO.GetAvailableSlotsFailed);
        }
    }

    // =====================================================
    // GET DOCTORS BY SPECIALTY
    // =====================================================

    public async Task<
        HttpResponseData<List<DoctorDTO>>>
        GetDoctorsBySpecialtyAsync(
            int specialtyId)
    {
        try
        {
            // =================================================
            // VALIDATE SPECIALTY ID
            // =================================================

            if (specialtyId <= 0)
            {
                return Response(
                    400,
                    DoctorResponseMessageDTO
                        .InvalidSpecialty
                );
            }


            // =================================================
            // QUERY DOCTORS
            //
            // Chỉ lấy:
            //
            // - đúng chuyên khoa
            // - bác sĩ đang hoạt động
            // =================================================

            var doctors =
                await _unitOfWork
                    .DoctorRepository
                    .WhereSql(
                        d =>
                            d.SpecialtyId == specialtyId &&
                            d.IsActive
                    )
                    .OrderBy(
                        d => d.FullName
                    )
                    .Select(
                        d => new DoctorDTO
                        {
                            Id =
                                d.Id,

                            SpecialtyId =
                                d.SpecialtyId,

                            FullName =
                                d.FullName,

                            Title =
                                d.Title,

                            Room =
                                d.Room,

                            ConsultationFee =
                                d.ConsultationFee,

                            AvatarUrl =
                                d.AvatarUrl,

                            ExperienceYears =
                                d.ExperienceYears
                        }
                    )
                    .ToListAsync();


            // =================================================
            // SUCCESS
            //
            // Không có Doctor:
            //
            // vẫn trả 200 + []
            // =================================================

            return Response(
                200,
                DoctorResponseMessageDTO
                    .GetListSuccess,
                doctors
            );
        }
        catch (Exception ex)
        {
            // =================================================
            // LOG ERROR
            // =================================================

            _logger.LogError(
                ex,
                "Failed to get doctors by specialty. " +
                "SpecialtyId: {SpecialtyId}",
                specialtyId
            );


            // =================================================
            // FAILED
            // =================================================

            return Response(
                500,
                DoctorResponseMessageDTO
                    .GetListFailed
            );
        }
    }


    // =====================================================
    // DOCTOR RESPONSE
    // =====================================================

    private static
        HttpResponseData<List<DoctorDTO>>
        Response(
            int statusCode,
            string message,
            List<DoctorDTO>? content = null)
    {
        return new HttpResponseData<List<DoctorDTO>>
        {
            StatusCode =
                statusCode,

            Message =
                message,

            Content =
                content ??
                new List<DoctorDTO>()
        };
    }


    // =====================================================
    // SLOT RESPONSE
    // =====================================================

    private static
        HttpResponseData<List<SlotDTO>>
        SlotResponse(
            int statusCode,
            string message,
            List<SlotDTO>? content = null)
    {
        return new HttpResponseData<List<SlotDTO>>
        {
            StatusCode =
                statusCode,

            Message =
                message,

            Content =
                content ??
                new List<SlotDTO>()
        };
    }
}