using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Appointment;
using ClinicManagementSystem.Application.DTOs.Doctor;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Application.Services;


public interface IDoctorService
{
    Task<HttpResponseData<List<DoctorDTO>>> GetDoctorsBySpecialtyAsync(int specialtyId);
    Task<HttpResponseData<List<SlotDTO>>> GetAvailableSlotsAsync(int doctorId, DateOnly date);
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
    // GET AVAILABLE SLOTS
    // =====================================================

    public async Task<
        HttpResponseData<List<SlotDTO>>>
        GetAvailableSlotsAsync(
            int doctorId,
            DateOnly date)
    {
        try
        {
            // =================================================
            // VALIDATE DOCTOR ID
            // =================================================

            if (doctorId <= 0)
            {
                return SlotResponse(
                    400,
                    DoctorResponseMessageDTO
                        .DoctorNotFound
                );
            }


            // =================================================
            // VALIDATE DATE
            // =================================================

            var today =
                DateOnly.FromDateTime(
                    DateTime.Now
                );


            if (date < today)
            {
                return SlotResponse(
                    400,
                    DoctorResponseMessageDTO
                        .InvalidAppointmentDate
                );
            }


            // =================================================
            // CHECK DOCTOR
            // =================================================

            var doctor =
                await _unitOfWork.DoctorRepository
                    .WhereSql(
                        d =>
                            d.Id == doctorId &&
                            d.IsActive
                    )
                    .FirstOrDefaultAsync();


            if (doctor == null)
            {
                return SlotResponse(
                    404,
                    DoctorResponseMessageDTO
                        .DoctorNotFound
                );
            }


            // =================================================
            // GET DAY OF WEEK
            //
            // .NET:
            //
            // Sunday    = 0
            // Monday    = 1
            // Tuesday   = 2
            // ...
            // Saturday  = 6
            // =================================================

            var dayOfWeek =
                (byte)date
                    .ToDateTime(
                        TimeOnly.MinValue
                    )
                    .DayOfWeek;


            // =================================================
            // FIND ACTIVE DOCTOR SCHEDULE
            //
            // Điều kiện:
            //
            // đúng Doctor
            // đúng thứ
            // IsActive
            // date >= EffectiveFrom
            //
            // EffectiveTo:
            // null hoặc date <= EffectiveTo
            // =================================================

            var schedule =
                await _unitOfWork
                    .DoctorScheduleRepository
                    .WhereSql(
                        s =>
                            s.DoctorId == doctorId &&
                            s.DayOfWeek == dayOfWeek &&
                            s.IsActive &&
                            s.EffectiveFrom <= date &&
                            (
                                !s.EffectiveTo.HasValue ||
                                s.EffectiveTo.Value >= date
                            )
                    )
                    .OrderByDescending(
                        s => s.EffectiveFrom
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // DOCTOR KHÔNG LÀM NGÀY NÀY
            //
            // Không phải lỗi.
            // Trả 200 + []
            // =================================================

            if (schedule == null)
            {
                return SlotResponse(
                    200,
                    DoctorResponseMessageDTO
                        .GetAvailableSlotsSuccess,
                    new List<SlotDTO>()
                );
            }


            // =================================================
            // GENERATE SLOTS
            // =================================================

            var slots =
                new List<SlotDTO>();


            var currentTime =
                schedule.StartTime;


            while (
                currentTime
                    .AddMinutes(
                        schedule.SlotMinutes
                    )
                <= schedule.EndTime)
            {
                // =============================================
                // SLOT START / END
                // =============================================

                var slotStartTime =
                    currentTime;

                var slotEndTime =
                    currentTime.AddMinutes(
                        schedule.SlotMinutes
                    );


                // =============================================
                // CHECK BREAK
                // =============================================

                var isBreakTime =
                    schedule.BreakStart.HasValue &&
                    schedule.BreakEnd.HasValue &&
                    slotStartTime <
                        schedule.BreakEnd.Value &&
                    slotEndTime >
                        schedule.BreakStart.Value;


                // =============================================
                // ADD SLOT IF NOT IN BREAK
                // =============================================

                if (!isBreakTime)
                {
                    slots.Add(
                        new SlotDTO
                        {
                            StartTime =
                                date.ToDateTime(
                                    slotStartTime
                                ),

                            EndTime =
                                date.ToDateTime(
                                    slotEndTime
                                ),

                            IsAvailable =
                                true
                        }
                    );
                }


                // =============================================
                // NEXT SLOT
                // =============================================

                currentTime =
                    slotEndTime;
            }


            // =================================================
            // SUCCESS
            // =================================================

            return SlotResponse(
                200,
                DoctorResponseMessageDTO
                    .GetAvailableSlotsSuccess,
                slots
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get available slots. " +
                "DoctorId: {DoctorId}, Date: {Date}",
                doctorId,
                date
            );


            return SlotResponse(
                500,
                DoctorResponseMessageDTO
                    .GetAvailableSlotsFailed
            );
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
            // - đúng chuyên khoa
            // - bác sĩ đang hoạt động
            // =================================================

            var doctors =
                await _unitOfWork.DoctorRepository
                    .WhereSql(
                        d =>
                            d.SpecialtyId == specialtyId &&
                            d.IsActive
                    )
                    .OrderBy(d => d.FullName)
                    .Select(
                        d => new DoctorDTO
                        {
                            Id = d.Id,

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
            // Không có bác sĩ vẫn trả 200 + []
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
    // RESPONSE
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
            StatusCode = statusCode,

            Message = message,

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
            StatusCode = statusCode,

            Message = message,

            Content =
                content ??
                new List<SlotDTO>()
        };
    }
}