using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Pharmacy;
using ClinicManagementSystem.Application.DTOs.Prescription;
using ClinicManagementSystem.Application.Enums;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Application.Services;


// =====================================================
// PHARMACY SERVICE CONTRACT
// =====================================================

public interface IPharmacyService
{
    Task<HttpResponseData<PrescriptionResponseDTO>>
        DispensePrescriptionAsync(
            int prescriptionId,
            int currentUserId,
            DispenseRequestDTO request);

    Task<HttpResponseData<bool>>
        ReportShortageAsync(
            int prescriptionId,
            int currentUserId,
            ReportShortageRequestDTO request);

    Task<HttpResponseData<PharmacyPrescriptionListResponseDTO>>
    GetPendingPrescriptionsAsync(
        string? keyword,
        int pageNumber,
        int pageSize);

    Task<HttpResponseData<PharmacyPrescriptionDetailResponseDTO>>
    GetPrescriptionDetailAsync(
        int prescriptionId);

    Task<HttpResponseData<bool>>
    CreateMedicineReceiptAsync(
        int currentUserId,
        MedicineReceiptRequestDTO request);

    Task<HttpResponseData<List<MedicineDTO>>>
        SearchMedicinesAsync(
            string keyword);

}


// =====================================================
// PHARMACY SERVICE
// =====================================================

