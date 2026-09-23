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
    Task<HttpResponseData<List<PatientRecordDTO>>> GetPatientRecordsAsync(
        int currentUserId
    );
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

    public async Task<HttpResponseData<List<PatientRecordDTO>>> GetPatientRecordsAsync(int currentUserId)
    {
        try
        {
            // GET CURRENT PATIENT
            var patient = await _unitOfWork.PatientRepository
                .WhereSql(p =>
                    p.UserId == currentUserId &&
                    p.IsActive
                )
                .FirstOrDefaultAsync();

            // PATIENT NOT FOUND
            if (patient == null)
            {
                return new HttpResponseData<List<PatientRecordDTO>>
                {
                    StatusCode = 404,
                    Message = "Không tìm thấy bệnh nhân.",
                    Content = null
                };
            }


            // GET FINALIZED MEDICAL RECORDS
            var medicalRecords = await _unitOfWork.MedicalRecordRepository
                .WhereSql(m =>
                    m.PatientId == patient.Id &&
                    m.Status == (byte)MedicalRecordStatus.Finalized
                )
                .OrderByDescending(m => m.FinalizedAt)
                .ToListAsync();


            // NO MEDICAL RECORDS
            if (medicalRecords.Count == 0)
            {
                return new HttpResponseData<List<PatientRecordDTO>>
                {
                    StatusCode = 200,
                    Message = "Chưa có lịch sử khám.",
                    Content = new List<PatientRecordDTO>()
                };
            }


            // =================================================
            // MAP MEDICAL RECORDS
            // =================================================

            var result = new List<PatientRecordDTO>();


            foreach (var medicalRecord in medicalRecords)
            {
                // GET PRESCRIPTION
                var prescription = await _unitOfWork.PrescriptionRepository
                    .WhereSql(p => p.MedicalRecordId == medicalRecord.Id).FirstOrDefaultAsync();

                // PRESCRIPTION DTO
                PatientRecordPrescriptionDTO? prescriptionDto = null;


                if (prescription != null)
                {
                    // GET PRESCRIPTION ITEMS
                    var prescriptionItems = await _unitOfWork.PrescriptionItemRepository
                        .WhereSql(i =>
                            i.PrescriptionId == prescription.Id
                        )
                        .OrderBy(i => i.Id)
                        .ToListAsync();


                    // =================================================
                    // MAP PRESCRIPTION ITEMS
                    // =================================================

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

                    // MAP PRESCRIPTION
                    prescriptionDto = new PatientRecordPrescriptionDTO
                    {
                        Id = prescription.Id,
                        Note = prescription.Note,
                        Status = prescription.Status,
                        CreatedAt = prescription.CreatedAt,
                        DispensedAt = prescription.DispensedAt,
                        Items = itemDtos
                    };
                }

                // MAP MEDICAL RECORD
                var recordDto = new PatientRecordDTO
                {
                    Id = medicalRecord.Id,
                    AppointmentId = medicalRecord.AppointmentId,
                    DoctorName = string.Empty,
                    Diagnosis = medicalRecord.Diagnosis,
                    Icd10Code = medicalRecord.Icd10Code,
                    FollowUpDate = medicalRecord.FollowUpDate,
                    Status = medicalRecord.Status,
                    FinalizedAt = medicalRecord.FinalizedAt,
                    CreatedAt = medicalRecord.CreatedAt,
                    Prescription = prescriptionDto
                };

                result.Add(recordDto);
            }


            // SUCCESS
            return new HttpResponseData<List<PatientRecordDTO>>
            {
                StatusCode = 200,
                Message = "Lấy lịch sử khám thành công.",
                Content = result
            };

        }
        catch (Exception ex)
        {
            // LOG ERROR

            _logger.LogError(
                ex,
                "Failed to get current patient for record history. UserId: {UserId}",
                currentUserId
            );


            return new HttpResponseData<List<PatientRecordDTO>>
            {
                StatusCode = 500,
                Message = "Lấy lịch sử khám thất bại.",
                Content = null
            };
        }
    }
}