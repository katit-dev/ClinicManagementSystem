using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.MedicalRecord;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Application.Enums;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Application.DTOs.Invoice;
using MedicalRecordServiceEntity = ClinicManagementSystem.Infrastructure.Models.MedicalRecordService;
using ClinicManagementSystem.Application.DTOs.Prescription;


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

    // =====================================================
    // GET MEDICAL RECORD BY ID - DOCTOR
    // =====================================================

    Task<HttpResponseData<MedicalRecordExamDTO>> GetMedicalRecordByIdForDoctorAsync(int medicalRecordId, int currentUserId);

    Task<HttpResponseData<MedicalRecordServiceDTO>> AddMedicalRecordServiceAsync(int medicalRecordId, int currentUserId, MedicalRecordServiceRequestDTO request);

    Task<HttpResponseData<MedicalRecordServiceDTO>> CancelMedicalRecordServiceAsync(int medicalRecordServiceId, int currentUserId);

    Task<HttpResponseData<MedicalRecordServiceDTO>> AddMedicalRecordServiceResultAsync(int medicalRecordServiceId, int currentUserId, LabResultRequestDTO request);

    Task<HttpResponseData<AttachmentDTO>> UploadMedicalRecordAttachmentAsync(int medicalRecordId, int currentUserId, Stream fileStream, string fileName, string contentType);

    Task<HttpResponseData<PrescriptionResponseDTO>> CreatePrescriptionAsync(int medicalRecordId, int currentUserId, PrescriptionRequestDTO request);

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

    private readonly IFileStorageService _fileStorageService;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public MedicalRecordService(
    IUnitOfWork unitOfWork,
    IInvoiceService invoiceService,
    IFileStorageService fileStorageService,
    ILogger<MedicalRecordService> logger)
    {
        _unitOfWork =
            unitOfWork;

        _invoiceService =
            invoiceService;

        _fileStorageService =
            fileStorageService;

        _logger =
            logger;
    }

    // =====================================================
    // CREATE PRESCRIPTION
    //
    // PUT:
    // /api/medical-records/{id}/prescription
    //
    // Doctor lấy từ JWT.
    // Mỗi MedicalRecord chỉ có 1 Prescription.
    // =====================================================

    public async Task<
        HttpResponseData<PrescriptionResponseDTO>>
        CreatePrescriptionAsync(
            int medicalRecordId,
            int currentUserId,
            PrescriptionRequestDTO request)
    {
        bool transactionStarted = false;

        try
        {
            // =================================================
            // VALIDATE MEDICAL RECORD ID
            // =================================================

            if (medicalRecordId <= 0)
            {
                return new HttpResponseData<PrescriptionResponseDTO>
                {
                    StatusCode = 400,

                    Message =
                        PrescriptionResponseMessageDTO
                            .MedicalRecordNotFound
                };
            }


            // =================================================
            // VALIDATE USER
            // =================================================

            if (currentUserId <= 0)
            {
                return new HttpResponseData<PrescriptionResponseDTO>
                {
                    StatusCode = 401,

                    Message =
                        PrescriptionResponseMessageDTO
                            .MedicalRecordAccessDenied
                };
            }


            // =================================================
            // VALIDATE REQUEST
            // =================================================

            if (request == null ||
                request.Items == null ||
                request.Items.Count == 0)
            {
                return new HttpResponseData<PrescriptionResponseDTO>
                {
                    StatusCode = 400,

                    Message =
                        PrescriptionResponseMessageDTO
                            .InvalidRequest
                };
            }


            // =================================================
            // VALIDATE ITEMS
            // =================================================

            foreach (var item in request.Items)
            {
                if (item.MedicineId <= 0 ||
                    item.Quantity <= 0 ||
                    string.IsNullOrWhiteSpace(item.Dosage) ||
                    (item.DurationDays.HasValue &&
                     item.DurationDays.Value <= 0))
                {
                    return new HttpResponseData<PrescriptionResponseDTO>
                    {
                        StatusCode = 400,

                        Message =
                            PrescriptionResponseMessageDTO
                                .InvalidRequest
                    };
                }
            }


            // =================================================
            // GET CURRENT DOCTOR
            //
            // JWT UserId
            //      ↓
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
                return new HttpResponseData<PrescriptionResponseDTO>
                {
                    StatusCode = 403,

                    Message =
                        PrescriptionResponseMessageDTO
                            .MedicalRecordAccessDenied
                };
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
                return new HttpResponseData<PrescriptionResponseDTO>
                {
                    StatusCode = 404,

                    Message =
                        PrescriptionResponseMessageDTO
                            .MedicalRecordNotFound
                };
            }


            // =================================================
            // CHECK DOCTOR OWNERSHIP
            // =================================================

            if (medicalRecord.DoctorId != doctor.Id)
            {
                return new HttpResponseData<PrescriptionResponseDTO>
                {
                    StatusCode = 403,

                    Message =
                        PrescriptionResponseMessageDTO
                            .MedicalRecordAccessDenied
                };
            }


            // =================================================
            // CHECK MEDICAL RECORD STATUS
            //
            // Chỉ Draft mới được kê đơn.
            // =================================================

            if (
                medicalRecord.Status !=
                (byte)MedicalRecordStatus.Draft
            )
            {
                return new HttpResponseData<PrescriptionResponseDTO>
                {
                    StatusCode = 409,

                    Message =
                        PrescriptionResponseMessageDTO
                            .MedicalRecordNotDraft
                };
            }


            // =================================================
            // CHECK EXISTING PRESCRIPTION
            //
            // Mỗi MedicalRecord chỉ có 1 Prescription.
            // =================================================

            var existingPrescription =
                await _unitOfWork
                    .PrescriptionRepository
                    .WhereSql(
                        p =>
                            p.MedicalRecordId ==
                            medicalRecordId
                    )
                    .FirstOrDefaultAsync();


            if (existingPrescription != null)
            {
                return new HttpResponseData<PrescriptionResponseDTO>
                {
                    StatusCode = 409,

                    Message =
                        PrescriptionResponseMessageDTO
                            .PrescriptionAlreadyExists
                };
            }


            // =================================================
            // GET MEDICINES
            // =================================================

            var medicineIds =
                request.Items
                    .Select(i => i.MedicineId)
                    .Distinct()
                    .ToList();


            var medicines =
                await _unitOfWork
                    .MedicineRepository
                    .WhereSql(
                        m =>
                            medicineIds.Contains(m.Id)
                    )
                    .ToListAsync();


            // =================================================
            // CHECK ALL MEDICINES EXIST
            // =================================================

            if (medicines.Count != medicineIds.Count)
            {
                return new HttpResponseData<PrescriptionResponseDTO>
                {
                    StatusCode = 404,

                    Message =
                        PrescriptionResponseMessageDTO
                            .MedicineNotFound
                };
            }


            // =================================================
            // CHECK ACTIVE
            // =================================================

            var inactiveMedicine =
                medicines.FirstOrDefault(
                    m => !m.IsActive
                );


            if (inactiveMedicine != null)
            {
                return new HttpResponseData<PrescriptionResponseDTO>
                {
                    StatusCode = 409,

                    Message =
                        PrescriptionResponseMessageDTO
                            .MedicineInactive
                };
            }


            // =================================================
            // GET PATIENT ALLERGIES
            // =================================================

            var allergens =
                await _unitOfWork
                    .PatientAllergyRepository
                    .WhereSql(
                        a =>
                            a.PatientId ==
                            medicalRecord.PatientId
                    )
                    .Select(
                        a => a.Allergen
                    )
                    .Where(
                        a =>
                            !string.IsNullOrWhiteSpace(a)
                    )
                    .ToListAsync();


            var normalizedAllergens =
                allergens
                    .Select(
                        a => a.Trim()
                    )
                    .Where(
                        a => a.Length > 0
                    )
                    .ToList();


            // =================================================
            // CHECK ALLERGY
            // =================================================

            foreach (var item in request.Items)
            {
                var medicine =
                    medicines.First(
                        m =>
                            m.Id ==
                            item.MedicineId
                    );


                if (
                    !string.IsNullOrWhiteSpace(
                        medicine.ActiveIngredient
                    ) &&
                    normalizedAllergens.Any(
                        allergen =>
                            string.Equals(
                                allergen,
                                medicine.ActiveIngredient.Trim(),
                                StringComparison.OrdinalIgnoreCase
                            )
                    ) &&
                    !item.AllergyConfirmed
                )
                {
                    return new HttpResponseData<PrescriptionResponseDTO>
                    {
                        StatusCode = 409,

                        Message =
                            PrescriptionResponseMessageDTO
                                .AllergyConfirmationRequired
                    };
                }
            }


            // =================================================
            // GET VALID BATCHES
            //
            // Chỉ tính:
            // - Quantity > 0
            // - ExpiryDate >= hôm nay
            // =================================================

            var today =
                DateOnly.FromDateTime(
                    DateTime.Now
                );


            var validBatches =
                await _unitOfWork
                    .MedicineBatchRepository
                    .WhereSql(
                        b =>
                            medicineIds.Contains(
                                b.MedicineId
                            ) &&
                            b.Quantity > 0 &&
                            b.ExpiryDate >= today
                    )
                    .ToListAsync();


            // =================================================
            // CHECK STOCK
            // =================================================

            foreach (var item in request.Items)
            {
                var availableQuantity =
                    validBatches
                        .Where(
                            b =>
                                b.MedicineId ==
                                item.MedicineId
                        )
                        .Sum(
                            b => b.Quantity
                        );


                if (
                    availableQuantity <
                    item.Quantity
                )
                {
                    return new HttpResponseData<PrescriptionResponseDTO>
                    {
                        StatusCode = 409,

                        Message =
                            PrescriptionResponseMessageDTO
                                .MedicineOutOfStock
                    };
                }


                // =============================================
                // CHECK EXPIRED ONLY
                //
                // Nếu không có batch còn hạn nhưng có batch
                // của thuốc này đã tồn tại → báo hết hạn.
                // =============================================

                var allBatches =
                    await _unitOfWork
                        .MedicineBatchRepository
                        .WhereSql(
                            b =>
                                b.MedicineId ==
                                item.MedicineId
                        )
                        .AnyAsync();


                var hasExpiredOrInvalidBatch =
                    await _unitOfWork
                        .MedicineBatchRepository
                        .WhereSql(
                            b =>
                                b.MedicineId ==
                                item.MedicineId &&
                                b.Quantity > 0 &&
                                b.ExpiryDate < today
                        )
                        .AnyAsync();


                if (
                    availableQuantity == 0 &&
                    hasExpiredOrInvalidBatch
                )
                {
                    return new HttpResponseData<PrescriptionResponseDTO>
                    {
                        StatusCode = 409,

                        Message =
                            PrescriptionResponseMessageDTO
                                .MedicineExpired
                    };
                }
            }


            // =================================================
            // BEGIN TRANSACTION
            // =================================================

            await _unitOfWork
                .BeginTransactionAsync();

            transactionStarted = true;


            // =================================================
            // CREATE PRESCRIPTION
            // =================================================

            var prescription =
                new Prescription
                {
                    MedicalRecordId =
                        medicalRecord.Id,

                    DoctorId =
                        doctor.Id,

                    Note =
                        string.IsNullOrWhiteSpace(
                            request.Note)
                            ? null
                            : request.Note.Trim(),

                    CreatedAt =
                        DateTime.Now

                    // Status:
                    // Không set ở đây vì source hiện tại
                    // chưa có enum/definition xác nhận
                    // giá trị status ban đầu.
                    //
                    // CLR default của byte = 0.
                };


            await _unitOfWork
                .PrescriptionRepository
                .AddAsync(
                    prescription
                );


            // =================================================
            // CREATE PRESCRIPTION ITEMS
            // =================================================

            var prescriptionItems =
                new List<PrescriptionItem>();


            foreach (var item in request.Items)
            {
                var medicine =
                    medicines.First(
                        m =>
                            m.Id ==
                            item.MedicineId
                    );


                var prescriptionItem =
                    new PrescriptionItem
                    {
                        Prescription =
                            prescription,

                        MedicineId =
                            medicine.Id,

                        Quantity =
                            item.Quantity,

                        Dosage =
                            item.Dosage.Trim(),

                        Instruction =
                            string.IsNullOrWhiteSpace(
                                item.Instruction)
                                ? null
                                : item.Instruction.Trim(),

                        MedicineNameSnapshot =
                            medicine.Name,

                        UnitPriceSnapshot =
                            medicine.Price,

                        DurationDays =
                            item.DurationDays,

                        Frequency =
                            null
                    };


                prescriptionItems.Add(
                    prescriptionItem
                );
            }


            await _unitOfWork
                .PrescriptionItemRepository
                .AddListItemsAsync(
                    prescriptionItems
                );


            // =================================================
            // SAVE
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
            // MAP RESPONSE
            // =================================================

            var result =
                new PrescriptionResponseDTO
                {
                    Id =
                        prescription.Id,

                    MedicalRecordId =
                        prescription.MedicalRecordId,

                    Note =
                        prescription.Note,

                    Status =
                        prescription.Status,

                    CreatedAt =
                        prescription.CreatedAt,

                    DispensedAt =
                        prescription.DispensedAt,

                    Items =
                        prescriptionItems
                            .Select(
                                item =>
                                    new PrescriptionItemResponseDTO
                                    {
                                        Id =
                                            item.Id,

                                        MedicineName =
                                            item.MedicineNameSnapshot,

                                        Quantity =
                                            item.Quantity,

                                        Dosage =
                                            item.Dosage
                                            ?? string.Empty,

                                        Instruction =
                                            item.Instruction,

                                        DurationDays =
                                            item.DurationDays,

                                        Frequency =
                                            item.Frequency
                                    }
                            )
                            .ToList()
                };


            // =================================================
            // SUCCESS
            // =================================================

            return new HttpResponseData<PrescriptionResponseDTO>
            {
                StatusCode = 200,

                Message =
                    PrescriptionResponseMessageDTO
                        .PrescriptionCreateSuccess,

                Content =
                    result
            };
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
                        "Failed to rollback prescription creation. " +
                        "MedicalRecordId: {MedicalRecordId}",
                        medicalRecordId
                    );
                }
            }


            // =================================================
            // LOG
            // =================================================

            _logger.LogError(
                ex,
                "Failed to create prescription. " +
                "MedicalRecordId: {MedicalRecordId}, " +
                "UserId: {UserId}",
                medicalRecordId,
                currentUserId
            );


            return new HttpResponseData<PrescriptionResponseDTO>
            {
                StatusCode = 500,

                Message =
                    PrescriptionResponseMessageDTO
                        .PrescriptionCreateFailed
            };
        }
    }

    // =====================================================
    // UPLOAD MEDICAL RECORD ATTACHMENT
    //
    // POST:
    // /api/medical-records/{id}/attachments
    //
    // Request:
    // multipart/form-data
    //
    // Allowed:
    // jpg
    // png
    // pdf
    // dicom
    // =====================================================

    public async Task<
        HttpResponseData<AttachmentDTO>>
        UploadMedicalRecordAttachmentAsync(
            int medicalRecordId,
            int currentUserId,
            Stream fileStream,
            string fileName,
            string contentType)
    {
        try
        {
            // =================================================
            // VALIDATE MEDICAL RECORD ID
            // =================================================

            if (medicalRecordId <= 0)
            {
                return new HttpResponseData<AttachmentDTO>
                {
                    StatusCode = 400,

                    Message =
                        MedicalRecordResponseMessageDTO
                            .MedicalRecordNotFound
                };
            }


            // =================================================
            // VALIDATE USER
            // =================================================

            if (currentUserId <= 0)
            {
                return new HttpResponseData<AttachmentDTO>
                {
                    StatusCode = 401,

                    Message =
                        MedicalRecordResponseMessageDTO
                            .MedicalRecordAccessDenied
                };
            }


            // =================================================
            // VALIDATE FILE
            // =================================================

            if (fileStream == null ||
                fileStream.Length <= 0 ||
                string.IsNullOrWhiteSpace(fileName))
            {
                return new HttpResponseData<AttachmentDTO>
                {
                    StatusCode = 400,

                    Message =
                        MedicalRecordResponseMessageDTO
                            .AttachmentFileRequired
                };
            }


            // =================================================
            // VALIDATE FILE TYPE
            // =================================================

            var extension =
                Path.GetExtension(fileName)
                    .ToLowerInvariant();

            var allowedExtensions =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase)
                {
                ".jpg",
                ".jpeg",
                ".png",
                ".pdf",
                ".dcm",
                ".dicom"
                };

            if (!allowedExtensions.Contains(
                extension))
            {
                return new HttpResponseData<AttachmentDTO>
                {
                    StatusCode = 400,

                    Message =
                        MedicalRecordResponseMessageDTO
                            .AttachmentFileTypeNotAllowed
                };
            }


            // =================================================
            // GET CURRENT DOCTOR
            //
            // JWT UserId
            //      ↓
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
                return new HttpResponseData<AttachmentDTO>
                {
                    StatusCode = 403,

                    Message =
                        MedicalRecordResponseMessageDTO
                            .MedicalRecordAccessDenied
                };
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
                return new HttpResponseData<AttachmentDTO>
                {
                    StatusCode = 404,

                    Message =
                        MedicalRecordResponseMessageDTO
                            .MedicalRecordNotFound
                };
            }


            // =================================================
            // CHECK DOCTOR OWNERSHIP
            // =================================================

            if (
                medicalRecord.DoctorId !=
                doctor.Id
            )
            {
                return new HttpResponseData<AttachmentDTO>
                {
                    StatusCode = 403,

                    Message =
                        MedicalRecordResponseMessageDTO
                            .MedicalRecordAccessDenied
                };
            }


            // =================================================
            // CHECK MEDICAL RECORD STATUS
            //
            // Chỉ Draft mới được thêm attachment.
            // =================================================

            if (
                medicalRecord.Status !=
                (byte)MedicalRecordStatus.Draft
            )
            {
                return new HttpResponseData<AttachmentDTO>
                {
                    StatusCode = 409,

                    Message =
                        MedicalRecordResponseMessageDTO
                            .MedicalRecordNotDraft
                };
            }


            // =================================================
            // UPLOAD FILE
            // =================================================

            var fileUrl =
                await _fileStorageService
                    .UploadAsync(
                        fileStream,
                        fileName,
                        contentType,
                        medicalRecordId
                    );


            // =================================================
            // CREATE ATTACHMENT
            // =================================================

            var attachment =
                new Attachment
                {
                    MedicalRecordId =
                        medicalRecord.Id,

                    FileUrl =
                        fileUrl,

                    FileType =
                        string.IsNullOrWhiteSpace(
                            contentType)
                            ? null
                            : contentType,

                    UploadedAt =
                        DateTime.UtcNow,

                    UploadedBy =
                        currentUserId
                };


            // =================================================
            // SAVE ATTACHMENT
            // =================================================

            await _unitOfWork
                .AttachmentRepository
                .AddAsync(
                    attachment
                );

            await _unitOfWork
                .SaveChangesAsync();


            // =================================================
            // MAP RESPONSE
            // =================================================

            var result =
                new AttachmentDTO
                {
                    Id =
                        attachment.Id,

                    MedicalRecordId =
                        attachment.MedicalRecordId,

                    FileUrl =
                        attachment.FileUrl,

                    FileType =
                        attachment.FileType,

                    UploadedAt =
                        attachment.UploadedAt,

                    UploadedBy =
                        attachment.UploadedBy
                };


            // =================================================
            // SUCCESS
            // =================================================

            return new HttpResponseData<AttachmentDTO>
            {
                StatusCode = 200,

                Message =
                    MedicalRecordResponseMessageDTO
                        .AttachmentUploadSuccess,

                Content =
                    result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to upload medical record attachment. " +
                "MedicalRecordId: {MedicalRecordId}, " +
                "UserId: {UserId}",
                medicalRecordId,
                currentUserId
            );

            return new HttpResponseData<AttachmentDTO>
            {
                StatusCode = 500,

                Message =
                    MedicalRecordResponseMessageDTO
                        .AttachmentUploadFailed
            };
        }
    }

    // =====================================================
    // ADD MEDICAL RECORD SERVICE RESULT
    //
    // POST:
    // /api/medical-record-services/{id}/result
    //
    // Khi nhập kết quả:
    // - Tạo LabResult
    // - MedicalRecordService.Status = Completed
    // - CompletedAt = thời điểm hiện tại
    // - PerformedBy = UserId hiện tại
    // =====================================================

    public async Task<
        HttpResponseData<MedicalRecordServiceDTO>>
        AddMedicalRecordServiceResultAsync(
            int medicalRecordServiceId,
            int currentUserId,
            LabResultRequestDTO request)
    {
        bool transactionStarted = false;

        try
        {
            // =================================================
            // VALIDATE ID
            // =================================================

            if (medicalRecordServiceId <= 0)
            {
                return new HttpResponseData<MedicalRecordServiceDTO>
                {
                    StatusCode = 400,

                    Message =
                        MedicalRecordResponseMessageDTO
                            .MedicalRecordServiceNotFound
                };
            }


            // =================================================
            // VALIDATE USER
            // =================================================

            if (currentUserId <= 0)
            {
                return new HttpResponseData<MedicalRecordServiceDTO>
                {
                    StatusCode = 401,

                    Message =
                        MedicalRecordResponseMessageDTO
                            .MedicalRecordAccessDenied
                };
            }


            // =================================================
            // VALIDATE REQUEST
            // =================================================

            if (request == null)
            {
                return new HttpResponseData<MedicalRecordServiceDTO>
                {
                    StatusCode = 400,

                    Message =
                        MedicalRecordResponseMessageDTO
                            .InvalidLabResultRequest
                };
            }


            // =================================================
            // VALIDATE RESULT VALUE
            // =================================================

            if (string.IsNullOrWhiteSpace(
                request.ResultValue))
            {
                return new HttpResponseData<MedicalRecordServiceDTO>
                {
                    StatusCode = 400,

                    Message =
                        MedicalRecordResponseMessageDTO
                            .LabResultValueRequired
                };
            }


            // =================================================
            // GET MEDICAL RECORD SERVICE
            //
            // Load Service để map response.
            // =================================================

            var medicalRecordService =
                await _unitOfWork
                    .MedicalRecordServiceRepository
                    .WhereSql(
                        s =>
                            s.Id ==
                            medicalRecordServiceId
                    )
                    .Include(
                        s => s.Service
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // NOT FOUND
            // =================================================

            if (medicalRecordService == null)
            {
                return new HttpResponseData<MedicalRecordServiceDTO>
                {
                    StatusCode = 404,

                    Message =
                        MedicalRecordResponseMessageDTO
                            .MedicalRecordServiceNotFound
                };
            }


            // =================================================
            // GET MEDICAL RECORD
            // =================================================

            var medicalRecord =
                await _unitOfWork
                    .MedicalRecordRepository
                    .WhereSql(
                        m =>
                            m.Id ==
                            medicalRecordService.MedicalRecordId
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // MEDICAL RECORD NOT FOUND
            // =================================================

            if (medicalRecord == null)
            {
                return new HttpResponseData<MedicalRecordServiceDTO>
                {
                    StatusCode = 404,

                    Message =
                        MedicalRecordResponseMessageDTO
                            .MedicalRecordNotFound
                };
            }


            // =================================================
            // MEDICAL RECORD MUST STILL BE DRAFT
            //
            // Finalized = đã khóa.
            // =================================================

            if (
                medicalRecord.Status !=
                (byte)MedicalRecordStatus.Draft
            )
            {
                return new HttpResponseData<MedicalRecordServiceDTO>
                {
                    StatusCode = 409,

                    Message =
                        MedicalRecordResponseMessageDTO
                            .MedicalRecordNotDraft
                };
            }


            // =================================================
            // CHECK SERVICE STATUS
            //
            // Không được nhập kết quả cho service đã Cancelled.
            // Không tạo lại kết quả cho service đã Completed.
            // =================================================

            if (
                medicalRecordService.Status ==
                (byte)MedicalRecordServiceStatus.Cancelled
            )
            {
                return new HttpResponseData<MedicalRecordServiceDTO>
                {
                    StatusCode = 409,

                    Message =
                        MedicalRecordResponseMessageDTO
                            .MedicalRecordServiceCancelled
                };
            }


            if (
                medicalRecordService.Status ==
                (byte)MedicalRecordServiceStatus.Completed
            )
            {
                return new HttpResponseData<MedicalRecordServiceDTO>
                {
                    StatusCode = 409,

                    Message =
                        MedicalRecordResponseMessageDTO
                            .MedicalRecordServiceCompleted
                };
            }


            // =================================================
            // CHECK EXISTING RESULT
            // =================================================

            var existingResult =
                await _unitOfWork
                    .LabResultRepository
                    .WhereSql(
                        r =>
                            r.MedicalRecordServiceId ==
                            medicalRecordService.Id
                    )
                    .FirstOrDefaultAsync();


            if (existingResult != null)
            {
                return new HttpResponseData<MedicalRecordServiceDTO>
                {
                    StatusCode = 409,

                    Message =
                        MedicalRecordResponseMessageDTO
                            .LabResultAlreadyExists
                };
            }


            // =================================================
            // BEGIN TRANSACTION
            // =================================================

            await _unitOfWork
                .BeginTransactionAsync();

            transactionStarted = true;


            // =================================================
            // CURRENT TIME
            // =================================================

            var now =
                DateTime.UtcNow;


            // =================================================
            // CREATE LAB RESULT
            // =================================================

            var labResult =
                new LabResult
                {
                    MedicalRecordServiceId =
                        medicalRecordService.Id,

                    ResultValue =
                        request.ResultValue.Trim(),

                    ReferenceRange =
                        string.IsNullOrWhiteSpace(
                            request.ReferenceRange)
                            ? null
                            : request.ReferenceRange.Trim(),

                    Conclusion =
                        string.IsNullOrWhiteSpace(
                            request.Conclusion)
                            ? null
                            : request.Conclusion.Trim(),

                    ResultedAt =
                        request.ResultedAt
                };


            // =================================================
            // ADD LAB RESULT
            // =================================================

            await _unitOfWork
                .LabResultRepository
                .AddAsync(
                    labResult
                );


            // =================================================
            // UPDATE MEDICAL RECORD SERVICE
            // =================================================

            medicalRecordService.Status =
                (byte)MedicalRecordServiceStatus.Completed;

            medicalRecordService.PerformedBy =
                currentUserId;

            medicalRecordService.CompletedAt =
                now;


            // =================================================
            // SAVE
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
            // MAP RESPONSE
            // =================================================

            var result =
                new MedicalRecordServiceDTO
                {
                    Id =
                        medicalRecordService.Id,

                    ServiceId =
                        medicalRecordService.ServiceId,

                    ServiceName =
                        medicalRecordService.Service?.Name
                        ?? string.Empty,

                    Quantity =
                        medicalRecordService.Quantity,

                    UnitPriceSnapshot =
                        medicalRecordService
                            .UnitPriceSnapshot,

                    Status =
                        medicalRecordService.Status,

                    OrderedAt =
                        medicalRecordService.OrderedAt,

                    CompletedAt =
                        medicalRecordService.CompletedAt,

                    Result =
                        new LabResultDTO
                        {
                            Id =
                                labResult.Id,

                            ResultValue =
                                labResult.ResultValue,

                            ReferenceRange =
                                labResult.ReferenceRange,

                            Conclusion =
                                labResult.Conclusion,

                            ResultedAt =
                                labResult.CreatedAt
                        }
                };


            // =================================================
            // SUCCESS
            // =================================================

            return new HttpResponseData<MedicalRecordServiceDTO>
            {
                StatusCode = 200,

                Message =
                    MedicalRecordResponseMessageDTO
                        .AddLabResultSuccess,

                Content =
                    result
            };
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
                        "Failed to rollback adding lab result. " +
                        "MedicalRecordServiceId: {MedicalRecordServiceId}",
                        medicalRecordServiceId
                    );
                }
            }


            // =================================================
            // LOG ERROR
            // =================================================

            _logger.LogError(
                ex,
                "Failed to add lab result. " +
                "MedicalRecordServiceId: {MedicalRecordServiceId}, " +
                "UserId: {UserId}",
                medicalRecordServiceId,
                currentUserId
            );


            return new HttpResponseData<MedicalRecordServiceDTO>
            {
                StatusCode = 500,

                Message =
                    MedicalRecordResponseMessageDTO
                        .AddLabResultFailed
            };
        }
    }

    // =====================================================
    // CANCEL MEDICAL RECORD SERVICE
    //
    // PATCH:
    // /api/medical-record-services/{id}/cancel
    //
    // Chỉ được hủy khi:
    // MedicalRecordService.Status = Ordered
    // =====================================================

    public async Task<
        HttpResponseData<MedicalRecordServiceDTO>>
        CancelMedicalRecordServiceAsync(
            int medicalRecordServiceId,
            int currentUserId)
    {
        try
        {
            // =================================================
            // VALIDATE ID
            // =================================================

            if (medicalRecordServiceId <= 0)
            {
                return new HttpResponseData<MedicalRecordServiceDTO>
                {
                    StatusCode = 400,

                    Message =
                        MedicalRecordResponseMessageDTO
                            .MedicalRecordServiceNotFound
                };
            }


            // =================================================
            // VALIDATE USER
            // =================================================

            if (currentUserId <= 0)
            {
                return new HttpResponseData<MedicalRecordServiceDTO>
                {
                    StatusCode = 401,

                    Message =
                        MedicalRecordResponseMessageDTO
                            .MedicalRecordAccessDenied
                };
            }


            // =================================================
            // GET CURRENT DOCTOR
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
                return new HttpResponseData<MedicalRecordServiceDTO>
                {
                    StatusCode = 403,

                    Message =
                        MedicalRecordResponseMessageDTO
                            .MedicalRecordAccessDenied
                };
            }


            // =================================================
            // GET MEDICAL RECORD SERVICE
            //
            // Load luôn Service để map response.
            // =================================================

            var medicalRecordService =
                await _unitOfWork
                    .MedicalRecordServiceRepository
                    .WhereSql(
                        s =>
                            s.Id == medicalRecordServiceId
                    )
                    .Include(
                        s => s.Service
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // SERVICE NOT FOUND
            // =================================================

            if (medicalRecordService == null)
            {
                return new HttpResponseData<MedicalRecordServiceDTO>
                {
                    StatusCode = 404,

                    Message =
                        MedicalRecordResponseMessageDTO
                            .MedicalRecordServiceNotFound
                };
            }


            // =================================================
            // GET MEDICAL RECORD
            // =================================================

            var medicalRecord =
                await _unitOfWork
                    .MedicalRecordRepository
                    .WhereSql(
                        m =>
                            m.Id ==
                            medicalRecordService.MedicalRecordId
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // MEDICAL RECORD NOT FOUND
            // =================================================

            if (medicalRecord == null)
            {
                return new HttpResponseData<MedicalRecordServiceDTO>
                {
                    StatusCode = 404,

                    Message =
                        MedicalRecordResponseMessageDTO
                            .MedicalRecordNotFound
                };
            }


            // =================================================
            // CHECK DOCTOR OWNERSHIP
            // =================================================

            if (
                medicalRecord.DoctorId !=
                doctor.Id
            )
            {
                return new HttpResponseData<MedicalRecordServiceDTO>
                {
                    StatusCode = 403,

                    Message =
                        MedicalRecordResponseMessageDTO
                            .MedicalRecordAccessDenied
                };
            }


            // =================================================
            // CHECK MEDICAL RECORD STATUS
            //
            // Finalized = đã khóa bệnh án.
            // =================================================

            if (
                medicalRecord.Status !=
                (byte)MedicalRecordStatus.Draft
            )
            {
                return new HttpResponseData<MedicalRecordServiceDTO>
                {
                    StatusCode = 409,

                    Message =
                        MedicalRecordResponseMessageDTO
                            .MedicalRecordNotDraft
                };
            }


            // =================================================
            // CHECK SERVICE STATUS
            //
            // Chỉ Ordered mới được hủy.
            // =================================================

            if (
                medicalRecordService.Status !=
                (byte)MedicalRecordServiceStatus.Ordered
            )
            {
                return new HttpResponseData<MedicalRecordServiceDTO>
                {
                    StatusCode = 409,

                    Message =
                        MedicalRecordResponseMessageDTO
                            .MedicalRecordServiceNotOrdered
                };
            }


            // =================================================
            // CANCEL
            // =================================================

            medicalRecordService.Status =
                (byte)MedicalRecordServiceStatus.Cancelled;


            // =================================================
            // SAVE
            // =================================================

            await _unitOfWork
                .SaveChangesAsync();


            // =================================================
            // MAP RESPONSE
            // =================================================

            var result =
                new MedicalRecordServiceDTO
                {
                    Id =
                        medicalRecordService.Id,

                    ServiceId =
                        medicalRecordService.ServiceId,

                    ServiceName =
                        medicalRecordService.Service?.Name
                        ?? string.Empty,

                    Quantity =
                        medicalRecordService.Quantity,

                    UnitPriceSnapshot =
                        medicalRecordService
                            .UnitPriceSnapshot,

                    Status =
                        medicalRecordService.Status,

                    OrderedAt =
                        medicalRecordService.OrderedAt,

                    CompletedAt =
                        medicalRecordService.CompletedAt,

                    Result = null
                };


            // =================================================
            // SUCCESS
            // =================================================

            return new HttpResponseData<MedicalRecordServiceDTO>
            {
                StatusCode = 200,

                Message =
                    MedicalRecordResponseMessageDTO
                        .CancelServiceSuccess,

                Content =
                    result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to cancel medical record service. " +
                "MedicalRecordServiceId: {MedicalRecordServiceId}, " +
                "UserId: {UserId}",
                medicalRecordServiceId,
                currentUserId
            );

            return new HttpResponseData<MedicalRecordServiceDTO>
            {
                StatusCode = 500,

                Message =
                    MedicalRecordResponseMessageDTO
                        .CancelServiceFailed
            };
        }
    }

    // =====================================================
    // ADD MEDICAL RECORD SERVICE
    //
    // POST:
    // /api/medical-records/{id}/services
    //
    // Doctor hiện tại lấy từ JWT.
    // =====================================================

    public async Task<
        HttpResponseData<MedicalRecordServiceDTO>>
        AddMedicalRecordServiceAsync(
            int medicalRecordId,
            int currentUserId,
            MedicalRecordServiceRequestDTO request)
    {
        try
        {
            // =================================================
            // VALIDATE MEDICAL RECORD ID
            // =================================================

            if (medicalRecordId <= 0)
            {
                return new HttpResponseData<MedicalRecordServiceDTO>
                {
                    StatusCode = 400,
                    Message =
                        MedicalRecordResponseMessageDTO
                            .MedicalRecordNotFound
                };
            }


            // =================================================
            // VALIDATE USER
            // =================================================

            if (currentUserId <= 0)
            {
                return new HttpResponseData<MedicalRecordServiceDTO>
                {
                    StatusCode = 401,
                    Message =
                        MedicalRecordResponseMessageDTO
                            .MedicalRecordAccessDenied
                };
            }


            // =================================================
            // VALIDATE REQUEST
            // =================================================

            if (request == null)
            {
                return new HttpResponseData<MedicalRecordServiceDTO>
                {
                    StatusCode = 400,
                    Message =
                        MedicalRecordResponseMessageDTO
                            .InvalidMedicalRecordServiceRequest
                };
            }


            // =================================================
            // VALIDATE SERVICE ID
            // =================================================

            if (request.ServiceId <= 0)
            {
                return new HttpResponseData<MedicalRecordServiceDTO>
                {
                    StatusCode = 400,
                    Message =
                        MedicalRecordResponseMessageDTO
                            .ServiceNotFound
                };
            }


            // =================================================
            // VALIDATE QUANTITY
            // =================================================

            if (request.Quantity <= 0)
            {
                return new HttpResponseData<MedicalRecordServiceDTO>
                {
                    StatusCode = 400,
                    Message =
                        MedicalRecordResponseMessageDTO
                            .InvalidServiceQuantity
                };
            }


            // =================================================
            // GET CURRENT DOCTOR
            //
            // JWT UserId
            //      ↓
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
                return new HttpResponseData<MedicalRecordServiceDTO>
                {
                    StatusCode = 403,
                    Message =
                        MedicalRecordResponseMessageDTO
                            .MedicalRecordAccessDenied
                };
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
                return new HttpResponseData<MedicalRecordServiceDTO>
                {
                    StatusCode = 404,
                    Message =
                        MedicalRecordResponseMessageDTO
                            .MedicalRecordNotFound
                };
            }


            // =================================================
            // CHECK DOCTOR OWNERSHIP
            // =================================================

            if (medicalRecord.DoctorId != doctor.Id)
            {
                return new HttpResponseData<MedicalRecordServiceDTO>
                {
                    StatusCode = 403,
                    Message =
                        MedicalRecordResponseMessageDTO
                            .MedicalRecordAccessDenied
                };
            }


            // =================================================
            // CHECK MEDICAL RECORD STATUS
            //
            // Chỉ Draft mới được thêm chỉ định.
            // =================================================

            if (
                medicalRecord.Status !=
                (byte)MedicalRecordStatus.Draft
            )
            {
                return new HttpResponseData<MedicalRecordServiceDTO>
                {
                    StatusCode = 409,
                    Message =
                        MedicalRecordResponseMessageDTO
                            .MedicalRecordNotDraft
                };
            }


            // =================================================
            // GET SERVICE
            // =================================================

            var service =
                await _unitOfWork
                    .ServiceRepository
                    .WhereSql(
                        s =>
                            s.Id == request.ServiceId
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // SERVICE NOT FOUND
            // =================================================

            if (service == null)
            {
                return new HttpResponseData<MedicalRecordServiceDTO>
                {
                    StatusCode = 404,
                    Message =
                        MedicalRecordResponseMessageDTO
                            .ServiceNotFound
                };
            }


            // =================================================
            // CREATE MEDICAL RECORD SERVICE
            // =================================================

            var medicalRecordService =
         new MedicalRecordServiceEntity
         {
             MedicalRecordId =
                 medicalRecord.Id,

             ServiceId =
                 service.Id,

             Quantity =
                 request.Quantity,

             UnitPriceSnapshot =
                 service.Price,

             Status =
                 (byte)MedicalRecordServiceStatus.Ordered,

             OrderedAt =
                 DateTime.UtcNow
         };


            // =================================================
            // ADD
            // =================================================

            await _unitOfWork
                .MedicalRecordServiceRepository
                .AddAsync(
                    medicalRecordService
                );


            // =================================================
            // SAVE
            // =================================================

            await _unitOfWork
                .SaveChangesAsync();


            // =================================================
            // MAP RESPONSE
            // =================================================

            var result =
                new MedicalRecordServiceDTO
                {
                    Id =
                        medicalRecordService.Id,

                    ServiceId =
                        medicalRecordService.ServiceId,

                    ServiceName =
                        service.Name,

                    Quantity =
                        medicalRecordService.Quantity,

                    UnitPriceSnapshot =
                        medicalRecordService
                            .UnitPriceSnapshot,

                    Status =
                        medicalRecordService.Status,

                    OrderedAt =
                        medicalRecordService.OrderedAt,

                    CompletedAt =
                        medicalRecordService.CompletedAt,

                    Result = null
                };


            // =================================================
            // SUCCESS
            // =================================================

            return new HttpResponseData<MedicalRecordServiceDTO>
            {
                StatusCode = 201,

                Message =
                    MedicalRecordResponseMessageDTO
                        .AddServiceSuccess,

                Content =
                    result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to add medical record service. " +
                "MedicalRecordId: {MedicalRecordId}, " +
                "UserId: {UserId}, " +
                "ServiceId: {ServiceId}",
                medicalRecordId,
                currentUserId,
                request?.ServiceId
            );

            return new HttpResponseData<MedicalRecordServiceDTO>
            {
                StatusCode = 500,

                Message =
                    MedicalRecordResponseMessageDTO
                        .AddServiceFailed
            };
        }
    }

    // =====================================================
    // GET MEDICAL RECORD BY ID - DOCTOR
    //
    // GET:
    // /api/medical-records/{id}
    //
    // Doctor hiện tại lấy từ JWT.
    //
    // Cho phép đọc:
    // - Draft
    // - Finalized
    //
    // Response gồm:
    // - MedicalRecord
    // - Doctor
    // - PatientVital
    // =====================================================

    public async Task<
        HttpResponseData<MedicalRecordExamDTO>>
        GetMedicalRecordByIdForDoctorAsync(
            int medicalRecordId,
            int currentUserId)
    {
        try
        {
            // =================================================
            // VALIDATE MEDICAL RECORD ID
            // =================================================

            if (medicalRecordId <= 0)
            {
                return new HttpResponseData<MedicalRecordExamDTO>
                {
                    StatusCode = 400,

                    Message =
                        MedicalRecordResponseMessageDTO
                            .MedicalRecordNotFound
                };
            }


            // =================================================
            // GET CURRENT DOCTOR
            //
            // JWT UserId
            //      ↓
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
                return new HttpResponseData<MedicalRecordExamDTO>
                {
                    StatusCode = 404,

                    Message =
                        MedicalRecordResponseMessageDTO
                            .MedicalRecordNotFound
                };
            }


            // =================================================
            // GET MEDICAL RECORD
            //
            // Chỉ lấy MedicalRecord thuộc Doctor hiện tại.
            //
            // Include:
            // - Doctor
            // - PatientVital
            // - service
            // =================================================

            var medicalRecord =
    await _unitOfWork
        .MedicalRecordRepository
        .WhereSql(
            m =>
                m.Id == medicalRecordId &&
                m.DoctorId == doctor.Id
        )
        .Include(
            m => m.Doctor
        )
        .Include(
            m => m.PatientVital
        )
        .Include(
            m => m.MedicalRecordServices
        )
        .ThenInclude(
            s => s.Service
        )
        .Include(
            m => m.MedicalRecordServices
        )
        .ThenInclude(
            s => s.LabResult
        )
        .FirstOrDefaultAsync();


            // =================================================
            // MEDICAL RECORD NOT FOUND
            // =================================================

            if (medicalRecord == null)
            {
                return new HttpResponseData<MedicalRecordExamDTO>
                {
                    StatusCode = 404,

                    Message =
                        MedicalRecordResponseMessageDTO
                            .MedicalRecordNotFound
                };
            }


            // =================================================
            // MAP PATIENT VITAL
            // =================================================

            PatientVitalDTO? vital = null;

            if (medicalRecord.PatientVital != null)
            {
                vital =
                    new PatientVitalDTO
                    {
                        Id =
                            medicalRecord.PatientVital.Id,

                        MedicalRecordId =
                            medicalRecord.PatientVital
                                .MedicalRecordId,

                        Temperature =
                            medicalRecord.PatientVital
                                .Temperature,

                        Pulse =
                            medicalRecord.PatientVital
                                .Pulse,

                        BloodPressure =
                            medicalRecord.PatientVital
                                .BloodPressure,

                        Weight =
                            medicalRecord.PatientVital
                                .Weight,

                        Height =
                            medicalRecord.PatientVital
                                .Height
                    };
            }

            // =====================================================
            // MAP MEDICAL RECORD SERVICES
            // =====================================================

            var serviceDtos =
                medicalRecord.MedicalRecordServices
                    .OrderBy(s => s.Id)
                    .Select(
                        s => new MedicalRecordServiceDTO
                        {
                            Id =
                                s.Id,

                            ServiceId =
                                s.ServiceId,

                            ServiceName =
                                s.Service?.Name
                                ?? string.Empty,

                            Quantity =
                                s.Quantity,

                            UnitPriceSnapshot =
                                s.UnitPriceSnapshot,

                            Status =
                                s.Status,

                            OrderedAt =
                                s.OrderedAt,

                            CompletedAt =
                                s.CompletedAt,

                            Result =
                                s.LabResult == null
                                    ? null
                                    : new LabResultDTO
                                    {
                                        Id =
                                            s.LabResult.Id,

                                        ResultValue =
                                            s.LabResult.ResultValue,

                                        ReferenceRange =
                                            s.LabResult.ReferenceRange,

                                        Conclusion =
                                            s.LabResult.Conclusion,

                                        ResultedAt =
                                            s.LabResult.CreatedAt
                                    }
                        }
                    )
                    .ToList();


            // =================================================
            // MAP MEDICAL RECORD EXAM DTO
            // =================================================

            var result =
                new MedicalRecordExamDTO
                {
                    Id =
                        medicalRecord.Id,

                    AppointmentId =
                        medicalRecord.AppointmentId,

                    DoctorName =
                        medicalRecord.Doctor?.FullName
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
                        medicalRecord.CreatedAt,

                    Vital = vital,

                    Services = serviceDtos
                };


            // =================================================
            // SUCCESS
            // =================================================

            return new HttpResponseData<MedicalRecordExamDTO>
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
                "Failed to get medical record for doctor. " +
                "MedicalRecordId: {MedicalRecordId}, " +
                "UserId: {UserId}",
                medicalRecordId,
                currentUserId
            );


            // =================================================
            // ERROR RESPONSE
            // =================================================

            return new HttpResponseData<MedicalRecordExamDTO>
            {
                StatusCode = 500,

                Message =
                    MedicalRecordResponseMessageDTO
                        .GetFailed
            };
        }
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