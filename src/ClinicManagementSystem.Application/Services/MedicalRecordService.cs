using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.MedicalRecord;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Application.Enums;


namespace ClinicManagementSystem.Application.Services;


// =====================================================
// MEDICAL RECORD SERVICE CONTRACT
// =====================================================

public interface IMedicalRecordService
{
    Task<HttpResponseData<MedicalRecordDTO>> GetMedicalRecordByAppointmentAsync(int appointmentId, int currentUserId);
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
            // GET MEDICAL RECORD
            // =================================================

            var medicalRecord =
                await _unitOfWork
                    .MedicalRecordRepository
                    .WhereSql(
                        m =>
                            m.AppointmentId ==
                                appointment.Id &&
                            m.PatientId ==
                                patient.Id
                    )
                    .FirstOrDefaultAsync();


            if (medicalRecord == null)
            {
                return Response(
                    404,
                    MedicalRecordResponseMessageDTO
                        .MedicalRecordNotFound
                );
            }


            // =================================================
            // CHECK FINALIZED
            // =================================================

            if (
                medicalRecord.Status !=
                (byte)MedicalRecordStatus.Finalized
            )
            {
                return Response(
                    409,
                    MedicalRecordResponseMessageDTO
                        .MedicalRecordNotFinalized
                );
            }


            // =================================================
            // GET DOCTOR
            // =================================================

            var doctor =
                await _unitOfWork
                    .DoctorRepository
                    .WhereSql(
                        d =>
                            d.Id ==
                            medicalRecord.DoctorId
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // MAP RESPONSE
            // =================================================

            var result =
                new MedicalRecordDTO
                {
                    Id =
                        medicalRecord.Id,

                    AppointmentId =
                        medicalRecord.AppointmentId,

                    DoctorName =
                        doctor?.FullName
                        ?? string.Empty,

                    Symptoms =
                        medicalRecord.Symptoms,

                    Diagnosis =
                        medicalRecord.Diagnosis,

                    Icd10Code =
                        medicalRecord.Icd10Code,

                    TreatmentPlan =
                        medicalRecord.TreatmentPlan,

                    Note =
                        medicalRecord.Note,

                    FollowUpDate =
                        medicalRecord.FollowUpDate,

                    Status =
                        medicalRecord.Status,

                    FinalizedAt =
                        medicalRecord.FinalizedAt,

                    CreatedAt =
                        medicalRecord.CreatedAt
                };


            // =================================================
            // SUCCESS
            // =================================================

            return Response(
                200,
                MedicalRecordResponseMessageDTO
                    .GetSuccess,
                result
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