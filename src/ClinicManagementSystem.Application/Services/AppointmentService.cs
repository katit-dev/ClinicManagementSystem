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
}


public class AppointmentService : IAppointmentService
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly IDoctorService _doctorService;

    private readonly ILogger<AppointmentService> _logger;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public AppointmentService(
        IUnitOfWork unitOfWork,
        IDoctorService doctorService,
        ILogger<AppointmentService> logger)
    {
        _unitOfWork = unitOfWork;

        _doctorService = doctorService;

        _logger = logger;
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
                        request.PatientId,

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

    private static
        HttpResponseData<AppointmentDTO>
        Response(
            int statusCode,
            string message,
            AppointmentDTO? content = null)
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
}