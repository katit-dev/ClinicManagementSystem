using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.PatientRecord;
using ClinicManagementSystem.Application.Enums;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Application.Services;


// =====================================================
// PATIENT RECORD SERVICE CONTRACT
// =====================================================

public interface IPatientRecordService
{
    Task<HttpResponseData<List<PatientRecordDTO>>> GetPatientRecordsAsync(int currentUserId);
    Task<HttpResponseData<List<PatientRecordDTO>>> GetPatientRecordsByPatientIdAsync(int patientId, int currentUserId);
}


// =====================================================
// PATIENT RECORD SERVICE
// =====================================================

public class PatientRecordService : IPatientRecordService
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly ILogger<PatientRecordService> _logger;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public PatientRecordService(
        IUnitOfWork unitOfWork,
        ILogger<PatientRecordService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    // =====================================================
    // GET PATIENT RECORDS
    // =====================================================

    public async Task<HttpResponseData<List<PatientRecordDTO>>> GetPatientRecordsAsync(
        int currentUserId)
    {
        try
        {
            // =================================================
            // GET CURRENT PATIENT
            // =================================================

            var patient = await _unitOfWork.PatientRepository
                .WhereSql(p =>
                    p.UserId == currentUserId &&
                    p.IsActive)
                .FirstOrDefaultAsync();

            if (patient == null)
            {
                return Response(
                    404,
                    "Không tìm thấy bệnh nhân."
                );
            }


            // =================================================
            // GET RECORDS
            // =================================================

            var records =
                await GetRecordsByPatientIdAsync(patient.Id);


            // =================================================
            // SUCCESS
            // =================================================

            if (records.Count == 0)
            {
                return Response(
                    200,
                    "Chưa có lịch sử khám.",
                    records
                );
            }

            return Response(
                200,
                "Lấy lịch sử khám thành công.",
                records
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get patient record history. UserId: {UserId}",
                currentUserId);

            return Response(
                500,
                "Lấy lịch sử khám thất bại."
            );
        }
    }

    // =====================================================
    // GET RECORDS BY PATIENT ID
    // =====================================================

    private async Task<List<PatientRecordDTO>> GetRecordsByPatientIdAsync(
        int patientId)
    {
        // =================================================
        // GET FINALIZED MEDICAL RECORDS
        // =================================================

        var medicalRecords = await _unitOfWork.MedicalRecordRepository
            .WhereSql(m =>
                m.PatientId == patientId &&
                m.Status == (byte)MedicalRecordStatus.Finalized)
            .OrderByDescending(m => m.FinalizedAt)
            .ToListAsync();


        // =================================================
        // RESULT
        // =================================================

        var result = new List<PatientRecordDTO>();


        // =================================================
        // MAP RECORDS
        // =================================================

        foreach (var medicalRecord in medicalRecords)
        {
            // =================================================
            // PRESCRIPTION
            // =================================================

            var prescription = await _unitOfWork.PrescriptionRepository
                .WhereSql(p =>
                    p.MedicalRecordId == medicalRecord.Id)
                .FirstOrDefaultAsync();

            PatientRecordPrescriptionDTO? prescriptionDto = null;

            if (prescription != null)
            {
                var prescriptionItems =
                    await _unitOfWork.PrescriptionItemRepository
                        .WhereSql(i =>
                            i.PrescriptionId == prescription.Id)
                        .OrderBy(i => i.Id)
                        .ToListAsync();

                var itemDtos = prescriptionItems
                    .Select(i => new PatientRecordPrescriptionItemDTO
                    {
                        Id = i.Id,
                        MedicineName = i.MedicineNameSnapshot,
                        Quantity = i.Quantity,
                        Dosage = i.Dosage,
                        Instruction = i.Instruction,
                        DurationDays = i.DurationDays,
                        Frequency = i.Frequency
                    })
                    .ToList();

                prescriptionDto =
                    new PatientRecordPrescriptionDTO
                    {
                        Id = prescription.Id,
                        Note = prescription.Note,
                        Status = prescription.Status,
                        CreatedAt = prescription.CreatedAt,
                        DispensedAt = prescription.DispensedAt,
                        Items = itemDtos
                    };
            }


            // =================================================
            // MEDICAL RECORD SERVICES
            // =================================================

            var medicalRecordServices =
                await _unitOfWork.MedicalRecordServiceRepository
                    .WhereSql(s =>
                        s.MedicalRecordId == medicalRecord.Id)
                    .OrderBy(s => s.Id)
                    .ToListAsync();


            // =================================================
            // LAB RESULTS
            // =================================================

            var labResultDtos =
                new List<PatientRecordLabResultDTO>();

            foreach (var medicalRecordService in medicalRecordServices)
            {
                var labResult =
                    await _unitOfWork.LabResultRepository
                        .WhereSql(l =>
                            l.MedicalRecordServiceId ==
                            medicalRecordService.Id)
                        .FirstOrDefaultAsync();

                if (labResult == null)
                {
                    continue;
                }

                var service =
                    await _unitOfWork.ServiceRepository
                        .WhereSql(s =>
                            s.Id == medicalRecordService.ServiceId)
                        .FirstOrDefaultAsync();

                labResultDtos.Add(
                    new PatientRecordLabResultDTO
                    {
                        Id = labResult.Id,
                        MedicalRecordServiceId = medicalRecordService.Id,
                        ServiceId = medicalRecordService.ServiceId,
                        ServiceName = service?.Name ?? string.Empty,
                        ServiceCode = service?.Code,
                        ResultValue = labResult.ResultValue,
                        ReferenceRange = labResult.ReferenceRange,
                        Conclusion = labResult.Conclusion,
                        ResultedAt = labResult.ResultedAt
                    }
                );
            }


            // =================================================
            // ATTACHMENTS
            // =================================================

            var attachments =
                await _unitOfWork.AttachmentRepository
                    .WhereSql(a =>
                        a.MedicalRecordId == medicalRecord.Id)
                    .OrderByDescending(a => a.UploadedAt)
                    .ToListAsync();

            var attachmentDtos = attachments
                .Select(a => new PatientRecordAttachmentDTO
                {
                    Id = a.Id,
                    FileType = a.FileType,
                    UploadedAt = a.UploadedAt
                })
                .ToList();


            // =================================================
            // DOCTOR
            // =================================================

            var doctor = await _unitOfWork.DoctorRepository
                .WhereSql(d =>
                    d.Id == medicalRecord.DoctorId)
                .FirstOrDefaultAsync();


            // =================================================
            // MAP MEDICAL RECORD
            // =================================================

            result.Add(
                new PatientRecordDTO
                {
                    Id = medicalRecord.Id,
                    AppointmentId = medicalRecord.AppointmentId,
                    DoctorName = doctor?.FullName ?? string.Empty,
                    Diagnosis = medicalRecord.Diagnosis,
                    Icd10Code = medicalRecord.Icd10Code,
                    FollowUpDate = medicalRecord.FollowUpDate,
                    Status = medicalRecord.Status,
                    FinalizedAt = medicalRecord.FinalizedAt,
                    CreatedAt = medicalRecord.CreatedAt,
                    Prescription = prescriptionDto,
                    LabResults = labResultDtos,
                    Attachments = attachmentDtos
                }
            );
        }

        return result;
    }

    // =====================================================
    // RESPONSE
    // =====================================================

    private static HttpResponseData<List<PatientRecordDTO>> Response(
        int statusCode,
        string message,
        List<PatientRecordDTO>? content = null)
    {
        return new HttpResponseData<List<PatientRecordDTO>>
        {
            StatusCode = statusCode,
            Message = message,
            Content = content
        };
    }

    // =====================================================
    // GET PATIENT RECORDS FOR RECEPTION
    // =====================================================

    public Task<HttpResponseData<List<PatientRecordDTO>>> GetPatientRecordsByPatientIdAsync(
        int patientId,
        int currentUserId)
    {
        return Task.FromResult(
            Response(
                501,
                "Chức năng xem lịch sử khám của bệnh nhân đang được triển khai."
            )
        );
    }
}