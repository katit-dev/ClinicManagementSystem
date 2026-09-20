using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.MedicalRecord;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;


namespace ClinicManagementSystem.Application.Services;


// =====================================================
// MEDICAL RECORD SERVICE CONTRACT
// =====================================================

public interface IMedicalRecordService
{
    Task<HttpResponseData<MedicalRecordDTO>>GetMedicalRecordByAppointmentAsync(int appointmentId, int currentUserId);
}

// =====================================================
// MEDICAL RECORD SERVICE
// =====================================================

public class MedicalRecordService
    : IMedicalRecordService
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly ILogger<MedicalRecordService> _logger;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public MedicalRecordService(
        IUnitOfWork unitOfWork,
        ILogger<MedicalRecordService> logger)
    {
        _unitOfWork =
            unitOfWork;

        _logger =
            logger;
    }


    // =====================================================
    // GET MEDICAL RECORD BY APPOINTMENT
    // =====================================================

    public async Task<
        HttpResponseData<MedicalRecordDTO>>
        GetMedicalRecordByAppointmentAsync(
            int appointmentId,
            int currentUserId)
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


            // =================================================
            // PATIENT NOT FOUND
            // =================================================

            if (patient == null)
            {
                return Response(
                    404,
                    MedicalRecordResponseMessageDTO
                        .MedicalRecordNotFound
                );
            }


            // =================================================
            // CHECK APPOINTMENT OWNERSHIP
            //
            // Appointment phải:
            //
            // - đúng AppointmentId
            // - thuộc Patient đang đăng nhập
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
                    MedicalRecordResponseMessageDTO
                        .MedicalRecordNotFound
                );
            }


            // =================================================
            // VALIDATION SUCCESS
            //
            // Chưa lấy MedicalRecord ở bước 5.3.
            // =================================================

            return Response(
                200,
                "Appointment hợp lệ để xem bệnh án."
            );
        }
        catch (Exception ex)
        {
            // =================================================
            // LOG ERROR
            // =================================================

            _logger.LogError(
                ex,
                "Failed to validate medical record access. " +
                "AppointmentId: {AppointmentId}, " +
                "UserId: {UserId}",
                appointmentId,
                currentUserId
            );


            return Response(
                500,
                MedicalRecordResponseMessageDTO
                    .GetFailed
            );
        }
    }


    // =====================================================
    // RESPONSE
    // =====================================================

    private static
        HttpResponseData<MedicalRecordDTO>
        Response(
            int statusCode,
            string message,
            MedicalRecordDTO? content = null)
    {
        return new HttpResponseData<MedicalRecordDTO>
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