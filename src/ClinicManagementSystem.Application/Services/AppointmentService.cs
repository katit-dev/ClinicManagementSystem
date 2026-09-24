using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Appointment;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Application.Enums;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Application.Services;


// =====================================================
// APPOINTMENT SERVICE CONTRACT
// =====================================================

public interface IAppointmentService
{
    Task<HttpResponseData<AppointmentDTO>> CreateAppointmentAsync(CreateAppointmentRequestDTO request, int currentUserId);
    Task<HttpResponseData<List<MyAppointmentDTO>>> GetMyAppointmentsAsync(int currentUserId, MyAppointmentFilter filter);
    Task<HttpResponseData<AppointmentDTO>> CancelAppointmentAsync(int appointmentId, CancelAppointmentRequestDTO request, int currentUserId);
    Task<HttpResponseData<AppointmentDTO>> RescheduleAppointmentAsync(int appointmentId, RescheduleAppointmentRequestDTO request, int currentUserId);

    // =====================================================
    // RECEPTION APPOINTMENTS
    // =====================================================
    Task<HttpResponseData<List<ReceptionAppointmentDTO>>> GetReceptionAppointmentsAsync(DateOnly date, int? doctorId, int? specialtyId, AppointmentStatus? status);



}


public class AppointmentService : IAppointmentService
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly IDoctorService _doctorService;

    private readonly ILogger<AppointmentService> _logger;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public AppointmentService(IUnitOfWork unitOfWork, IDoctorService doctorService, ILogger<AppointmentService> logger)
    {
        _unitOfWork = unitOfWork;

        _doctorService = doctorService;

        _logger = logger;
    }

    // =====================================================
    // GET RECEPTION APPOINTMENTS
    // =====================================================

    public async Task<HttpResponseData<List<ReceptionAppointmentDTO>>> GetReceptionAppointmentsAsync(
        DateOnly date,
        int? doctorId,
        int? specialtyId,
        AppointmentStatus? status)
    {
        try
        {
            // =================================================
            // DATE RANGE
            // =================================================

            var startOfDay =
                date.ToDateTime(
                    TimeOnly.MinValue
                );

            var endOfDay =
                startOfDay.AddDays(1);


            // =================================================
            // BASE QUERY
            // =================================================

            var query =
                _unitOfWork
                    .AppointmentRepository
                    .WhereSql(
                        a =>
                            a.StartTime >= startOfDay &&
                            a.StartTime < endOfDay
                    );


            // =================================================
            // FILTER BY DOCTOR
            // =================================================

            if (doctorId.HasValue)
            {
                query =
                    query.Where(
                        a =>
                            a.DoctorId == doctorId.Value
                    );
            }


            // =================================================
            // FILTER BY SPECIALTY
            // =================================================

            if (specialtyId.HasValue)
            {
                query =
                    query.Where(
                        a =>
                            a.Doctor.SpecialtyId == specialtyId.Value
                    );
            }


            // =================================================
            // FILTER BY STATUS
            // =================================================

            if (status.HasValue)
            {
                query =
                    query.Where(
                        a =>
                            a.Status == (byte)status.Value
                    );
            }


            // =================================================
            // EXECUTE QUERY
            // =================================================

            var appointments =
                await query
                    .OrderBy(
                        a => a.StartTime
                    )
                    .ToListAsync();


            // =================================================
            // SUCCESS
            //
            // Chưa map ReceptionAppointmentDTO ở bước này.
            // =================================================

            return new HttpResponseData<List<ReceptionAppointmentDTO>>
            {
                StatusCode = 200,

                Message =
                    $"Tìm thấy {appointments.Count} lịch hẹn trong ngày.",

                Content =
                    new List<ReceptionAppointmentDTO>()
            };
        }
        catch (Exception ex)
        {
            // =================================================
            // LOG ERROR
            // =================================================

            _logger.LogError(
                ex,
                "Failed to get reception appointments. " +
                "Date: {Date}, " +
                "DoctorId: {DoctorId}, " +
                "SpecialtyId: {SpecialtyId}, " +
                "Status: {Status}",
                date,
                doctorId,
                specialtyId,
                status
            );


            // =================================================
            // ERROR RESPONSE
            // =================================================

            return new HttpResponseData<List<ReceptionAppointmentDTO>>
            {
                StatusCode = 500,

                Message =
                    "Không thể lấy danh sách lịch hẹn.",

                Content =
                    new List<ReceptionAppointmentDTO>()
            };
        }
    }
    // =====================================================
    // RESCHEDULE APPOINTMENT
    // =====================================================

    public async Task<HttpResponseData<AppointmentDTO>> RescheduleAppointmentAsync(int appointmentId, RescheduleAppointmentRequestDTO request, int currentUserId)
    {
        bool transactionStarted = false;
        try
        {
            // =================================================
            // GET CURRENT PATIENT
            // =================================================

            var patient =
                await _unitOfWork
                    .PatientRepository
                    .WhereSql(
                        p =>
                            p.UserId == currentUserId &&
                            p.IsActive
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // PATIENT NOT FOUND
            // =================================================

            if (patient == null)
            {
                return Response(
                    404,
                    AppointmentResponseMessageDTO
                        .PatientNotFound
                );
            }


            // =================================================
            // GET APPOINTMENT
            //
            // Chỉ lấy Appointment thuộc Patient hiện tại.
            // =================================================

            var appointment =
                await _unitOfWork
                    .AppointmentRepository
                    .WhereSql(
                        a =>
                            a.Id == appointmentId &&
                            a.PatientId == patient.Id
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // APPOINTMENT NOT FOUND
            // =================================================

            if (appointment == null)
            {
                return Response(
                    404,
                    AppointmentResponseMessageDTO
                        .AppointmentNotFound
                );
            }


            // =================================================
            // CHECK RESCHEDULABLE STATUS
            //
            // Patient chỉ được đổi lịch khi:
            //
            // Pending
            // Confirmed
            //
            // Không cho đổi:
            //
            // CheckedIn
            // Completed
            // Cancelled
            // NoShow
            // =================================================

            var canReschedule =
                appointment.Status ==
                    (byte)AppointmentStatus.Pending
                ||
                appointment.Status ==
                    (byte)AppointmentStatus.Confirmed;


            if (!canReschedule)
            {
                return Response(
                    400,
                    AppointmentResponseMessageDTO
                        .CannotRescheduleAppointment
                );
            }


            // =================================================
            // CHECK CURRENT APPOINTMENT TIME
            //
            // Appointment hiện tại đã tới hoặc qua giờ khám
            // thì không cho Patient tự đổi nữa.
            // =================================================

            var now =
                DateTime.Now;

            if (appointment.StartTime <= now)
            {
                return Response(
                    400,
                    AppointmentResponseMessageDTO
                        .CannotRescheduleAppointment
                );
            }


            // =================================================
            // CHECK NEW START TIME
            //
            // Giờ mới phải nằm trong tương lai.
            // =================================================

            if (request.NewStartTime <= now)
            {
                return Response(
                    400,
                    AppointmentResponseMessageDTO
                        .InvalidAppointmentDate
                );
            }


            // =================================================
            // CHECK SAME TIME
            //
            // Không cần reschedule nếu giờ mới
            // giống hệt giờ hiện tại.
            // =================================================

            if (
                request.NewStartTime ==
                appointment.StartTime
            )
            {
                return Response(
                    400,
                    AppointmentResponseMessageDTO
                        .CannotRescheduleAppointment
                );
            }

            // =================================================
            // GET NEW APPOINTMENT DATE
            // =================================================

            var appointmentDate =
                DateOnly.FromDateTime(
                    request.NewStartTime
                );

            // =================================================
            // RE-CHECK AVAILABLE SLOTS
            // =================================================

            var availableSlotsResponse =
                await _doctorService
                    .GetAvailableSlotsAsync(
                        appointment.DoctorId,
                        appointmentDate
                    );

            // =================================================
            // FIND SELECTED SLOT
            // =================================================

            var selectedSlot =
                availableSlotsResponse.Content?
                    .FirstOrDefault(
                        slot =>
                            slot.StartTime ==
                            request.NewStartTime
                    );
            // =================================================
            // SLOT NOT AVAILABLE
            // =================================================

            if (selectedSlot == null)
            {
                return Response(
                    409,
                    AppointmentResponseMessageDTO
                        .SlotNotAvailable
                );
            }

            // =================================================
            // UPDATE APPOINTMENT TIME
            // =================================================

            appointment.StartTime =
                selectedSlot.StartTime;

            appointment.EndTime =
                selectedSlot.EndTime;

            appointment.UpdatedAt =
                DateTime.Now;


            // =================================================
            // BEGIN TRANSACTION
            // =================================================

            await _unitOfWork
                .BeginTransactionAsync();

            transactionStarted = true;


            // =================================================
            // RE-CHECK APPOINTMENT CONFLICT
            // =================================================

            var appointmentExists =
                await _unitOfWork
                    .AppointmentRepository
                    .WhereSql(
                        a =>
                            a.Id != appointment.Id &&

                            a.DoctorId ==
                                appointment.DoctorId &&

                            a.Status <
                                (byte)AppointmentStatus.Cancelled &&

                            a.StartTime <
                                appointment.EndTime &&

                            a.EndTime >
                                appointment.StartTime
                    )
                    .AnyAsync();


            // =================================================
            // SLOT NOT AVAILABLE
            // =================================================

            if (appointmentExists)
            {
                await _unitOfWork
                    .RollbackTransactionAsync();

                transactionStarted = false;

                return Response(
                    409,
                    AppointmentResponseMessageDTO
                        .SlotNotAvailable
                );
            }


            // =================================================
            // SAVE CHANGES
            // =================================================

            await _unitOfWork
                .SaveChangesAsync();


            // =================================================
            // COMMIT TRANSACTION
            // =================================================

            await _unitOfWork
                .CommitTransactionAsync();

            transactionStarted = false;


            // =================================================
            // SUCCESS
            // =================================================

            return Response(
                200,
                AppointmentResponseMessageDTO
                    .RescheduleSuccess
            );
        }
        catch (Exception ex)
        {
            // =================================================
            // ROLLBACK TRANSACTION
            // =================================================

            if (transactionStarted)
            {
                try
                {
                    await _unitOfWork
                        .RollbackTransactionAsync();
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(
                        rollbackEx,
                        "Failed to rollback appointment reschedule."
                    );
                }
            }


            // =================================================
            // LOG ERROR
            // =================================================

            _logger.LogError(
                ex,
                "Failed to reschedule appointment. " +
                "AppointmentId: {AppointmentId}, " +
                "UserId: {UserId}",
                appointmentId,
                currentUserId
            );


            return Response(
                500,
                AppointmentResponseMessageDTO
                    .RescheduleFailed
            );
        }
    }

    // =====================================================
    // CANCEL APPOINTMENT
    // =====================================================

    public async Task<HttpResponseData<AppointmentDTO>> CancelAppointmentAsync(int appointmentId, CancelAppointmentRequestDTO request, int currentUserId)
    {
        bool transactionStarted = false;

        try
        {
            // =================================================
            // GET CURRENT PATIENT
            // =================================================

            var patient =
                await _unitOfWork
                    .PatientRepository
                    .WhereSql(
                        p =>
                            p.UserId == currentUserId &&
                            p.IsActive
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // PATIENT NOT FOUND
            // =================================================

            if (patient == null)
            {
                return Response(
                    404,
                    AppointmentResponseMessageDTO
                        .PatientNotFound
                );
            }


            // =================================================
            // GET APPOINTMENT
            // =================================================

            var appointment =
                await _unitOfWork
                    .AppointmentRepository
                    .WhereSql(
                        a =>
                            a.Id == appointmentId &&
                            a.PatientId == patient.Id
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // APPOINTMENT NOT FOUND
            // =================================================

            if (appointment == null)
            {
                return Response(
                    404,
                    AppointmentResponseMessageDTO
                        .AppointmentNotFound
                );
            }


            // =================================================
            // CHECK CANCELLABLE STATUS
            //
            // Patient chỉ được tự hủy khi:
            //
            // Pending
            // Confirmed
            //
            // Không cho hủy:
            //
            // CheckedIn
            // Completed
            // Cancelled
            // NoShow
            // =================================================

            var canCancel =
                appointment.Status ==
                    (byte)AppointmentStatus.Pending
                ||
                appointment.Status ==
                    (byte)AppointmentStatus.Confirmed;


            if (!canCancel)
            {
                return Response(
                    400,
                    AppointmentResponseMessageDTO
                        .CannotCancelAppointment
                );
            }


            // =================================================
            // CHECK APPOINTMENT TIME
            //
            // Lịch đã tới hoặc đã qua
            // thì Patient không được tự hủy nữa.
            // =================================================

            if (
                appointment.StartTime <=
                DateTime.Now
            )
            {
                return Response(
                    400,
                    AppointmentResponseMessageDTO
                        .CannotCancelAppointment
                );
            }


            // =================================================
            // CURRENT STATUS
            //
            // Phải lưu lại trước khi đổi sang Cancelled
            // để ghi AppointmentStatusHistory.
            // =================================================

            var fromStatus =
                appointment.Status;

            // =================================================
            // UPDATE APPOINTMENT
            // =================================================

            var now =
                DateTime.Now;

            appointment.Status =
                (byte)AppointmentStatus.Cancelled;

            appointment.CancelReason =
                request.CancelReason.Trim();

            appointment.UpdatedAt =
                now;

            // =================================================
            // CREATE STATUS HISTORY
            // =================================================

            var statusHistory =
                new AppointmentStatusHistory
                {
                    AppointmentId =
                        appointment.Id,

                    FromStatus =
                        fromStatus,

                    ToStatus =
                        (byte)AppointmentStatus.Cancelled,

                    ChangedBy =
                        currentUserId,

                    Reason =
                        request.CancelReason.Trim(),

                    ChangedAt =
                        now
                };

            // =================================================
            // BEGIN TRANSACTION
            // =================================================

            await _unitOfWork.BeginTransactionAsync();

            transactionStarted = true;

            // =================================================
            // ADD STATUS HISTORY
            // =================================================

            await _unitOfWork.AppointmentStatusHistoryRepository.AddAsync(statusHistory);

            // =================================================
            // SAVE CHANGES
            // =================================================

            await _unitOfWork.SaveChangesAsync();

            // =================================================
            // COMMIT TRANSACTION
            // =================================================

            await _unitOfWork.CommitTransactionAsync();

            transactionStarted = false;

            // =================================================
            // SUCCESS
            // =================================================

            return Response(200, AppointmentResponseMessageDTO.CancelSuccess);

        }
        catch (Exception ex)
        {
            // =================================================
            // ROLLBACK TRANSACTION
            // =================================================

            if (transactionStarted)
            {
                try
                {
                    await _unitOfWork
                        .RollbackTransactionAsync();
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(
                        rollbackEx,
                        "Failed to rollback appointment cancellation."
                    );
                }
            }


            // =================================================
            // LOG ERROR
            // =================================================

            _logger.LogError(
                ex,
                "Failed to cancel appointment. " +
                "AppointmentId: {AppointmentId}, " +
                "UserId: {UserId}",
                appointmentId,
                currentUserId
            );


            return Response(
                500,
                AppointmentResponseMessageDTO
                    .CancelFailed
            );
        }
    }

    // =====================================================
    // GET MY APPOINTMENTS
    // =====================================================

    public async Task<HttpResponseData<List<MyAppointmentDTO>>> GetMyAppointmentsAsync(int currentUserId, MyAppointmentFilter filter)
    {
        try
        {
            // =================================================
            // GET CURRENT PATIENT
            // =================================================

            var patient =
                await _unitOfWork
                    .PatientRepository
                    .WhereSql(
                        p =>
                            p.UserId == currentUserId &&
                            p.IsActive
                    )
                    .FirstOrDefaultAsync();

            if (patient == null)
            {
                return MyAppointmentsResponse(
                    404,
                    AppointmentResponseMessageDTO
                        .PatientNotFound
                );
            }


            // =================================================
            // CURRENT TIME
            // =================================================

            var now =
                DateTime.Now;


            // =================================================
            // BASE QUERY
            //
            // Chỉ lấy Appointment của Patient đang đăng nhập.
            // =================================================

            var query =
                _unitOfWork
                    .AppointmentRepository
                    .WhereSql(
                        a =>
                            a.PatientId == patient.Id
                    );


            // =================================================
            // FILTER
            // =================================================

            switch (filter)
            {
                // =============================================
                // UPCOMING
                //
                // Pending   = 0
                // Confirmed = 1
                // CheckedIn = 2
                //
                // Và lịch chưa qua.
                // =============================================

                case MyAppointmentFilter.Upcoming:

                    query =
                        query.Where(
                            a =>
                                a.StartTime >= now &&
                                a.Status <
                                    (byte)
                                    AppointmentStatus.Completed
                        );

                    break;


                // =============================================
                // COMPLETED
                // =============================================

                case MyAppointmentFilter.Completed:

                    query =
                        query.Where(
                            a =>
                                a.Status ==
                                    (byte)
                                    AppointmentStatus.Completed
                        );

                    break;


                // =============================================
                // CANCELLED
                // =============================================

                case MyAppointmentFilter.Cancelled:

                    query =
                        query.Where(
                            a =>
                                a.Status ==
                                    (byte)
                                    AppointmentStatus.Cancelled
                        );

                    break;


                // =============================================
                // ALL
                //
                // Không thêm điều kiện.
                // =============================================

                case MyAppointmentFilter.All:

                default:
                    break;
            }


            // =================================================
            // QUERY + MAP DTO
            // =================================================

            var appointments =
                await query
                    .OrderByDescending(
                        a => a.StartTime
                    )
                    .Select(
                        a =>
                            new MyAppointmentDTO
                            {
                                Id =
                                    a.Id,

                                AppointmentCode =
                                    a.AppointmentCode,

                                DoctorId = a.DoctorId,

                                DoctorName =
                                    a.Doctor.FullName,

                                SpecialtyName =
                                    a.Doctor
                                        .Specialty
                                        .Name,

                                StartTime =
                                    a.StartTime,

                                EndTime =
                                    a.EndTime,

                                Status =
                                    a.Status,

                                QueueNumber =
                                    a.QueueNumber
                            }
                    )
                    .ToListAsync();


            // =================================================
            // UPCOMING
            //
            // Với lịch sắp tới:
            // lịch gần nhất nên nằm trên đầu.
            // =================================================

            if (
                filter ==
                MyAppointmentFilter.Upcoming
            )
            {
                appointments =
                    appointments
                        .OrderBy(
                            a => a.StartTime
                        )
                        .ToList();
            }


            // =================================================
            // SUCCESS
            // =================================================

            return MyAppointmentsResponse(
                200,
                AppointmentResponseMessageDTO
                    .GetMyAppointmentsSuccess,
                appointments
            );
        }
        catch (Exception ex)
        {
            // =================================================
            // LOG ERROR
            // =================================================

            _logger.LogError(
                ex,
                "Failed to get appointments for UserId: {UserId}",
                currentUserId
            );


            return MyAppointmentsResponse(
                500,
                AppointmentResponseMessageDTO
                    .GetMyAppointmentsFailed
            );
        }
    }


    // =====================================================
    // CREATE APPOINTMENT
    // =====================================================

    public async Task<
        HttpResponseData<AppointmentDTO>>
        CreateAppointmentAsync(
            CreateAppointmentRequestDTO request, int currentUserId)
    {
        bool transactionStarted = false;
        try
        {
            // =================================================
            // CHECK PATIENT
            // =================================================

            var patient =
         await _unitOfWork
        .PatientRepository
        .WhereSql(p => p.UserId == currentUserId && p.IsActive)
        .FirstOrDefaultAsync();

            if (patient == null)
            {
                return Response(
                    404,
                    AppointmentResponseMessageDTO
                        .PatientNotFound
                );
            }


            // =================================================
            // CHECK DOCTOR
            // =================================================

            var doctor =
                await _unitOfWork
                    .DoctorRepository
                    .WhereSql(
                        d =>
                            d.Id == request.DoctorId &&
                            d.IsActive
                    )
                    .FirstOrDefaultAsync();

            if (doctor == null)
            {
                return Response(
                    404,
                    AppointmentResponseMessageDTO
                        .DoctorNotFound
                );
            }

            // =================================================
            // GET SPECIALTY (lay ten chuyen khoa de bo vao AppointmentDTO, vi endpoint response la AppointmentDTO)
            // =================================================

            var specialty =
                await _unitOfWork
                    .SpecialtyRepository
                    .WhereSql(
                        s =>
                            s.Id == doctor.SpecialtyId
                    )
                    .FirstOrDefaultAsync();

            // =================================================
            // CHECK APPOINTMENT TIME
            // =================================================

            if (request.StartTime <= DateTime.Now)
            {
                return Response(
                    400,
                    AppointmentResponseMessageDTO
                        .InvalidAppointmentDate
                );
            }


            // =================================================
            // RE-CHECK AVAILABLE SLOT
            // =================================================

            var appointmentDate =
                DateOnly.FromDateTime(
                    request.StartTime
                );

            var availableSlotsResponse =
                await _doctorService
                    .GetAvailableSlotsAsync(
                        request.DoctorId,
                        appointmentDate
                    );


            var selectedSlot =
                availableSlotsResponse.Content?
                    .FirstOrDefault(
                        slot =>
                            slot.StartTime ==
                            request.StartTime
                    );


            // =================================================
            // SLOT NOT AVAILABLE
            // =================================================

            if (selectedSlot == null)
            {
                return Response(
                    409,
                    AppointmentResponseMessageDTO
                        .SlotNotAvailable
                );
            }

            // =================================================
            // PREPARE APPOINTMENT
            // =================================================

            var now =
                DateTime.UtcNow;


            // =================================================
            // GENERATE APPOINTMENT CODE
            // =================================================

            var appointmentCode =
                "APT" +
                Guid.NewGuid()
                    .ToString("N")[..17]
                    .ToUpperInvariant();


            // =================================================
            // CREATE APPOINTMENT ENTITY
            // =================================================

            var appointment =
                new Appointment
                {
                    PatientId =
                        patient.Id,

                    DoctorId =
                        request.DoctorId,

                    StartTime =
                        selectedSlot.StartTime,

                    EndTime =
                        selectedSlot.EndTime,

                    Status =
                        (byte)AppointmentStatus.Pending,

                    Reason =
                        request.Reason,

                    CreatedAt =
                        now,

                    AppointmentCode =
                        appointmentCode,

                    Source =
                        (byte)AppointmentSource.Online,

                    FeeSnapshot =
                        doctor.ConsultationFee
                };

            // =================================================
            // CREATE APPOINTMENT STATUS HISTORY
            // =================================================

            var statusHistory =
                new AppointmentStatusHistory
                {
                    Appointment =
                        appointment,

                    FromStatus =
                        null,

                    ToStatus =
                        (byte)AppointmentStatus.Pending,

                    ChangedAt =
                        now
                };

            // =================================================
            // REMINDER TIME
            //
            // Nhắc trước lịch khám 24 giờ.
            // =================================================

            var reminderAt =
                appointment.StartTime.AddHours(-24);


            // =================================================
            // CREATE NOTIFICATION
            // =================================================

            var notification =
                new Notification
                {
                    Appointment =
                        appointment,

                    UserId =
                        patient.UserId,

                    Channel =
                        "InApp",

                    Title =
                        "Nhắc lịch khám",

                    Content =
                        $"Bạn có lịch khám vào " +
                        $"{appointment.StartTime:HH:mm dd/MM/yyyy}.",

                    Status =
                        0,

                    ScheduledAt =
                        reminderAt,

                    CreatedAt =
                        now
                };

            // =================================================
            // BEGIN TRANSACTION
            // =================================================

            await _unitOfWork
                .BeginTransactionAsync();

            transactionStarted = true;

            // =================================================
            // RE-CHECK APPOINTMENT CONFLICT
            // =================================================

            var appointmentExists =
                await _unitOfWork
                    .AppointmentRepository
                    .WhereSql(
                        a =>
                            a.DoctorId ==
                                request.DoctorId &&

                            a.Status <
                                (byte)AppointmentStatus.Cancelled &&

                            a.StartTime <
                                appointment.EndTime &&

                            a.EndTime >
                                appointment.StartTime
                    )
                    .AnyAsync();

            // check appoitnmentExists
            if (appointmentExists)
            {
                await _unitOfWork
                    .RollbackTransactionAsync();

                transactionStarted = false;

                return Response(
                    409,
                    AppointmentResponseMessageDTO
                        .SlotNotAvailable
                );
            }

            // =================================================
            // ADD APPOINTMENT
            // =================================================

            await _unitOfWork
                .AppointmentRepository
                .AddAsync(
                    appointment
                );


            // =================================================
            // ADD STATUS HISTORY
            // =================================================

            await _unitOfWork
                .AppointmentStatusHistoryRepository
                .AddAsync(
                    statusHistory
                );


            // =================================================
            // ADD NOTIFICATION
            // =================================================

            await _unitOfWork
                .NotificationRepository
                .AddAsync(
                    notification
                );

            // =================================================
            // SAVE CHANGES
            // =================================================

            await _unitOfWork
                .SaveChangesAsync();


            // =================================================
            // COMMIT TRANSACTION
            // =================================================

            await _unitOfWork
                .CommitTransactionAsync();

            transactionStarted = false;



            // =================================================
            // MAP RESPONSE DTO
            // =================================================

            var result =
                new AppointmentDTO
                {
                    Id =
                        appointment.Id,

                    AppointmentCode =
                        appointment.AppointmentCode,

                    DoctorName =
                        doctor.FullName,

                    SpecialtyName =
                        specialty?.Name
                        ?? string.Empty,

                    StartTime =
                        appointment.StartTime,

                    Status =
                        appointment.Status,

                    QueueNumber =
                        appointment.QueueNumber,

                    FeeSnapshot =
                        appointment.FeeSnapshot
                };


            // =================================================
            // SUCCESS
            // =================================================

            return Response(
                201,
                AppointmentResponseMessageDTO
                    .CreateSuccess,
                result
            );
        }
        catch (Exception ex)
        {
            // =================================================
            // ROLLBACK TRANSACTION
            // =================================================

            if (transactionStarted)
            {
                try
                {
                    await _unitOfWork
                        .RollbackTransactionAsync();
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(
                        rollbackEx,
                        "Failed to rollback appointment creation."
                    );
                }
            }

            // =================================================
            // LOG ERROR
            // =================================================

            _logger.LogError(
                ex,
                "Failed to create appointment."
            );


            return Response(
                500,
                AppointmentResponseMessageDTO
                    .CreateFailed
            );
        }
    }


    // =====================================================
    // RESPONSE
    // =====================================================

    private static HttpResponseData<AppointmentDTO> Response(int statusCode, string message, AppointmentDTO? content = null)
    {
        return new HttpResponseData<AppointmentDTO>
        {
            StatusCode =
                statusCode,

            Message =
                message,

            Content =
                content
        };
    }

    // =====================================================
    // MY APPOINTMENTS RESPONSE
    // =====================================================

    private static HttpResponseData<List<MyAppointmentDTO>> MyAppointmentsResponse(int statusCode, string message, List<MyAppointmentDTO>? content = null)
    {
        return new HttpResponseData<List<MyAppointmentDTO>>
        {
            StatusCode =
                statusCode,

            Message =
                message,

            Content =
                content ??
                new List<MyAppointmentDTO>()
        };
    }
}