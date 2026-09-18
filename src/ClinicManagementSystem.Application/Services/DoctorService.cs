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
            // CURRENT DATE
            // =================================================

            var today =
                DateOnly.FromDateTime(
                    DateTime.Now
                );


            // =================================================
            // VALIDATE APPOINTMENT DATE
            // =================================================

            if (date < today)
            {
                return SlotResponse(
                    400,
                    DoctorResponseMessageDTO
                        .InvalidAppointmentDate
                );
            }


            // =================================================
            // SELECTED DAY RANGE
            //
            // Ví dụ:
            //
            // date = 2026-09-21
            //
            // dayStart = 2026-09-21 00:00
            // dayEnd   = 2026-09-22 00:00
            //
            // Dùng để kiểm tra DoctorTimeOff
            // có overlap với ngày đang chọn hay không.
            // =================================================

            var dayStart =
                date.ToDateTime(
                    TimeOnly.MinValue
                );

            var dayEnd =
                date
                    .AddDays(1)
                    .ToDateTime(
                        TimeOnly.MinValue
                    );


            // =================================================
            // CHECK DOCTOR
            // =================================================

            var doctor =
                await _unitOfWork
                    .DoctorRepository
                    .WhereSql(
                        d =>
                            d.Id == doctorId &&
                            d.IsActive
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // DOCTOR NOT FOUND
            // =================================================

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
            // Wednesday = 3
            // Thursday  = 4
            // Friday    = 5
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
            // - đúng Doctor
            // - đúng thứ
            // - schedule active
            // - đã tới EffectiveFrom
            // - chưa quá EffectiveTo
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
            //
            // Trả:
            //
            // 200 + []
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
            // GET DOCTOR TIME OFF
            //
            // DoctorId == doctorId
            //      → nghỉ riêng của Doctor
            //
            // DoctorId == null
            //      → nghỉ chung
            //
            // Công thức overlap:
            //
            // TimeOff.Start < Day.End
            // &&
            // TimeOff.End > Day.Start
            // =================================================

            var timeOffs =
                await _unitOfWork
                    .DoctorTimeOffRepository
                    .WhereSql(
                        t =>
                            (
                                t.DoctorId == doctorId ||
                                t.DoctorId == null
                            ) &&
                            t.StartAt < dayEnd &&
                            t.EndAt > dayStart
                    )
                    .ToListAsync();

            // =================================================
            // GET EXISTING APPOINTMENTS
            //
            // Chỉ lấy:
            // - đúng Doctor
            // - Appointment còn giữ chỗ: Status < 4
            // - có overlap với ngày đang chọn
            // =================================================

            var appointments =
                await _unitOfWork
                    .AppointmentRepository
                    .WhereSql(
                        a =>
                            a.DoctorId == doctorId &&
                            a.Status < 4 &&
                            a.StartTime < dayEnd &&
                            a.EndTime > dayStart
                    )
                    .ToListAsync();


            // =================================================
            // GENERATE SLOTS
            // =================================================

            var slots =
                new List<SlotDTO>();


            // =================================================
            // CURRENT TIME POINTER
            //
            // Ví dụ:
            //
            // StartTime = 08:00
            //
            // currentTime bắt đầu = 08:00
            // =================================================

            var currentTime =
                schedule.StartTime;


            // =================================================
            // SLOT LOOP
            //
            // Ví dụ:
            //
            // currentTime = 08:00
            // SlotMinutes = 30
            //
            // 08:00 + 30 = 08:30
            //
            // Nếu:
            //
            // 08:30 <= EndTime
            //
            // thì tạo slot.
            // =================================================

            while (
                currentTime
                    .AddMinutes(
                        schedule.SlotMinutes
                    )
                <= schedule.EndTime
            )
            {
                // =============================================
                // SLOT START / END TIME
                //
                // TimeOnly
                //
                // Ví dụ:
                //
                // slotStartTime = 08:00
                // slotEndTime   = 08:30
                // =============================================

                var slotStartTime =
                    currentTime;

                var slotEndTime =
                    currentTime.AddMinutes(
                        schedule.SlotMinutes
                    );


                // =============================================
                // SLOT DATE TIME
                //
                // Ghép:
                //
                // date
                // +
                // TimeOnly
                //
                // thành DateTime.
                //
                // Ví dụ:
                //
                // 2026-09-21
                // +
                // 09:00
                //
                // =
                //
                // 2026-09-21 09:00
                // =============================================

                var slotStart =
                    date.ToDateTime(
                        slotStartTime
                    );

                var slotEnd =
                    date.ToDateTime(
                        slotEndTime
                    );


                // =============================================
                // CHECK BREAK
                //
                // Ví dụ:
                //
                // Break:
                // 12:00 - 13:30
                //
                // Slot:
                // 12:30 - 13:00
                //
                // → overlap
                // → isBreakTime = true
                // =============================================

                var isBreakTime =
                    schedule.BreakStart.HasValue &&
                    schedule.BreakEnd.HasValue &&
                    slotStartTime <
                        schedule.BreakEnd.Value &&
                    slotEndTime >
                        schedule.BreakStart.Value;


                // =============================================
                // CHECK DOCTOR TIME OFF
                //
                // Công thức overlap:
                //
                // Slot.Start < TimeOff.End
                // &&
                // Slot.End > TimeOff.Start
                //
                // Ví dụ:
                //
                // TimeOff:
                // 09:00 - 10:00
                //
                // Slot:
                // 09:00 - 09:30
                //
                // → isTimeOff = true
                // =============================================

                var isTimeOff =
                    timeOffs.Any(
                        t =>
                            slotStart < t.EndAt &&
                            slotEnd > t.StartAt
                    );

                // =============================================
                // CHECK EXISTING APPOINTMENT
                // =============================================

                var isBooked =
                    appointments.Any(
                        a =>
                            slotStart < a.EndTime &&
                            slotEnd > a.StartTime
                    );

                // =============================================
                // ADD AVAILABLE SLOT
                //
                // Chỉ add nếu:
                //
                // - không nằm trong giờ nghỉ Break
                // - không nằm trong DoctorTimeOff
                // =============================================

                if (
                    !isBreakTime &&
                    !isTimeOff
                )
                {
                    slots.Add(
                        new SlotDTO
                        {
                            StartTime =
                                slotStart,

                            EndTime =
                                slotEnd,

                            IsAvailable =
                                true
                        }
                    );
                }


                // =============================================
                // NEXT SLOT
                //
                // Ví dụ:
                //
                // slot hiện tại:
                // 08:00 - 08:30
                //
                // currentTime = 08:30
                //
                // vòng sau:
                // 08:30 - 09:00
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
            // =================================================
            // LOG ERROR
            // =================================================

            _logger.LogError(
                ex,
                "Failed to get available slots. " +
                "DoctorId: {DoctorId}, Date: {Date}",
                doctorId,
                date
            );


            // =================================================
            // FAILED
            // =================================================

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