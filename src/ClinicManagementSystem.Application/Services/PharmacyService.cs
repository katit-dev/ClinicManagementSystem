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
                    "Không xác định được đơn thuốc."
                );
            }


            // =================================================
            // VALIDATE USER
            // =================================================

            if (currentUserId <= 0)
            {
                return Response(
                    401,
                    "Không xác định được người phát thuốc."
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
                    "Danh sách thuốc phát không được rỗng."
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
                        "Thông tin phát thuốc không hợp lệ."
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
                    "Không tìm thấy đơn thuốc."
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
                    "Đơn thuốc đã được phát."
                );
            }


            if (
                prescription.Status !=
                (byte)PrescriptionStatus.Finalized
            )
            {
                return Response(
                    409,
                    "Đơn thuốc chưa được chốt."
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
                    "Đơn thuốc không có thuốc để phát."
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
                        $"Thiếu thông tin phát thuốc cho " +
                        $"thuốc #{prescriptionItem.Id}."
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
                        $"Số lượng phát của " +
                        $"{prescriptionItem.MedicineNameSnapshot} " +
                        $"không đúng số lượng kê."
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
                    "Có thuốc không thuộc đơn thuốc."
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
                        $"Thuốc " +
                        $"{prescriptionItem.MedicineNameSnapshot} " +
                        "không đủ tồn kho."
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
                        $"Lô thuốc của " +
                        $"{prescriptionItem.MedicineNameSnapshot} " +
                        "không đúng thứ tự FEFO."
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
                            "Không tìm thấy thuốc."
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
                            $"Tồn tổng của thuốc " +
                            $"{medicine.Name} không hợp lệ."
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
                        $"Không đủ thuốc " +
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
                    "Phát thuốc thành công.",

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
                "Không thể phát thuốc."
            );
        }
    }


    // =====================================================
    // RESPONSE HELPER
    // =====================================================

    private static HttpResponseData<
        PrescriptionResponseDTO>
        Response(
            int statusCode,
            string message)
    {
        return new HttpResponseData<
            PrescriptionResponseDTO>
        {
            StatusCode =
                statusCode,

            Message =
                message
        };
    }
}