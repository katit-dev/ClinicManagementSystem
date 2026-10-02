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