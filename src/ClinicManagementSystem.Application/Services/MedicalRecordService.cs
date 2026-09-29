using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.MedicalRecord;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Application.Enums;
using ClinicManagementSystem.Infrastructure.Models;


namespace ClinicManagementSystem.Application.Services;


// =====================================================
// MEDICAL RECORD SERVICE CONTRACT
// =====================================================

public interface IMedicalRecordService
{
    Task<HttpResponseData<MedicalRecordDTO>> GetMedicalRecordByAppointmentAsync(int appointmentId, int currentUserId);

    Task<HttpResponseData<List<MedicalRecordDTO>>> GetPatientMedicalRecordsAsync(int patientId, int currentUserId);

    Task<HttpResponseData<MedicalRecordDraftResponseDTO>> UpdateMedicalRecordDraftAsync(int medicalRecordId, int currentUserId, MedicalRecordDraftRequestDTO request);

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
    // UPDATE MEDICAL RECORD DRAFT
    // =====================================================

    public async Task<
        HttpResponseData<MedicalRecordDraftResponseDTO>>
        UpdateMedicalRecordDraftAsync(
            int medicalRecordId,
            int currentUserId,
            MedicalRecordDraftRequestDTO request)
    {
        bool transactionStarted = false;

        try
        {
            // =================================================
            // VALIDATE MEDICAL RECORD ID
            // =================================================

            if (medicalRecordId <= 0)
            {
                return DraftResponse(
                    400,
                    MedicalRecordResponseMessageDTO
                        .MedicalRecordNotFound
                );
            }


            // =================================================
            // VALIDATE USER
            // =================================================

            if (currentUserId <= 0)
            {
                return DraftResponse(
                    401,
                    MedicalRecordResponseMessageDTO
                        .MedicalRecordAccessDenied
                );
            }


            // =================================================
            // VALIDATE REQUEST
            // =================================================

            if (request == null)
            {
                return DraftResponse(
                    400,
                    MedicalRecordResponseMessageDTO
                        .SaveDraftFailed
                );
            }


            // =================================================
            // GET CURRENT DOCTOR
            //
            // UserId trong JWT
            //       ↓
            // Doctor.UserId
            // =================================================

            var doctor =
                await _unitOfWork
                    .DoctorRepository
                    .WhereSql(
                        d =>
                            d.UserId == currentUserId &&
                            d.IsActive
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // DOCTOR NOT FOUND
            // =================================================

            if (doctor == null)
            {
                return DraftResponse(
                    403,
                    MedicalRecordResponseMessageDTO
                        .MedicalRecordAccessDenied
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
                            m.Id == medicalRecordId
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // MEDICAL RECORD NOT FOUND
            // =================================================

            if (medicalRecord == null)
            {
                return DraftResponse(
                    404,
                    MedicalRecordResponseMessageDTO
                        .MedicalRecordNotFound
                );
            }


            // =================================================
            // CHECK DOCTOR OWNERSHIP
            // =================================================

            if (
                medicalRecord.DoctorId !=
                doctor.Id
            )
            {
                return DraftResponse(
                    403,
                    MedicalRecordResponseMessageDTO
                        .MedicalRecordAccessDenied
                );
            }


            // =================================================
            // CHECK STATUS
            //
            // Chỉ Draft mới được sửa.
            // =================================================

            if (
                medicalRecord.Status !=
                (byte)MedicalRecordStatus.Draft
            )
            {
                return DraftResponse(
                    409,
                    MedicalRecordResponseMessageDTO
                        .MedicalRecordNotDraft
                );
            }


            // =================================================
            // BEGIN TRANSACTION
            // =================================================

            await _unitOfWork
                .BeginTransactionAsync();

            transactionStarted = true;


            // =================================================
            // UPDATE MEDICAL RECORD
            // =================================================

            medicalRecord.Symptoms =
                request.Symptoms;

            medicalRecord.Diagnosis =
                request.Diagnosis;

            medicalRecord.Icd10Code =
                request.Icd10Code;

            medicalRecord.TreatmentPlan =
                request.TreatmentPlan;

            medicalRecord.Note =
                request.Note;

            medicalRecord.FollowUpDate =
                request.FollowUpDate;


            // =================================================
            // UPSERT PATIENT VITALS
            //
            // Nếu request không có vitals:
            // → giữ nguyên dữ liệu hiện tại.
            //
            // Nếu có vitals:
            // → có record → UPDATE
            // → chưa có → INSERT
            // =================================================

            PatientVital? patientVital = null;


            if (request.Vitals != null)
            {
                patientVital =
                    await _unitOfWork
                        .PatientVitalRepository
                        .WhereSql(
                            v =>
                                v.MedicalRecordId ==
                                medicalRecord.Id
                        )
                        .FirstOrDefaultAsync();


                // =================================================
                // INSERT VITAL
                // =================================================

                if (patientVital == null)
                {
                    patientVital =
                        new PatientVital
                        {
                            MedicalRecordId =
                                medicalRecord.Id,

                            Temperature =
                                request.Vitals.Temperature,

                            Pulse =
                                request.Vitals.Pulse,

                            BloodPressure =
                                request.Vitals.BloodPressure,

                            Weight =
                                request.Vitals.Weight,

                            Height =
                                request.Vitals.Height
                        };


                    await _unitOfWork
                        .PatientVitalRepository
                        .AddAsync(
                            patientVital
                        );
                }


                // =================================================
                // UPDATE VITAL
                // =================================================

                else
                {
                    patientVital.Temperature =
                        request.Vitals.Temperature;

                    patientVital.Pulse =
                        request.Vitals.Pulse;

                    patientVital.BloodPressure =
                        request.Vitals.BloodPressure;

                    patientVital.Weight =
                        request.Vitals.Weight;

                    patientVital.Height =
                        request.Vitals.Height;
                }
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
            // GET VITALS AFTER SAVE
            // =================================================

            var savedVital =
                await _unitOfWork
                    .PatientVitalRepository
                    .WhereSql(
                        v =>
                            v.MedicalRecordId ==
                            medicalRecord.Id
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // MAP RESPONSE
            // =================================================

            var result =
                new MedicalRecordDraftResponseDTO
                {
                    Id =
                        medicalRecord.Id,

                    AppointmentId =
                        medicalRecord.AppointmentId,

                    DoctorName =
                        doctor.FullName,

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
                        medicalRecord.CreatedAt,

                    Vitals =
                        savedVital == null
                            ? null
                            : new PatientVitalResponseDTO
                            {
                                Id =
                                    savedVital.Id,

                                MedicalRecordId =
                                    savedVital.MedicalRecordId,

                                Temperature =
                                    savedVital.Temperature,

                                Pulse =
                                    savedVital.Pulse,

                                BloodPressure =
                                    savedVital.BloodPressure,

                                Weight =
                                    savedVital.Weight,

                                Height =
                                    savedVital.Height
                            }
                };


            // =================================================
            // SUCCESS
            // =================================================

            return DraftResponse(
                200,
                MedicalRecordResponseMessageDTO
                    .SaveDraftSuccess,
                result
            );
        }
        catch (Exception ex)
        {
            // =================================================
            // ROLLBACK
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
                        "Failed to rollback medical record draft update. " +
                        "MedicalRecordId: {MedicalRecordId}",
                        medicalRecordId
                    );
                }
            }


            // =================================================
            // LOG ERROR
            // =================================================

            _logger.LogError(
                ex,
                "Failed to update medical record draft. " +
                "MedicalRecordId: {MedicalRecordId}, " +
                "UserId: {UserId}",
                medicalRecordId,
                currentUserId
            );


            return DraftResponse(
                500,
                MedicalRecordResponseMessageDTO
                    .SaveDraftFailed
            );
        }
    }


    // =====================================================
    // GET PATIENT MEDICAL HISTORY
    //
    // Doctor / Receptionist
    //
    // GET:
    // /api/medical-records/patient/{patientId}
    // =====================================================

    public async Task<
        HttpResponseData<List<MedicalRecordDTO>>>
        GetPatientMedicalRecordsAsync(
            int patientId,
            int currentUserId)
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
                            p.Id == patientId &&
                            p.IsActive
                    )
                    .FirstOrDefaultAsync();


            if (patient == null)
            {
                return new HttpResponseData<
                    List<MedicalRecordDTO>>
                {
                    StatusCode = 404,

                    Message =
                        MedicalRecordResponseMessageDTO
                            .PatientNotFound
                };
            }


            // =================================================
            // GET FINALIZED MEDICAL RECORDS
            // =================================================

            var records =
                await _unitOfWork
                    .MedicalRecordRepository
                    .WhereSql(
                        m =>
                            m.PatientId == patientId &&
                            m.Status ==
                                (byte)MedicalRecordStatus.Finalized
                    )
                    .Include(m => m.Doctor)
                    .OrderByDescending(
                        m => m.CreatedAt
                    )
                    .ToListAsync();


            // =================================================
            // MAP DTO
            // =================================================

            var result =
                records
                    .Select(
                        record =>
                            new MedicalRecordDTO
                            {
                                Id =
                                    record.Id,

                                AppointmentId =
                                    record.AppointmentId,

                                DoctorName =
                                    record.Doctor != null
                                        ? record.Doctor.FullName
                                        : string.Empty,

                                Symptoms =
                                    record.Symptoms,

                                Diagnosis =
                                    record.Diagnosis,

                                Icd10Code =
                                    record.Icd10Code,

                                TreatmentPlan =
                                    record.TreatmentPlan,

                                Note =
                                    record.Note,

                                FollowUpDate =
                                    record.FollowUpDate,

                                Status =
                                    record.Status,

                                FinalizedAt =
                                    record.FinalizedAt,

                                CreatedAt =
                                    record.CreatedAt
                            }
                    )
                    .ToList();


            // =================================================
            // AUDIT LOG
            // =================================================

            var auditLog =
                new AuditLog
                {
                    UserId =
                        currentUserId,

                    Action =
                        "VIEW_PATIENT_MEDICAL_RECORDS",

                    EntityName =
                        "Patient",

                    EntityId =
                        patientId,

                    Details =
                        $"Viewed medical records of patient " +
                        $"{patient.PatientCode}.",

                    Succeeded =
                        true,

                    OccurredAt =
                        DateTime.UtcNow
                };


            await _unitOfWork
                .AuditLogRepository
                .AddAsync(auditLog);


            await _unitOfWork
                .SaveChangesAsync();


            // =================================================
            // SUCCESS
            // =================================================

            return new HttpResponseData<
                List<MedicalRecordDTO>>
            {
                StatusCode = 200,

                Message =
                    MedicalRecordResponseMessageDTO
                        .GetSuccess,

                Content =
                    result
            };
        }
        catch (Exception ex)
        {
            // =================================================
            // LOG ERROR
            // =================================================

            _logger.LogError(
                ex,
                "Failed to get patient medical records. " +
                "PatientId: {PatientId}, " +
                "UserId: {UserId}",
                patientId,
                currentUserId
            );


            // =================================================
            // FAILED
            // =================================================

            return new HttpResponseData<
                List<MedicalRecordDTO>>
            {
                StatusCode = 500,

                Message =
                    MedicalRecordResponseMessageDTO
                        .GetFailed
            };
        }
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

    // =====================================================
    // DRAFT RESPONSE
    // =====================================================

    private static
        HttpResponseData<MedicalRecordDraftResponseDTO>
        DraftResponse(
            int statusCode,
            string message,
            MedicalRecordDraftResponseDTO? content = null)
    {
        return new HttpResponseData<MedicalRecordDraftResponseDTO>
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