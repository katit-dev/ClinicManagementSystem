using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Pharmacy;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.UnitOfWork;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Application.Services;


// =====================================================
// MEDICINE INVENTORY SERVICE CONTRACT
// =====================================================

public interface IMedicineInventoryService
{
    Task<
        HttpResponseData<List<MedicineInventoryDTO>>>
        GetInventoryAsync(
            bool lowStock,
            int nearExpiryDays);

    Task<
        HttpResponseData<StockTransactionDTO>>
        AdjustStockAsync(
            int medicineId,
            int currentUserId,
            AdjustMedicineStockRequestDTO request);

    Task<
        HttpResponseData<List<StockTransactionDTO>>>
        GetTransactionsAsync(
            int medicineId,
            DateTime? from,
            DateTime? to);
}


// =====================================================
// MEDICINE INVENTORY SERVICE
// =====================================================

public class MedicineInventoryService
    : IMedicineInventoryService
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly ILogger<MedicineInventoryService>
        _logger;


    public MedicineInventoryService(
        IUnitOfWork unitOfWork,
        ILogger<MedicineInventoryService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }


    // =====================================================
    // GET INVENTORY
    // =====================================================

    public async Task<
        HttpResponseData<List<MedicineInventoryDTO>>>
        GetInventoryAsync(
            bool lowStock,
            int nearExpiryDays)
    {
        try
        {
            if (nearExpiryDays <= 0)
            {
                nearExpiryDays = 30;
            }


            var today =
                DateOnly.FromDateTime(
                    DateTime.Now
                );

            var nearExpiryLimit =
                today.AddDays(
                    nearExpiryDays
                );


            // =================================================
            // GET ACTIVE MEDICINES
            // =================================================

            var medicines =
                await _unitOfWork
                    .MedicineRepository
                    .WhereSql(
                        m => m.IsActive
                    )
                    .OrderBy(
                        m => m.Name
                    )
                    .ToListAsync();


            var medicineIds =
                medicines
                    .Select(m => m.Id)
                    .ToHashSet();


            // =================================================
            // GET BATCHES
            // =================================================

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
                    )
                    .ToListAsync();


            var batchesByMedicine =
                batches
                    .GroupBy(
                        b => b.MedicineId
                    )
                    .ToDictionary(
                        g => g.Key,
                        g => g.ToList()
                    );


            var result =
                new List<MedicineInventoryDTO>();


            // =================================================
            // BUILD INVENTORY
            // =================================================

            foreach (var medicine in medicines)
            {
                batchesByMedicine.TryGetValue(
                    medicine.Id,
                    out var medicineBatches
                );

                medicineBatches ??=
                    new List<MedicineBatch>();


                // =============================================
                // ONLY NON-EXPIRED BATCHES
                // =============================================

                var validBatches =
                    medicineBatches
                        .Where(
                            b =>
                                b.ExpiryDate >= today
                        )
                        .ToList();


                var totalStock =
                    validBatches.Sum(
                        b => b.Quantity
                    );


                var isLowStock =
                    totalStock <
                    medicine.MinStock;


                // =============================================
                // NEAR EXPIRY BATCHES
                // =============================================

                var nearExpiryBatches =
                    validBatches
                        .Where(
                            b =>
                                b.ExpiryDate <=
                                nearExpiryLimit
                        )
                        .OrderBy(
                            b => b.ExpiryDate
                        )
                        .ThenBy(
                            b => b.Id
                        )
                        .ToList();


                // =============================================
                // NEAREST VALID BATCH
                // =============================================

                var nearestBatch =
                    validBatches
                        .OrderBy(
                            b => b.ExpiryDate
                        )
                        .ThenBy(
                            b => b.Id
                        )
                        .FirstOrDefault();


                // =============================================
                // LOW STOCK FILTER
                // =============================================

                if (
                    lowStock &&
                    !isLowStock
                )
                {
                    continue;
                }

                result.Add(
        new MedicineInventoryDTO
        {
            Id = medicine.Id,

            Code = medicine.Code,

            Name = medicine.Name,

            ActiveIngredient =
                medicine.ActiveIngredient,

            StockQuantity =
                totalStock,

            MinStock =
                medicine.MinStock,

            IsLowStock =
                isLowStock,

            NearExpiryBatchNo =
                nearestBatch?.BatchNo,

            NearExpiryDate =
                nearestBatch?.ExpiryDate,

            NearExpiryBatchQuantity =
                nearestBatch?.Quantity,

            IsNearExpiry =
                nearExpiryBatches.Count > 0,

            NearExpiryBatchCount =
                nearExpiryBatches.Count,

            Batches =
                validBatches
                    .OrderBy(b => b.ExpiryDate)
                    .ThenBy(b => b.Id)
                    .Select(
                        b => new MedicineInventoryBatchDTO
                        {
                            BatchId = b.Id,
                            BatchNo = b.BatchNo,
                            ExpiryDate = b.ExpiryDate,
                            Quantity = b.Quantity
                        }
                    )
                    .ToList()
        }
    );

            }


            return new HttpResponseData<
                List<MedicineInventoryDTO>>
            {
                StatusCode = 200,

                Message =
                    PharmacyResponseMessageDTO
                        .InventoryGetSuccess,

                Content =
                    result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get medicine inventory."
            );

            return new HttpResponseData<
                List<MedicineInventoryDTO>>
            {
                StatusCode = 500,

                Message =
                    PharmacyResponseMessageDTO
                        .InventoryGetFailed
            };
        }
    }


    // =====================================================
    // ADJUST STOCK
    //
    // POST:
    // /api/medicines/{id}/adjust
    // =====================================================

    public async Task<
        HttpResponseData<StockTransactionDTO>>
        AdjustStockAsync(
            int medicineId,
            int currentUserId,
            AdjustMedicineStockRequestDTO request)
    {
        bool transactionStarted = false;


        try
        {
            // =================================================
            // VALIDATE
            // =================================================

            if (medicineId <= 0 ||
                currentUserId <= 0)
            {
                return Response(
                    400,
                    "Thông tin điều chỉnh không hợp lệ."
                );
            }


            if (request == null ||
                request.BatchId <= 0 ||
                request.Quantity == 0 ||
                string.IsNullOrWhiteSpace(
                    request.Reason))
            {
                return Response(
                    400,
                    "Batch, số lượng và lý do là bắt buộc."
                );
            }


            var reason =
                request.Reason.Trim();


            if (reason.Length > 500)
            {
                return Response(
                    400,
                    "Lý do không được vượt quá 500 ký tự."
                );
            }


            // =================================================
            // GET MEDICINE
            // =================================================

            var medicine =
                await _unitOfWork
                    .MedicineRepository
                    .WhereSql(
                        m =>
                            m.Id == medicineId &&
                            m.IsActive
                    )
                    .FirstOrDefaultAsync();


            if (medicine == null)
            {
                return Response(
                    404,
                    "Không tìm thấy thuốc."
                );
            }


            // =================================================
            // GET BATCH
            // =================================================

            var batch =
                await _unitOfWork
                    .MedicineBatchRepository
                    .WhereSql(
                        b =>
                            b.Id == request.BatchId &&
                            b.MedicineId == medicineId
                    )
                    .FirstOrDefaultAsync();


            if (batch == null)
            {
                return Response(
                    404,
                    "Không tìm thấy lô thuốc."
                );
            }


            // =================================================
            // BEGIN TRANSACTION
            // =================================================

            await _unitOfWork
                .BeginTransactionAsync();

            transactionStarted = true;


            var now =
                DateTime.Now;


            var quantityBefore =
                batch.Quantity;


            var quantityAfter =
                quantityBefore +
                request.Quantity;


            // =================================================
            // CANNOT GO BELOW ZERO
            // =================================================

            if (quantityAfter < 0)
            {
                await _unitOfWork
                    .RollbackTransactionAsync();

                transactionStarted = false;

                return Response(
                    409,
                    "Tồn lô không thể nhỏ hơn 0."
                );
            }


            // =================================================
            // UPDATE BATCH
            // =================================================

            batch.Quantity =
                quantityAfter;


            // =================================================
            // RECALCULATE TOTAL STOCK
            //
            // Chỉ tính các lô chưa hết hạn.
            // =================================================

            var allBatches =
                await _unitOfWork
                    .MedicineBatchRepository
                    .WhereSql(
                        b =>
                            b.MedicineId ==
                                medicine.Id
                    )
                    .ToListAsync();


            var today =
                DateOnly.FromDateTime(
                    now
                );


            var totalStock =
                allBatches
                    .Where(
                        b =>
                            b.ExpiryDate >= today
                    )
                    .Sum(
                        b => b.Quantity
                    );


            medicine.StockQuantity =
                totalStock;

            medicine.UpdatedAt =
                now;


            // =================================================
            // STOCK TRANSACTION
            //
            // type = 2 = ADJUST
            //
            // quantity luôn dương trong DB.
            // =================================================

            var stockTransaction =
                new MedicineStockTransaction
                {
                    MedicineId =
                        medicine.Id,

                    BatchId =
                        batch.Id,

                    Type = 2,

                    Quantity =
                        Math.Abs(
                            request.Quantity
                        ),

                    QuantityBefore =
                        quantityBefore,

                    QuantityAfter =
                        quantityAfter,

                    PrescriptionId =
                        null,

                    ReversalOfTransactionId =
                        null,

                    CreatedBy =
                        currentUserId,

                    CreatedAt =
                        now,

                    Note =
                        reason
                };


            await _unitOfWork
                .MedicineStockTransactionRepository
                .AddAsync(
                    stockTransaction
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
                        "ADJUST_MEDICINE_STOCK",

                    EntityName =
                        "MedicineBatch",

                    EntityId =
                        batch.Id,

                    Details =
                        $"Điều chỉnh tồn thuốc " +
                        $"#{medicine.Id} - " +
                        $"lô {batch.BatchNo}: " +
                        $"{quantityBefore} → " +
                        $"{quantityAfter}. " +
                        $"Lý do: {reason}",

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
                new StockTransactionDTO
                {
                    Id =
                        stockTransaction.Id,

                    MedicineId =
                        stockTransaction.MedicineId,

                    BatchId =
                        stockTransaction.BatchId,

                    BatchNo =
                        batch.BatchNo,

                    Type =
                        stockTransaction.Type,

                    Quantity =
                        stockTransaction.Quantity,

                    QuantityBefore =
                        stockTransaction.QuantityBefore,

                    QuantityAfter =
                        stockTransaction.QuantityAfter,

                    PrescriptionId =
                        null,

                    CreatedBy =
                        stockTransaction.CreatedBy,

                    CreatedAt =
                        stockTransaction.CreatedAt,

                    Note =
                        stockTransaction.Note
                };


            return new HttpResponseData<
                StockTransactionDTO>
            {
                StatusCode = 200,

                Message =
                    PharmacyResponseMessageDTO
                        .StockAdjustSuccess,

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
                        "Failed to rollback stock adjustment."
                    );
                }
            }


            _logger.LogError(
                ex,
                "Failed to adjust medicine stock. " +
                "MedicineId: {MedicineId}, " +
                "UserId: {UserId}",
                medicineId,
                currentUserId
            );


            return Response(
                500,
                PharmacyResponseMessageDTO
                    .StockAdjustFailed
            );
        }
    }


    // =====================================================
    // GET STOCK TRANSACTIONS
    //
    // GET:
    // /api/medicines/{id}/transactions
    // =====================================================

    public async Task<
        HttpResponseData<List<StockTransactionDTO>>>
        GetTransactionsAsync(
            int medicineId,
            DateTime? from,
            DateTime? to)
    {
        try
        {
            if (medicineId <= 0)
            {
                return new HttpResponseData<
                    List<StockTransactionDTO>>
                {
                    StatusCode = 400,

                    Message =
                        "Mã thuốc không hợp lệ."
                };
            }


            var medicineExists =
                await _unitOfWork
                    .MedicineRepository
                    .WhereSql(
                        m => m.Id == medicineId
                    )
                    .AnyAsync();


            if (!medicineExists)
            {
                return new HttpResponseData<
                    List<StockTransactionDTO>>
                {
                    StatusCode = 404,

                    Message =
                        "Không tìm thấy thuốc."
                };
            }


            var query =
                _unitOfWork
                    .MedicineStockTransactionRepository
                    .WhereSql(
                        t =>
                            t.MedicineId ==
                            medicineId
                    );


            if (from.HasValue)
            {
                query =
                    query.Where(
                        t =>
                            t.CreatedAt >=
                            from.Value
                    );
            }


            if (to.HasValue)
            {
                query =
                    query.Where(
                        t =>
                            t.CreatedAt <=
                            to.Value
                    );
            }


            var transactions =
                await query
                    .Include(
                        t => t.MedicineBatch
                    )
                    .OrderByDescending(
                        t => t.CreatedAt
                    )
                    .ThenByDescending(
                        t => t.Id
                    )
                    .ToListAsync();


            var result =
                transactions
                    .Select(
                        t =>
                            new StockTransactionDTO
                            {
                                Id =
                                    t.Id,

                                MedicineId =
                                    t.MedicineId,

                                BatchId =
                                    t.BatchId,

                                BatchNo =
                                    t.MedicineBatch
                                        ?.BatchNo
                                    ?? string.Empty,

                                Type =
                                    t.Type,

                                Quantity =
                                    t.Quantity,

                                QuantityBefore =
                                    t.QuantityBefore,

                                QuantityAfter =
                                    t.QuantityAfter,

                                PrescriptionId =
                                    t.PrescriptionId,

                                CreatedBy =
                                    t.CreatedBy,

                                CreatedAt =
                                    t.CreatedAt,

                                Note =
                                    t.Note
                            }
                    )
                    .ToList();


            return new HttpResponseData<
                List<StockTransactionDTO>>
            {
                StatusCode = 200,

                Message =
                    PharmacyResponseMessageDTO
                        .StockTransactionGetSuccess,

                Content =
                    result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get stock transactions. " +
                "MedicineId: {MedicineId}",
                medicineId
            );


            return new HttpResponseData<
                List<StockTransactionDTO>>
            {
                StatusCode = 500,

                Message =
                    PharmacyResponseMessageDTO
                        .StockTransactionGetFailed
            };
        }
    }


    // =====================================================
    // RESPONSE HELPER
    // =====================================================

    private static HttpResponseData<
        StockTransactionDTO>
        Response(
            int statusCode,
            string message)
    {
        return new HttpResponseData<
            StockTransactionDTO>
        {
            StatusCode =
                statusCode,

            Message =
                message
        };
    }
}