public class PharmacyService
    : IPharmacyService
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly ILogger<PharmacyService> _logger;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public PharmacyService(
        IUnitOfWork unitOfWork,
        ILogger<PharmacyService> logger)
    {
        _unitOfWork = unitOfWork;

        _logger = logger;
    }

    // =====================================================
    // SEARCH MEDICINES
    //
    // GET:
    // /api/medicines?keyword=
    //
    // Tìm theo:
    // - Mã thuốc
    // - Tên thuốc
    // - Hoạt chất
    //
    // Chỉ lấy thuốc đang active.
    // Tối đa 20 kết quả.
    // =====================================================

    public async Task<
        HttpResponseData<List<MedicineDTO>>>
        SearchMedicinesAsync(
            string keyword)
    {
        try
        {
            // =================================================
            // VALIDATE KEYWORD
            // =================================================

            if (string.IsNullOrWhiteSpace(keyword))
            {
                return new HttpResponseData<
                    List<MedicineDTO>>
                {
                    StatusCode = 400,

                    Message =
                        "Từ khóa tìm thuốc không được để trống.",

                    Content =
                        []
                };
            }


            // =================================================
            // NORMALIZE KEYWORD
            // =================================================

            keyword =
                keyword.Trim();


            // =================================================
            // SEARCH PATTERN
            // =================================================

            var pattern =
                $"%{keyword}%";


            // =================================================
            // GET MEDICINES
            //
            // Chỉ lấy thuốc active.
            //
            // Search:
            // - code
            // - name
            // - active_ingredient
            // =================================================

            var medicines =
                await _unitOfWork
                    .MedicineRepository
                    .WhereSql(
                        m =>
                            m.IsActive &&
                            (
                                EF.Functions.Like(
                                    m.Name,
                                    pattern
                                )
                                ||
                                (
                                    m.Code != null &&
                                    EF.Functions.Like(
                                        m.Code,
                                        pattern
                                    )
                                )
                                ||
                                (
                                    m.ActiveIngredient != null &&
                                    EF.Functions.Like(
                                        m.ActiveIngredient,
                                        pattern
                                    )
                                )
                            )
                    )
                    .OrderBy(
                        m => m.Name
                    )
                    .ThenBy(
                        m => m.Id
                    )
                    .Take(20)
                    .ToListAsync();


            // =================================================
            // MAP DTO
            // =================================================

            var result =
                medicines
                    .Select(
                        m =>
                            new MedicineDTO
                            {
                                Id =
                                    m.Id,

                                Code =
                                    m.Code,

                                Name =
                                    m.Name,

                                ActiveIngredient =
                                    m.ActiveIngredient,

                                Concentration =
                                    m.Concentration,

                                Unit =
                                    m.Unit,

                                Price =
                                    m.Price,

                                StockQuantity =
                                    m.StockQuantity,

                                IsActive =
                                    m.IsActive
                            }
                    )
                    .ToList();


            // =================================================
            // NO RESULT
            // =================================================

            if (result.Count == 0)
            {
                return new HttpResponseData<
                    List<MedicineDTO>>
                {
                    StatusCode = 200,

                    Message =
                        "Không tìm thấy thuốc phù hợp.",

                    Content =
                        []
                };
            }


            // =================================================
            // SUCCESS
            // =================================================

            return new HttpResponseData<
                List<MedicineDTO>>
            {
                StatusCode = 200,

                Message =
                    "Tìm thuốc thành công.",

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
                "Failed to search medicines. " +
                "Keyword: {Keyword}",
                keyword
            );


            // =================================================
            // ERROR RESPONSE
            // =================================================

            return new HttpResponseData<
                List<MedicineDTO>>
            {
                StatusCode = 500,

                Message =
                    "Không thể tìm thuốc.",

                Content =
                    []
            };
        }
    }

    // =====================================================
    // CREATE MEDICINE RECEIPT
    //
    // POST:
    // /api/medicines/receipts
    //
    // - Tạo mới hoặc cộng vào medicine_batches
    // - Cập nhật medicines.stock_quantity
    // - Tạo medicine_stock_transactions với Type = IN
    // - Tất cả nằm trong cùng transaction
    // =====================================================
    // =====================================================
    // CREATE MEDICINE RECEIPT
    //
    // POST:
    // /api/medicines/receipts
    //
    // - Tạo mới hoặc cộng vào medicine_batches
    // - Cập nhật medicines.stock_quantity
    // - Tạo medicine_stock_transactions
    // - Type = 0 = IN
    // - Tất cả xử lý trong cùng transaction
    // =====================================================

    public async Task<
        HttpResponseData<bool>>
        CreateMedicineReceiptAsync(
            int currentUserId,
            MedicineReceiptRequestDTO request)
    {
        bool transactionStarted = false;

        try
        {
            // =================================================
            // VALIDATE USER
            // =================================================

            if (currentUserId <= 0)
            {
                return new HttpResponseData<bool>
                {
                    StatusCode = 401,

                    Message =
                        "Không xác định được người nhập thuốc."
                };
            }


            // =================================================
            // VALIDATE REQUEST
            // =================================================

            if (
                request == null ||
                request.Items == null ||
                request.Items.Count == 0
            )
            {
                return new HttpResponseData<bool>
                {
                    StatusCode = 400,

                    Message =
                        "Danh sách thuốc nhập không được rỗng."
                };
            }


            // =================================================
            // VALIDATE ITEMS
            // =================================================

            foreach (var item in request.Items)
            {
                if (
                    item.MedicineId <= 0 ||
                    string.IsNullOrWhiteSpace(
                        item.BatchNo) ||
                    item.Quantity <= 0 ||
                    item.ImportPrice < 0
                )
                {
                    return new HttpResponseData<bool>
                    {
                        StatusCode = 400,

                        Message =
                            "Thông tin thuốc nhập không hợp lệ."
                    };
                }
            }


            // =================================================
            // GET MEDICINE IDS
            // =================================================

            var medicineIds =
                request.Items
                    .Select(
                        x => x.MedicineId
                    )
                    .Distinct()
                    .ToList();


            // =================================================
            // GET MEDICINES
            // =================================================

            var medicines =
                await _unitOfWork
                    .MedicineRepository
                    .WhereSql(
                        m =>
                            medicineIds.Contains(
                                m.Id
                            )
                    )
                    .ToListAsync();


            // =================================================
            // CHECK MEDICINES EXIST
            // =================================================

            if (
                medicines.Count !=
                medicineIds.Count
            )
            {
                return new HttpResponseData<bool>
                {
                    StatusCode = 404,

                    Message =
                        "Không tìm thấy thuốc."
                };
            }


            // =================================================
            // CHECK MEDICINES ACTIVE
            // =================================================

            var inactiveMedicine =
                medicines.FirstOrDefault(
                    m => !m.IsActive
                );


            if (inactiveMedicine != null)
            {
                return new HttpResponseData<bool>
                {
                    StatusCode = 409,

                    Message =
                        $"Thuốc {inactiveMedicine.Name} " +
                        "đã ngừng hoạt động."
                };
            }


            // =================================================
            // BEGIN TRANSACTION
            // =================================================

            await _unitOfWork
                .BeginTransactionAsync();

            transactionStarted = true;


            // =================================================
            // PREPARE STOCK TRANSACTIONS
            //
            // Lưu tạm batch + quantity before.
            //
            // Batch mới chưa có Id cho tới khi SaveChanges.
            // =================================================

            var pendingTransactions =
                new List<
                    (
                        MedicineBatch Batch,
                        MedicineStockTransaction Transaction
                    )
                >();


            // =================================================
            // PROCESS EACH ITEM
            // =================================================

            foreach (var item in request.Items)
            {
                // =============================================
                // GET MEDICINE
                // =============================================

                var medicine =
                    medicines.First(
                        m =>
                            m.Id ==
                            item.MedicineId
                    );


                // =============================================
                // NORMALIZE BATCH NO
                // =============================================

                var batchNo =
                    item.BatchNo.Trim();


                // =============================================
                // EXPIRY DATE
                // =============================================

                var expiryDate =
                    DateOnly.FromDateTime(
                        item.ExpiryDate
                    );


                // =============================================
                // FIND EXISTING BATCH
                //
                // Unique:
                // (medicine_id, batch_no)
                // =============================================

                var batch =
                    await _unitOfWork
                        .MedicineBatchRepository
                        .WhereSql(
                            b =>
                                b.MedicineId ==
                                    medicine.Id
                                &&
                                b.BatchNo ==
                                    batchNo
                        )
                        .FirstOrDefaultAsync();


                // =============================================
                // BATCH QUANTITY BEFORE
                // =============================================

                var batchQuantityBefore =
                    batch?.Quantity ?? 0;


                // =============================================
                // CREATE NEW BATCH
                // =============================================

                if (batch == null)
                {
                    batch =
                        new MedicineBatch
                        {
                            MedicineId =
                                medicine.Id,

                            BatchNo =
                                batchNo,

                            ExpiryDate =
                                expiryDate,

                            Quantity =
                                item.Quantity,

                            ImportPrice =
                                item.ImportPrice
                        };


                    await _unitOfWork
                        .MedicineBatchRepository
                        .AddAsync(
                            batch
                        );
                }
                else
                {
                    // =========================================
                    // EXISTING BATCH
                    //
                    // Cộng thêm số lượng vào lô.
                    // =========================================

                    batch.Quantity =
                        batchQuantityBefore +
                        item.Quantity;
                }


                // =============================================
                // MEDICINE TOTAL STOCK BEFORE
                // =============================================

                var medicineStockBefore =
                    medicine.StockQuantity;


                // =============================================
                // UPDATE TOTAL STOCK
                // =============================================

                medicine.StockQuantity =
                    medicineStockBefore +
                    item.Quantity;


                medicine.UpdatedAt =
                    DateTime.Now;


                // =============================================
                // BUILD NOTE
                // =============================================

                var noteParts =
                    new List<string>
                    {
                    "Nhập thuốc"
                    };


                if (
                    !string.IsNullOrWhiteSpace(
                        request.SupplierName)
                )
                {
                    noteParts.Add(
                        $"NCC: {request.SupplierName.Trim()}"
                    );
                }


                if (
                    !string.IsNullOrWhiteSpace(
                        request.ReferenceCode)
                )
                {
                    noteParts.Add(
                        $"Số HĐ: {request.ReferenceCode.Trim()}"
                    );
                }


                noteParts.Add(
                    $"Lô: {batchNo}"
                );


                // =============================================
                // CREATE PENDING TRANSACTION
                //
                // Chưa gán BatchId ở đây vì batch mới có thể
                // chưa được DB cấp Id.
                // =============================================

                var stockTransaction =
                    new MedicineStockTransaction
                    {
                        MedicineId =
                            medicine.Id,

                        Type = 0,

                        Quantity =
                            item.Quantity,

                        QuantityBefore =
                            batchQuantityBefore,

                        QuantityAfter =
                            batchQuantityBefore +
                            item.Quantity,

                        PrescriptionId =
                            null,

                        ReversalOfTransactionId =
                            null,

                        CreatedBy =
                            currentUserId,

                        CreatedAt =
                            DateTime.Now,

                        Note =
                            string.Join(
                                " | ",
                                noteParts
                            )
                    };


                pendingTransactions.Add(
                    (
                        batch,
                        stockTransaction
                    )
                );
            }


            // =================================================
            // SAVE BATCHES + MEDICINES
            //
            // Sau SaveChanges:
            // batch mới sẽ có Id.
            // =================================================

            await _unitOfWork
                .SaveChangesAsync();


            // =================================================
            // CREATE STOCK TRANSACTIONS
            // =================================================

            foreach (
                var pending
                in pendingTransactions
            )
            {
                pending.Transaction.BatchId =
                    pending.Batch.Id;


                await _unitOfWork
                    .MedicineStockTransactionRepository
                    .AddAsync(
                        pending.Transaction
                    );
            }


            // =================================================
            // SAVE STOCK TRANSACTIONS
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
            // SUCCESS
            // =================================================

            return new HttpResponseData<bool>
            {
                StatusCode = 200,

                Message =
                    "Nhập thuốc thành công.",

                Content =
                    true
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

                        "Rollback medicine receipt failed. " +
                        "UserId: {UserId}",

                        currentUserId
                    );
                }
            }


            // =================================================
            // LOG
            // =================================================

            _logger.LogError(
                ex,

                "Failed to create medicine receipt. " +
                "UserId: {UserId}",

                currentUserId
            );


            // =================================================
            // ERROR RESPONSE
            // =================================================

            return new HttpResponseData<bool>
            {
                StatusCode = 500,

                Message =
                    "Không thể nhập thuốc.",

                Content =
                    false
            };
        }
    }


    // =====================================================
    // GET PRESCRIPTION DETAIL
    //
    // GET:
    // /api/pharmacy/prescriptions/{id}
    //
    // Trả về:
    // - Prescription
    // - Patient
    // - Doctor
    // - Prescription Items
    // - Available Stock
    // - FEFO Batches
    // =====================================================

    public async Task<
        HttpResponseData<PharmacyPrescriptionDetailResponseDTO>>
        GetPrescriptionDetailAsync(
            int prescriptionId)
    {
        try
        {
            // =================================================
            // VALIDATE ID
            // =================================================

            if (prescriptionId <= 0)
            {
                return new HttpResponseData<
                    PharmacyPrescriptionDetailResponseDTO>
                {
                    StatusCode = 400,

                    Message =
                        PharmacyResponseMessageDTO
                            .PrescriptionIdInvalid
                };
            }


            // =================================================
            // GET PRESCRIPTION
            // =================================================

            var prescription =
                await _unitOfWork
                    .PrescriptionRepository
                    .WhereSql(
                        p =>
                            p.Id == prescriptionId
                    )
                    .FirstOrDefaultAsync();


            if (prescription == null)
            {
                return new HttpResponseData<
                    PharmacyPrescriptionDetailResponseDTO>
                {
                    StatusCode = 404,

                    Message =
                        PharmacyResponseMessageDTO
                            .PrescriptionNotFound
                };
            }


            // =================================================
            // CHECK STATUS
            //
            // Pharmacy chỉ xử lý đơn đã Finalized.
            // =================================================

            if (
                prescription.Status !=
                (byte)PrescriptionStatus.Finalized
            )
            {
                return new HttpResponseData<
                    PharmacyPrescriptionDetailResponseDTO>
                {
                    StatusCode = 409,

                    Message =
                        PharmacyResponseMessageDTO
                            .PrescriptionNotAvailableForPharmacy
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
                            prescription.MedicalRecordId
                    )
                    .FirstOrDefaultAsync();


            if (medicalRecord == null)
            {
                return new HttpResponseData<
                    PharmacyPrescriptionDetailResponseDTO>
                {
                    StatusCode = 404,

                    Message =
                        PharmacyResponseMessageDTO
                            .MedicalRecordNotFound
                };
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
                            medicalRecord.PatientId
                    )
                    .FirstOrDefaultAsync();


            if (patient == null)
            {
                return new HttpResponseData<
                    PharmacyPrescriptionDetailResponseDTO>
                {
                    StatusCode = 404,

                    Message =
                        "Không tìm thấy bệnh nhân."
                };
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
                            prescription.DoctorId
                    )
                    .FirstOrDefaultAsync();


            if (doctor == null)
            {
                return new HttpResponseData<
                    PharmacyPrescriptionDetailResponseDTO>
                {
                    StatusCode = 404,

                    Message =
                        "Không tìm thấy bác sĩ."
                };
            }


            // =================================================
            // GET PRESCRIPTION ITEMS
            // =================================================

            var prescriptionItems =
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


            if (prescriptionItems.Count == 0)
            {
                return new HttpResponseData<
                    PharmacyPrescriptionDetailResponseDTO>
                {
                    StatusCode = 409,

                    Message =
                        PharmacyResponseMessageDTO
                            .PrescriptionHasNoItems
                };
            }


            // =================================================
            // CURRENT DATE
            // =================================================

            var today =
                DateOnly.FromDateTime(
                    DateTime.Now
                );


            // =================================================
            // GET MEDICINE BATCHES
            //
            // Chỉ lấy:
            // - còn hàng
            // - chưa hết hạn
            //
            // FEFO:
            // expiry_date ASC
            // id ASC
            // =================================================

            var medicineIds =
                prescriptionItems
                    .Select(
                        i => i.MedicineId
                    )
                    .Distinct()
                    .ToList();


            var batches =
                await _unitOfWork
                    .MedicineBatchRepository
                    .WhereSql(
                        b =>
                            medicineIds.Contains(
                                b.MedicineId
                            )
                            &&
                            b.Quantity > 0
                            &&
                            b.ExpiryDate >= today
                    )
                    .OrderBy(
                        b => b.ExpiryDate
                    )
                    .ThenBy(
                        b => b.Id
                    )
                    .ToListAsync();


            // =================================================
            // GROUP BATCHES BY MEDICINE
            // =================================================

            var batchesByMedicine =
                batches
                    .GroupBy(
                        b => b.MedicineId
                    )
                    .ToDictionary(
                        g => g.Key,
                        g => g.ToList()
                    );


            // =================================================
            // MAP ITEMS
            // =================================================

            var itemDtos =
                prescriptionItems
                    .Select(
                        item =>
                        {
                            batchesByMedicine.TryGetValue(
                                item.MedicineId,
                                out var medicineBatches
                            );


                            medicineBatches ??=
                                new List<MedicineBatch>();


                            return new PharmacyPrescriptionDetailItemDTO
                            {
                                PrescriptionItemId =
                                    item.Id,

                                MedicineId =
                                    item.MedicineId,

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
                                    item.Frequency,

                                AvailableQuantity =
                                    medicineBatches.Sum(
                                        b => b.Quantity
                                    ),

                                Batches =
                                    medicineBatches
                                        .Select(
                                            batch =>
                                                new PharmacyPrescriptionBatchDTO
                                                {
                                                    BatchId =
                                                        batch.Id,

                                                    BatchNo =
                                                        batch.BatchNo,

                                                    ExpiryDate =
                                                        batch.ExpiryDate,

                                                    Quantity =
                                                        batch.Quantity
                                                }
                                        )
                                        .ToList()
                            };
                        }
                    )
                    .ToList();


            // =================================================
            // MAP RESPONSE
            // =================================================

            var result =
                new PharmacyPrescriptionDetailResponseDTO
                {
                    PrescriptionId =
                        prescription.Id,

                    MedicalRecordId =
                        prescription.MedicalRecordId,

                    Status =
                        prescription.Status,

                    Note =
                        prescription.Note,

                    CreatedAt =
                        prescription.CreatedAt,

                    DispensedAt =
                        prescription.DispensedAt,

                    PatientId =
                        patient.Id,

                    PatientCode =
                        patient.PatientCode,

                    PatientName =
                        patient.FullName,

                    DoctorName =
                        doctor.FullName,

                    Items =
                        itemDtos
                };


            // =================================================
            // SUCCESS
            // =================================================

            return new HttpResponseData<
                PharmacyPrescriptionDetailResponseDTO>
            {
                StatusCode = 200,

                Message =
                    PharmacyResponseMessageDTO
                        .GetPrescriptionDetailSuccess,

                Content =
                    result
            };
        }
        catch (Exception ex)
        {
            // =================================================
            // LOG
            // =================================================

            _logger.LogError(
                ex,
                "Failed to get pharmacy prescription detail. " +
                "PrescriptionId: {PrescriptionId}",
                prescriptionId
            );


            // =================================================
            // FAILED
            // =================================================

            return new HttpResponseData<
                PharmacyPrescriptionDetailResponseDTO>
            {
                StatusCode = 500,

                Message =
                    PharmacyResponseMessageDTO
                        .GetPrescriptionDetailFailed
            };
        }
    }

    // =====================================================
    // GET PENDING PRESCRIPTIONS
    //
    // GET:
    // /api/pharmacy/prescriptions
    //
    // Chỉ lấy Prescription:
    // Status = Finalized
    //
    // Search:
    // - PrescriptionId
    // - PatientCode
    // - PatientName
    //
    // Pagination:
    // - pageNumber
    // - pageSize
    // =====================================================

    public async Task<
        HttpResponseData<PharmacyPrescriptionListResponseDTO>>
        GetPendingPrescriptionsAsync(
            string? keyword,
            int pageNumber,
            int pageSize)
    {
        try
        {
            // =================================================
            // NORMALIZE PAGINATION
            // =================================================

            if (pageNumber <= 0)
            {
                pageNumber = 1;
            }

            if (pageSize <= 0)
            {
                pageSize = 20;
            }

            // Không cho client request quá nhiều record.
            if (pageSize > 100)
            {
                pageSize = 100;
            }


            // =================================================
            // NORMALIZE KEYWORD
            // =================================================

            keyword =
                string.IsNullOrWhiteSpace(keyword)
                    ? null
                    : keyword.Trim();


            // =================================================
            // BASE QUERY
            //
            // Chỉ đơn đã Finalized mới nằm trong hàng chờ.
            // =================================================

            var prescriptionQuery =
                _unitOfWork
                    .PrescriptionRepository
                    .WhereSql(
                        p =>
                            p.Status ==
                            (byte)PrescriptionStatus.Finalized
                    );


            // =================================================
            // SEARCH
            //
            // Nếu keyword là số:
            // → tìm theo PrescriptionId
            //
            // Đồng thời tìm:
            // → PatientCode
            // → PatientName
            // =================================================

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var patientIds =
                    await _unitOfWork
                        .PatientRepository
                        .WhereSql(
                            p =>
                                p.PatientCode.Contains(
                                    keyword
                                )
                                ||
                                p.FullName.Contains(
                                    keyword
                                )
                        )
                        .Select(
                            p => p.Id
                        )
                        .ToListAsync();


                var medicalRecordIds =
                    new List<int>();


                if (patientIds.Count > 0)
                {
                    medicalRecordIds =
                        await _unitOfWork
                            .MedicalRecordRepository
                            .WhereSql(
                                m =>
                                    patientIds.Contains(
                                        m.PatientId
                                    )
                            )
                            .Select(
                                m => m.Id
                            )
                            .ToListAsync();
                }


                if (
                    int.TryParse(
                        keyword,
                        out var prescriptionId
                    )
                )
                {
                    prescriptionQuery =
                        prescriptionQuery.Where(
                            p =>
                                p.Id ==
                                    prescriptionId
                                ||
                                medicalRecordIds.Contains(
                                    p.MedicalRecordId
                                )
                        );
                }
                else
                {
                    prescriptionQuery =
                        prescriptionQuery.Where(
                            p =>
                                medicalRecordIds.Contains(
                                    p.MedicalRecordId
                                )
                        );
                }
            }


            // =================================================
            // TOTAL COUNT
            // =================================================

            var totalCount =
                await prescriptionQuery
                    .CountAsync();


            // =================================================
            // GET CURRENT PAGE
            //
            // Mới nhất trước.
            // =================================================

            var prescriptions =
                await prescriptionQuery
                    .OrderByDescending(
                        p => p.CreatedAt
                    )
                    .ThenByDescending(
                        p => p.Id
                    )
                    .Skip(
                        (pageNumber - 1) *
                        pageSize
                    )
                    .Take(
                        pageSize
                    )
                    .ToListAsync();


            // =================================================
            // EMPTY PAGE
            // =================================================

            if (prescriptions.Count == 0)
            {
                var emptyResult =
                    new PharmacyPrescriptionListResponseDTO
                    {
                        Items = new(),

                        PageNumber =
                            pageNumber,

                        PageSize =
                            pageSize,

                        TotalCount =
                            totalCount,

                        TotalPages =
                            totalCount == 0
                                ? 0
                                : (int)Math.Ceiling(
                                    totalCount /
                                    (double)pageSize
                                )
                    };


                return new HttpResponseData<
                    PharmacyPrescriptionListResponseDTO>
                {
                    StatusCode = 200,

                    Message =
                        PharmacyResponseMessageDTO
                            .GetPendingPrescriptionsSuccess,

                    Content =
                        emptyResult
                };
            }


            // =================================================
            // GET MEDICAL RECORDS
            // =================================================

            var medicalRecordIdsForPage =
                prescriptions
                    .Select(
                        p => p.MedicalRecordId
                    )
                    .Distinct()
                    .ToList();


            var medicalRecords =
                await _unitOfWork
                    .MedicalRecordRepository
                    .WhereSql(
                        m =>
                            medicalRecordIdsForPage.Contains(
                                m.Id
                            )
                    )
                    .ToListAsync();


            // =================================================
            // GET PATIENTS
            // =================================================

            var patientIdsForPage =
                medicalRecords
                    .Select(
                        m => m.PatientId
                    )
                    .Distinct()
                    .ToList();


            var patients =
                await _unitOfWork
                    .PatientRepository
                    .WhereSql(
                        p =>
                            patientIdsForPage.Contains(
                                p.Id
                            )
                    )
                    .ToListAsync();


            // =================================================
            // GET DOCTORS
            // =================================================

            var doctorIdsForPage =
                prescriptions
                    .Select(
                        p => p.DoctorId
                    )
                    .Distinct()
                    .ToList();


            var doctors =
                await _unitOfWork
                    .DoctorRepository
                    .WhereSql(
                        d =>
                            doctorIdsForPage.Contains(
                                d.Id
                            )
                    )
                    .ToListAsync();


            // =================================================
            // GET PRESCRIPTION ITEMS
            //
            // Chỉ cần đếm số thuốc trong mỗi đơn.
            // =================================================

            var prescriptionIdsForPage =
                prescriptions
                    .Select(
                        p => p.Id
                    )
                    .ToList();


            var prescriptionItems =
                await _unitOfWork
                    .PrescriptionItemRepository
                    .WhereSql(
                        i =>
                            prescriptionIdsForPage.Contains(
                                i.PrescriptionId
                            )
                    )
                    .ToListAsync();


            // =================================================
            // MAP LOOKUP
            // =================================================

            var medicalRecordMap =
                medicalRecords
                    .ToDictionary(
                        m => m.Id
                    );


            var patientMap =
                patients
                    .ToDictionary(
                        p => p.Id
                    );


            var doctorMap =
                doctors
                    .ToDictionary(
                        d => d.Id
                    );


            var itemCountMap =
                prescriptionItems
                    .GroupBy(
                        i => i.PrescriptionId
                    )
                    .ToDictionary(
                        g => g.Key,
                        g => g.Count()
                    );


            // =================================================
            // MAP DTO
            // =================================================

            var items =
                prescriptions
                    .Select(
                        prescription =>
                        {
                            medicalRecordMap.TryGetValue(
                                prescription.MedicalRecordId,
                                out var medicalRecord
                            );


                            Patient? patient = null;


                            if (medicalRecord != null)
                            {
                                patientMap.TryGetValue(
                                    medicalRecord.PatientId,
                                    out patient
                                );
                            }


                            doctorMap.TryGetValue(
                                prescription.DoctorId,
                                out var doctor
                            );


                            itemCountMap.TryGetValue(
                                prescription.Id,
                                out var itemCount
                            );


                            return new PharmacyPrescriptionListItemDTO
                            {
                                PrescriptionId =
                                    prescription.Id,

                                MedicalRecordId =
                                    prescription.MedicalRecordId,

                                Status =
                                    prescription.Status,

                                CreatedAt =
                                    prescription.CreatedAt,

                                PatientId =
                                    medicalRecord?.PatientId
                                    ?? 0,

                                PatientCode =
                                    patient?.PatientCode
                                    ?? string.Empty,

                                PatientName =
                                    patient?.FullName
                                    ?? string.Empty,

                                DoctorName =
                                    doctor?.FullName
                                    ?? string.Empty,

                                ItemCount =
                                    itemCount
                            };
                        }
                    )
                    .ToList();


            // =================================================
            // PAGINATION
            // =================================================

            var totalPages =
                totalCount == 0
                    ? 0
                    : (int)Math.Ceiling(
                        totalCount /
                        (double)pageSize
                    );


            var result =
                new PharmacyPrescriptionListResponseDTO
                {
                    Items =
                        items,

                    PageNumber =
                        pageNumber,

                    PageSize =
                        pageSize,

                    TotalCount =
                        totalCount,

                    TotalPages =
                        totalPages
                };


            // =================================================
            // SUCCESS
            // =================================================

            return new HttpResponseData<
                PharmacyPrescriptionListResponseDTO>
            {
                StatusCode = 200,

                Message =
                    PharmacyResponseMessageDTO
                        .GetPendingPrescriptionsSuccess,

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
                "Failed to get pending pharmacy prescriptions. " +
                "Keyword: {Keyword}, " +
                "PageNumber: {PageNumber}, " +
                "PageSize: {PageSize}",
                keyword,
                pageNumber,
                pageSize
            );


            // =================================================
            // FAILED
            // =================================================

            return new HttpResponseData<
                PharmacyPrescriptionListResponseDTO>
            {
                StatusCode = 500,

                Message =
                    PharmacyResponseMessageDTO
                        .GetPendingPrescriptionsFailed
            };
        }
    }


    // =====================================================
    // DISPENSE PRESCRIPTION
    //
    // POST:
    // /api/prescriptions/{id}/dispense
    //
    // FEFO:
    // expiry_date tăng dần
    //
    // Nếu lô đầu không đủ:
    // → tự chuyển sang lô kế tiếp.
    // =====================================================

    public async Task<
        HttpResponseData<PrescriptionResponseDTO>>
        DispensePrescriptionAsync(
            int prescriptionId,
            int currentUserId,
            DispenseRequestDTO request)
    {
        bool transactionStarted = false;

        try
        {
            // =================================================
            // VALIDATE ID
            // =================================================

            if (prescriptionId <= 0)
            {
                return Response(
                    400,
                    PharmacyResponseMessageDTO
                        .PrescriptionIdInvalid
                );
            }


            // =================================================
            // VALIDATE USER
            // =================================================

            if (currentUserId <= 0)
            {
                return Response(
                    401,
                    PharmacyResponseMessageDTO
                        .DispenseUserInvalid
                );
            }


            // =================================================
            // VALIDATE REQUEST
            // =================================================

            if (request == null ||
                request.Items == null ||
                request.Items.Count == 0)
            {
                return Response(
                    400,
                    PharmacyResponseMessageDTO
                        .DispenseRequestEmpty
                );
            }


            foreach (var item in request.Items)
            {
                if (item.PrescriptionItemId <= 0 ||
                    item.BatchId <= 0 ||
                    item.Quantity <= 0)
                {
                    return Response(
                        400,
                        PharmacyResponseMessageDTO
                            .DispenseRequestInvalid
                    );
                }
            }


            // =================================================
            // GET PRESCRIPTION
            // =================================================

            var prescription =
                await _unitOfWork
                    .PrescriptionRepository
                    .WhereSql(
                        p =>
                            p.Id == prescriptionId
                    )
                    .FirstOrDefaultAsync();


            if (prescription == null)
            {
                return Response(
                    404,
                    PharmacyResponseMessageDTO
                        .PrescriptionNotFound
                );
            }


            // =================================================
            // CHECK PRESCRIPTION STATUS
            //
            // Chỉ Finalized mới được phát.
            //
            // 0 = Draft
            // 1 = Finalized
            // 2 = Dispensed
            // 3 = Cancelled
            // =================================================

            if (
                prescription.Status ==
                (byte)PrescriptionStatus.Dispensed
            )
            {
                return Response(
                    409,
                    PharmacyResponseMessageDTO
                        .PrescriptionAlreadyDispensed
                );
            }


            if (
                prescription.Status !=
                (byte)PrescriptionStatus.Finalized
            )
            {
                return Response(
                    409,
                    PharmacyResponseMessageDTO
                        .PrescriptionNotFinalized
                );
            }


            // =================================================
            // GET PRESCRIPTION ITEMS
            // =================================================

            var prescriptionItems =
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


            if (prescriptionItems.Count == 0)
            {
                return Response(
                    409,
                    PharmacyResponseMessageDTO
                        .PrescriptionHasNoItems
                );
            }


            // =================================================
            // VALIDATE REQUEST ITEMS
            //
            // Tổng quantity request của mỗi
            // PrescriptionItem phải đúng bằng
            // quantity được kê.
            // =================================================

            var requestedGroups =
                request.Items
                    .GroupBy(
                        x =>
                            x.PrescriptionItemId
                    )
                    .ToDictionary(
                        g => g.Key,
                        g => g.ToList()
                    );


            foreach (var prescriptionItem
                in prescriptionItems)
            {
                if (!requestedGroups.TryGetValue(
                    prescriptionItem.Id,
                    out var allocations))
                {
                    return Response(
                        400,
                        $"{PharmacyResponseMessageDTO.DispenseItemMissing} " +
                        $"#{prescriptionItem.Id}."
                    );
                }


                var requestedQuantity =
                    allocations.Sum(
                        x => x.Quantity
                    );


                if (
                    requestedQuantity !=
                    prescriptionItem.Quantity
                )
                {
                    return Response(
                        400,
                        $"{PharmacyResponseMessageDTO.DispenseQuantityInvalid} " +
                        $"{prescriptionItem.MedicineNameSnapshot}."
                    );
                }
            }


            // =================================================
            // CHECK EXTRA PRESCRIPTION ITEM
            // =================================================

            var prescriptionItemIds =
                prescriptionItems
                    .Select(
                        x => x.Id
                    )
                    .ToHashSet();


            if (
                requestedGroups.Keys.Any(
                    id =>
                        !prescriptionItemIds.Contains(id)
                )
            )
            {
                return Response(
                    400,
                    PharmacyResponseMessageDTO
                        .PrescriptionItemInvalid
                );
            }


            // =================================================
            // CURRENT DATE
            // =================================================

            var today =
                DateOnly.FromDateTime(
                    DateTime.Now
                );


            // =================================================
            // BEGIN TRANSACTION
            // =================================================

            await _unitOfWork
                .BeginTransactionAsync();

            transactionStarted = true;


            // =================================================
            // DISPENSE EACH PRESCRIPTION ITEM
            // =================================================

            foreach (var prescriptionItem
                in prescriptionItems)
            {
                var allocations =
                    requestedGroups[
                        prescriptionItem.Id
                    ];


                // =================================================
                // GET VALID BATCHES
                //
                // FEFO:
                // expiry_date tăng dần
                // =================================================

                var batches =
                    await _unitOfWork
                        .MedicineBatchRepository
                        .WhereSql(
                            b =>
                                b.MedicineId ==
                                    prescriptionItem.MedicineId
                                &&
                                b.Quantity > 0
                                &&
                                b.ExpiryDate >= today
                        )
                        .OrderBy(
                            b => b.ExpiryDate
                        )
                        .ThenBy(
                            b => b.Id
                        )
                        .ToListAsync();


                // =================================================
                // CHECK STOCK
                // =================================================

                var availableQuantity =
                    batches.Sum(
                        b => b.Quantity
                    );


                if (
                    availableQuantity <
                    prescriptionItem.Quantity
                )
                {
                    await _unitOfWork
                        .RollbackTransactionAsync();

                    transactionStarted = false;

                    return Response(
                        409,
                        $"{PharmacyResponseMessageDTO.InsufficientStock} " +
                        $"{prescriptionItem.MedicineNameSnapshot}."
                    );
                }


                // =================================================
                // PREFERRED BATCH
                //
                // batchId từ request phải là lô FEFO
                // đầu tiên được đề xuất.
                // =================================================

                var preferredBatchId =
                    allocations
                        .First()
                        .BatchId;


                if (
                    batches.Count == 0 ||
                    batches[0].Id != preferredBatchId
                )
                {
                    await _unitOfWork
                        .RollbackTransactionAsync();

                    transactionStarted = false;

                    return Response(
                        409,
                        $"{PharmacyResponseMessageDTO.InvalidFefoBatch} " +
                        $"{prescriptionItem.MedicineNameSnapshot}."
                    );
                }


                // =================================================
                // DISPENSE
                //
                // Lô trước không đủ:
                // → tự chuyển lô tiếp theo.
                // =================================================

                var remaining =
                    prescriptionItem.Quantity;


                foreach (var batch in batches)
                {
                    if (remaining <= 0)
                    {
                        break;
                    }


                    var dispenseQuantity =
                        Math.Min(
                            batch.Quantity,
                            remaining
                        );


                    if (dispenseQuantity <= 0)
                    {
                        continue;
                    }


                    // =============================================
                    // BATCH BEFORE
                    // =============================================

                    var quantityBefore =
                        batch.Quantity;


                    // =============================================
                    // UPDATE BATCH
                    // =============================================

                    batch.Quantity =
                        quantityBefore -
                        dispenseQuantity;


                    // =============================================
                    // GET MEDICINE
                    // =============================================

                    var medicine =
                        await _unitOfWork
                            .MedicineRepository
                            .WhereSql(
                                m =>
                                    m.Id ==
                                    prescriptionItem.MedicineId
                            )
                            .FirstOrDefaultAsync();


                    if (medicine == null)
                    {
                        await _unitOfWork
                            .RollbackTransactionAsync();

                        transactionStarted = false;

                        return Response(
                            404,
                            PharmacyResponseMessageDTO
                                .MedicineNotFound
                        );
                    }


                    // =============================================
                    // UPDATE TOTAL STOCK
                    // =============================================

                    if (
                        medicine.StockQuantity <
                        dispenseQuantity
                    )
                    {
                        await _unitOfWork
                            .RollbackTransactionAsync();

                        transactionStarted = false;

                        return Response(
                            409,
                            $"{PharmacyResponseMessageDTO.InvalidMedicineStock} " +
                            $"{medicine.Name}."
                        );
                    }


                    medicine.StockQuantity -=
                        dispenseQuantity;


                    medicine.UpdatedAt =
                        DateTime.Now;


                    // =============================================
                    // STOCK TRANSACTION
                    //
                    // Type = 1 = OUT
                    // =============================================

                    var stockTransaction =
                        new MedicineStockTransaction
                        {
                            MedicineId =
                                medicine.Id,

                            BatchId =
                                batch.Id,

                            Type = 1,

                            Quantity =
                                dispenseQuantity,

                            QuantityBefore =
                                quantityBefore,

                            QuantityAfter =
                                batch.Quantity,

                            PrescriptionId =
                                prescription.Id,

                            CreatedBy =
                                currentUserId,

                            CreatedAt =
                                DateTime.Now,

                            Note =
                                $"Phát thuốc theo đơn #{prescription.Id}."
                        };


                    await _unitOfWork
                        .MedicineStockTransactionRepository
                        .AddAsync(
                            stockTransaction
                        );


                    remaining -=
                        dispenseQuantity;
                }


                if (remaining > 0)
                {
                    await _unitOfWork
                        .RollbackTransactionAsync();

                    transactionStarted = false;

                    return Response(
                        409,
                        $"{PharmacyResponseMessageDTO.DispenseQuantityInsufficient} " +
                        $"{prescriptionItem.MedicineNameSnapshot}."
                    );
                }
            }


            // =================================================
            // UPDATE PRESCRIPTION
            // =================================================

            var dispensedAt =
                DateTime.Now;


            prescription.Status =
                (byte)PrescriptionStatus.Dispensed;

            prescription.DispensedAt =
                dispensedAt;

            prescription.DispensedBy =
                currentUserId;


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


            return new HttpResponseData<
                PrescriptionResponseDTO>
            {
                StatusCode = 200,

                Message =
                    PharmacyResponseMessageDTO
                        .DispenseSuccess,

                Content =
                    result
            };
        }
        catch (Exception ex)
        {
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
                        "Rollback dispense failed. " +
                        "PrescriptionId: {PrescriptionId}",
                        prescriptionId
                    );
                }
            }


            _logger.LogError(
                ex,
                "Failed to dispense prescription. " +
                "PrescriptionId: {PrescriptionId}, " +
                "UserId: {UserId}",
                prescriptionId,
                currentUserId
            );


            return Response(
                500,
                PharmacyResponseMessageDTO
                    .DispenseFailed
            );
        }
    }


    // =====================================================
    // REPORT SHORTAGE
    //
    // POST:
    // /api/prescriptions/{id}/report-shortage
    //
    // Dược sĩ báo thiếu thuốc
    // → tạo notification cho bác sĩ.
    //
    // Không:
    // - đổi Prescription.Status
    // - trừ kho
    // - tạo StockTransaction
    // =====================================================

    public async Task<HttpResponseData<bool>>
        ReportShortageAsync(
            int prescriptionId,
            int currentUserId,
            ReportShortageRequestDTO request)
    {
        try
        {
            // =================================================
            // VALIDATE PRESCRIPTION ID
            // =================================================

            if (prescriptionId <= 0)
            {
                return ShortageResponse(
                    400,
                    PharmacyResponseMessageDTO
                        .PrescriptionIdInvalid
                );
            }


            // =================================================
            // VALIDATE USER
            // =================================================

            if (currentUserId <= 0)
            {
                return ShortageResponse(
                    401,
                    PharmacyResponseMessageDTO
                        .UserNotFound
                );
            }


            // =================================================
            // VALIDATE REQUEST
            // =================================================

            if (
                request == null ||
                request.MedicineId <= 0
            )
            {
                return ShortageResponse(
                    400,
                    PharmacyResponseMessageDTO
                        .ShortageRequestInvalid
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
                            p.Id ==
                            prescriptionId
                    )
                    .FirstOrDefaultAsync();


            if (prescription == null)
            {
                return ShortageResponse(
                    404,
                    PharmacyResponseMessageDTO
                        .PrescriptionNotFound
                );
            }


            // =================================================
            // CHECK PRESCRIPTION STATUS
            //
            // Chỉ đơn đã Finalized mới được báo thiếu.
            // =================================================

            if (
                prescription.Status !=
                (byte)PrescriptionStatus.Finalized
            )
            {
                return ShortageResponse(
                    409,
                    PharmacyResponseMessageDTO
                        .ShortagePrescriptionNotFinalized
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
                            m.Id ==
                            prescription.MedicalRecordId
                    )
                    .FirstOrDefaultAsync();


            if (medicalRecord == null)
            {
                return ShortageResponse(
                    404,
                    PharmacyResponseMessageDTO
                        .MedicalRecordNotFound
                );
            }


            // =================================================
            // GET DOCTOR
            //
            // MedicalRecord
            //      ↓
            // Doctor
            //      ↓
            // UserId
            // =================================================

            var doctor =
                await _unitOfWork
                    .DoctorRepository
                    .WhereSql(
                        d =>
                            d.Id ==
                            medicalRecord.DoctorId
                            &&
                            d.IsActive
                    )
                    .FirstOrDefaultAsync();


            if (doctor == null)
            {
                return ShortageResponse(
                    404,
                    PharmacyResponseMessageDTO
                        .ShortageDoctorNotFound
                );
            }


            // =================================================
            // CHECK MEDICINE
            // =================================================

            var medicine =
                await _unitOfWork
                    .MedicineRepository
                    .WhereSql(
                        m =>
                            m.Id ==
                            request.MedicineId
                    )
                    .FirstOrDefaultAsync();


            if (medicine == null)
            {
                return ShortageResponse(
                    404,
                    PharmacyResponseMessageDTO
                        .MedicineNotFound
                );
            }


            // =================================================
            // CHECK MEDICINE BELONGS TO PRESCRIPTION
            //
            // Không cho phép báo thiếu một thuốc
            // không nằm trong đơn.
            // =================================================

            var prescriptionItem =
                await _unitOfWork
                    .PrescriptionItemRepository
                    .WhereSql(
                        i =>
                            i.PrescriptionId ==
                                prescription.Id
                            &&
                            i.MedicineId ==
                                request.MedicineId
                    )
                    .FirstOrDefaultAsync();


            if (prescriptionItem == null)
            {
                return ShortageResponse(
                    400,
                    PharmacyResponseMessageDTO
                        .MedicineNotInPrescription
                );
            }


            // =================================================
            // CREATE NOTIFICATION
            //
            // Gửi InApp notification cho Doctor.
            //
            // appointment_id bắt buộc theo schema.
            // =================================================

            var notification =
                new Notification
                {
                    AppointmentId =
                        medicalRecord.AppointmentId,

                    UserId =
                        doctor.UserId,

                    Channel =
                        "InApp",

                    Recipient =
                        doctor.Email,

                    Title =
                        "Báo thiếu thuốc",

                    Content =
                        $"Thuốc \"{medicine.Name}\" " +
                        $"trong đơn thuốc #{prescription.Id} " +
                        $"đang thiếu. " +
                        (
                            string.IsNullOrWhiteSpace(
                                request.Note
                            )
                                ? "Vui lòng kiểm tra và đổi thuốc thay thế."
                                : request.Note
                        ),

                    Status = 0,

                    CreatedAt =
                        DateTime.Now
                };


            await _unitOfWork
                .NotificationRepository
                .AddAsync(
                    notification
                );


            // =================================================
            // SAVE
            // =================================================

            await _unitOfWork
                .SaveChangesAsync();


            // =================================================
            // SUCCESS
            // =================================================

            return new HttpResponseData<bool>
            {
                StatusCode = 200,

                Message =
                    PharmacyResponseMessageDTO
                        .ShortageSuccess,

                Content = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to report medicine shortage. " +
                "PrescriptionId: {PrescriptionId}, " +
                "MedicineId: {MedicineId}, " +
                "UserId: {UserId}",
                prescriptionId,
                request?.MedicineId,
                currentUserId
            );


            return ShortageResponse(
                500,
                PharmacyResponseMessageDTO
                    .ShortageFailed
            );
        }
    }


    // =====================================================
    // DISPENSE RESPONSE HELPER
    // =====================================================

    private static
        HttpResponseData<PrescriptionResponseDTO>
        Response(
            int statusCode,
            string message,
            PrescriptionResponseDTO? content = null)
    {
        return new HttpResponseData<
            PrescriptionResponseDTO>
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
    // SHORTAGE RESPONSE HELPER
    // =====================================================

    private static
        HttpResponseData<bool>
        ShortageResponse(
            int statusCode,
            string message,
            bool content = false)
    {
        return new HttpResponseData<bool>
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