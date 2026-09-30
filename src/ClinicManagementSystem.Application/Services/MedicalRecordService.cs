using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.MedicalRecord;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Application.Enums;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Application.DTOs.Invoice;


namespace ClinicManagementSystem.Application.Services;


// =====================================================
// MEDICAL RECORD SERVICE CONTRACT
// =====================================================

public interface IMedicalRecordService
{
    Task<HttpResponseData<MedicalRecordDTO>> GetMedicalRecordByAppointmentAsync(int appointmentId, int currentUserId);

    Task<HttpResponseData<List<MedicalRecordDTO>>> GetPatientMedicalRecordsAsync(int patientId, int currentUserId);

    Task<HttpResponseData<MedicalRecordDraftResponseDTO>> UpdateMedicalRecordDraftAsync(int medicalRecordId, int currentUserId, MedicalRecordDraftRequestDTO request);

    Task<HttpResponseData<InvoiceDTO>> FinalizeMedicalRecordAsync(int medicalRecordId, int currentUserId);

}

// =====================================================
// MEDICAL RECORD SERVICE
// =====================================================

public class MedicalRecordService
    : IMedicalRecordService
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly IInvoiceService _invoiceService;

    private readonly ILogger<MedicalRecordService> _logger;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public MedicalRecordService(
    IUnitOfWork unitOfWork,
    IInvoiceService invoiceService,
    ILogger<MedicalRecordService> logger)
    {
        _unitOfWork =
            unitOfWork;

        _invoiceService =
            invoiceService;

        _logger =
            logger;
    }

    // =====================================================
    // FINALIZE MEDICAL RECORD
    // =====================================================

    public async Task<HttpResponseData<InvoiceDTO>>
        FinalizeMedicalRecordAsync(
            int medicalRecordId,
            int currentUserId)
    {
        bool transactionStarted = false;

        try
        {
            // =================================================
            // VALIDATE ID
            // =================================================

            if (medicalRecordId <= 0)
            {
                return InvoiceResponse(
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
                return InvoiceResponse(
                    401,
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
                return InvoiceResponse(
                    404,
                    MedicalRecordResponseMessageDTO
                        .MedicalRecordNotFound
                );
            }


            // =================================================
            // CHECK DOCTOR OWNERSHIP
            // =================================================

            var doctor =
                await _unitOfWork
                    .DoctorRepository
                    .WhereSql(
                        d =>
                            d.Id == medicalRecord.DoctorId &&
                            d.UserId == currentUserId &&
                            d.IsActive
                    )
                    .FirstOrDefaultAsync();


            if (doctor == null)
            {
                return InvoiceResponse(
                    403,
                    MedicalRecordResponseMessageDTO
                        .MedicalRecordAccessDenied
                );
            }


            // =================================================
            // CHECK MEDICAL RECORD STATUS
            //
            // Chỉ Draft mới được chốt.
            // =================================================

            if (
                medicalRecord.Status !=
                (byte)MedicalRecordStatus.Draft
            )
            {
                return InvoiceResponse(
                    409,
                    MedicalRecordResponseMessageDTO
                        .MedicalRecordAlreadyFinalized
                );
            }


            // =================================================
            // CHECK DIAGNOSIS
            // =================================================

            if (
                string.IsNullOrWhiteSpace(
                    medicalRecord.Diagnosis
                )
            )
            {
                return InvoiceResponse(
                    400,
                    MedicalRecordResponseMessageDTO
                        .DiagnosisRequired
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
                            a.Id ==
                            medicalRecord.AppointmentId
                    )
                    .FirstOrDefaultAsync();


            if (appointment == null)
            {
                return InvoiceResponse(
                    404,
                    MedicalRecordResponseMessageDTO
                        .AppointmentNotFound
                );
            }


            // =================================================
            // GET PATIENT
            // =================================================

            var patient =
                await _unitOfWork
                    .PatientRepository
                    .WhereSql(
                        p =>
                            p.Id ==
                            medicalRecord.PatientId &&
                            p.IsActive
                    )
                    .FirstOrDefaultAsync();


            if (patient == null)
            {
                return InvoiceResponse(
                    404,
                    MedicalRecordResponseMessageDTO
                        .MedicalRecordNotFound
                );
            }


            // =================================================
            // GET MEDICAL RECORD SERVICES
            // =================================================

            var medicalRecordServices =
                await _unitOfWork
                    .MedicalRecordServiceRepository
                    .WhereSql(
                        s =>
                            s.MedicalRecordId ==
                            medicalRecord.Id
                    )
                    .OrderBy(
                        s => s.Id
                    )
                    .ToListAsync();


            // =================================================
            // VALIDATE CLS STATUS
            //
            // Completed = 2
            // Cancelled = 3
            //
            // Các status khác:
            // → chưa thể chốt bệnh án.
            // =================================================

            var invalidService =
    medicalRecordServices
        .FirstOrDefault(
            s =>
                s.Status !=
                    (byte)MedicalRecordServiceStatus.Completed
                &&
                s.Status !=
                    (byte)MedicalRecordServiceStatus.Cancelled
        );


            if (invalidService != null)
            {
                return InvoiceResponse(
                    409,
                    MedicalRecordResponseMessageDTO
                        .MedicalRecordServicesNotCompleted
                );
            }


            // =================================================
            // GET PRESCRIPTION
            // =================================================

            var prescription =
                await _unitOfWork
                    .PrescriptionRepository
                    .WhereSql(
                        p =>
                            p.MedicalRecordId ==
                            medicalRecord.Id
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // GET PRESCRIPTION ITEMS
            // =================================================

            var prescriptionItems =
                new List<PrescriptionItem>();


            if (prescription != null)
            {
                prescriptionItems =
                    await _unitOfWork
                        .PrescriptionItemRepository
                        .WhereSql(
                            i =>
                                i.PrescriptionId ==
                                prescription.Id
                        )
                        .OrderBy(
                            i => i.Id
                        )
                        .ToListAsync();
            }


            // =================================================
            // CALCULATE CONSULTATION FEE
            // =================================================

            var consultationFee =
                appointment.FeeSnapshot;


            // =================================================
            // CALCULATE COMPLETED SERVICES
            // =================================================

            var completedServices =
                medicalRecordServices
                    .Where(
                        s =>
                            s.Status == (byte)MedicalRecordServiceStatus.Completed
                    )
                    .ToList();


            var serviceTotal =
                completedServices
                    .Sum(
                        s =>
                            s.Quantity *
                            s.UnitPriceSnapshot
                    );


            // =================================================
            // CALCULATE MEDICINE TOTAL
            // =================================================

            var medicineTotal =
                prescriptionItems
                    .Sum(
                        i =>
                            i.Quantity *
                            i.UnitPriceSnapshot
                    );


            // =================================================
            // CALCULATE INVOICE TOTAL
            //
            // Phí khám
            // + CLS Completed
            // + Thuốc
            // =================================================

            var totalAmount =
                consultationFee +
                serviceTotal +
                medicineTotal;


            // =================================================
            // BEGIN TRANSACTION
            // =================================================

            await _unitOfWork
                .BeginTransactionAsync();

            transactionStarted = true;


            var now =
                DateTime.Now;


            // =================================================
            // CREATE INVOICE
            // =================================================

            var invoice =
                new Invoice
                {
                    PatientId =
                        patient.Id,

                    AppointmentId =
                        appointment.Id,

                    MedicalRecordId =
                        medicalRecord.Id,

                    PatientName =
                        patient.FullName,

                    TotalAmount =
                        totalAmount,

                    DiscountAmount =
                        0,

                    TaxAmount =
                        0,

                    PaidAmount =
                        0,

                    InsuranceAmount =
                        0,

                    Status =
                        (byte)InvoiceStatus.Unpaid,

                    CreatedBy =
                        currentUserId,

                    CreatedAt =
                        now,

                    InvoiceNo =
                        "INV" +
                        Guid.NewGuid()
                            .ToString("N")[..17]
                            .ToUpperInvariant()
                };


            await _unitOfWork
                .InvoiceRepository
                .AddAsync(
                    invoice
                );


            // =================================================
            // SAVE INVOICE FIRST
            //
            // Lấy Invoice.Id trước khi tạo InvoiceItem.
            // =================================================

            await _unitOfWork
                .SaveChangesAsync();


            // =================================================
            // CREATE CLS INVOICE ITEMS
            // =================================================

            foreach (var medicalRecordService
                in completedServices)
            {
                var service =
                    await _unitOfWork
                        .ServiceRepository
                        .WhereSql(
                            s =>
                                s.Id ==
                                medicalRecordService.ServiceId
                        )
                        .FirstOrDefaultAsync();


                var amount =
                    medicalRecordService.Quantity *
                    medicalRecordService.UnitPriceSnapshot;


                var invoiceItem =
                    new InvoiceItem
                    {
                        InvoiceId =
                            invoice.Id,

                        MedicineId =
                            null,

                        MedicalRecordServiceId =
                            medicalRecordService.Id,

                        Description =
                            service?.Name ??
                            $"Dịch vụ #{medicalRecordService.ServiceId}",

                        Quantity =
                            medicalRecordService.Quantity,

                        UnitPrice =
                            medicalRecordService.UnitPriceSnapshot,

                        Amount =
                            amount,

                        DiscountAmount =
                            0
                    };


                await _unitOfWork
                    .InvoiceItemRepository
                    .AddAsync(
                        invoiceItem
                    );
            }


            // =================================================
            // CREATE MEDICINE INVOICE ITEMS
            // =================================================

            foreach (var prescriptionItem
                in prescriptionItems)
            {
                var amount =
                    prescriptionItem.Quantity *
                    prescriptionItem.UnitPriceSnapshot;


                var invoiceItem =
                    new InvoiceItem
                    {
                        InvoiceId =
                            invoice.Id,

                        MedicineId =
                            prescriptionItem.MedicineId,

                        MedicalRecordServiceId =
                            null,

                        Description =
                            prescriptionItem.MedicineNameSnapshot,

                        Quantity =
                            prescriptionItem.Quantity,

                        UnitPrice =
                            prescriptionItem.UnitPriceSnapshot,

                        Amount =
                            amount,

                        DiscountAmount =
                            0
                    };


                await _unitOfWork
                    .InvoiceItemRepository
                    .AddAsync(
                        invoiceItem
                    );
            }


            // =================================================
            // FINALIZE MEDICAL RECORD
            // =================================================

            medicalRecord.Status =
                (byte)MedicalRecordStatus.Finalized;

            medicalRecord.FinalizedAt =
                now;

            medicalRecord.UpdatedAt =
                now;


            // =================================================
            // UPDATE APPOINTMENT
            // =================================================

            var fromAppointmentStatus =
                appointment.Status;

            appointment.Status =
                (byte)AppointmentStatus.Completed;

            appointment.UpdatedAt =
                now;


            // =================================================
            // APPOINTMENT STATUS HISTORY
            // =================================================

            var appointmentHistory =
                new AppointmentStatusHistory
                {
                    AppointmentId =
                        appointment.Id,

                    FromStatus =
                        fromAppointmentStatus,

                    ToStatus =
                        (byte)AppointmentStatus.Completed,

                    ChangedBy =
                        currentUserId,

                    Reason =
                        "Chốt bệnh án.",

                    ChangedAt =
                        now
                };


            await _unitOfWork
                .AppointmentStatusHistoryRepository
                .AddAsync(
                    appointmentHistory
                );


            // =================================================
            // AUDIT LOG
            // =================================================

            var auditLog =
                new AuditLog
                {
                    UserId =
                        currentUserId,

                    Action =
                        "FINALIZE_MEDICAL_RECORD",

                    EntityName =
                        "MedicalRecord",

                    EntityId =
                        medicalRecord.Id,

                    Details =
                        $"Chốt bệnh án #{medicalRecord.Id}. " +
                        $"Hóa đơn: {invoice.InvoiceNo}. " +
                        $"Tổng tiền: {invoice.TotalAmount:N0}.",

                    IpAddress =
                        null,

                    Succeeded =
                        true,

                    OccurredAt =
                        now
                };


            await _unitOfWork
                .AuditLogRepository
                .AddAsync(
                    auditLog
                );


            // =================================================
            // SAVE ALL
            // =================================================

            await _unitOfWork
                .SaveChangesAsync();


            // =================================================
            // COMMIT
            // =================================================

            await _unitOfWork
                .CommitTransactionAsync();

            transactionStarted = false;


            // =================================================
            // GET FINALIZED INVOICE
            // =================================================

            var result =
    await _invoiceService
        .GetInvoiceAsync(
            invoice.Id
        );

            if (result.StatusCode != 200)
            {
                return result;
            }

            // success
            result.Message =
                MedicalRecordResponseMessageDTO
                    .FinalizeSuccess;

            return result;
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
                        "Failed to rollback medical record finalize transaction. " +
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
                "Failed to finalize medical record. " +
                "MedicalRecordId: {MedicalRecordId}, " +
                "UserId: {UserId}",
                medicalRecordId,
                currentUserId
            );


            return InvoiceResponse(
                500,
                MedicalRecordResponseMessageDTO
                    .FinalizeFailed
            );
        }
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

    // =====================================================
    // INVOICE RESPONSE
    // =====================================================

    private static HttpResponseData<InvoiceDTO>
        InvoiceResponse(
            int statusCode,
            string message,
            InvoiceDTO? content = null)
    {
        return new HttpResponseData<InvoiceDTO>
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