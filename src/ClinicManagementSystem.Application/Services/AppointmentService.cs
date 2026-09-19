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
    Task<HttpResponseData<AppointmentDTO>> CreateAppointmentAsync(CreateAppointmentRequestDTO request);
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
            CreateAppointmentRequestDTO request)
    {
        try
        {
            // =================================================
            // CHECK PATIENT
            // =================================================

            var patient =
                await _unitOfWork
                    .PatientRepository
                    .WhereSql(
                        p =>
                            p.Id == request.PatientId &&
                            p.IsActive
                    )
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
            // CHƯA INSERT APPOINTMENT Ở BƯỚC NÀY
            // =================================================

            return Response(
                200,
                "Slot hợp lệ."
            );
        }
        catch (Exception ex)
        {
            // =================================================
            // LOG ERROR
            // =================================================

            _logger.LogError(
                ex,
                "Failed to validate appointment creation."
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