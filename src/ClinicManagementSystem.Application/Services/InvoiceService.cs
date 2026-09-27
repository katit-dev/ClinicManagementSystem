using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Invoice;
using ClinicManagementSystem.Application.DTOs.Payment;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Application.Enums;
using ClinicManagementSystem.Infrastructure.Repositories;

namespace ClinicManagementSystem.Application.Services;

public interface IInvoiceService
{
    Task<HttpResponseData<InvoiceDTO>> GetInvoiceAsync(int invoiceId);

    Task<HttpResponseData<InvoiceDTO>> CreatePaymentAsync(int invoiceId, PaymentRequestDTO request, int currentUserId);

    Task<HttpResponseData<InvoiceDTO>> RefundPaymentAsync(int invoiceId, PaymentRequestDTO request, int currentUserId);

    Task<HttpResponseData<InvoiceDTO>> CancelInvoiceAsync(int invoiceId, CancelInvoiceRequestDTO request, int currentUserId);
}

public class InvoiceService : IInvoiceService
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly ILogger<InvoiceService> _logger;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public InvoiceService(IUnitOfWork unitOfWork, ILogger<InvoiceService> logger)
    {
        _unitOfWork = unitOfWork;

        _logger = logger;
    }


    // =====================================================
    // GET INVOICE
    // =====================================================

    public async Task<HttpResponseData<InvoiceDTO>>
        GetInvoiceAsync(
            int invoiceId)
    {
        try
        {
            // =================================================
            // VALIDATE ID
            // =================================================

            if (invoiceId <= 0)
            {
                return Response(
                    400,
                    InvoiceResponseMessageDTO.InvoiceNotFound
                );
            }


            // =================================================
            // GET INVOICE
            // =================================================

            var invoice =
                await _unitOfWork
                    .InvoiceRepository
                    .WhereSql(
                        i =>
                            i.Id == invoiceId
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // INVOICE NOT FOUND
            // =================================================

            if (invoice == null)
            {
                return Response(
                    404,
                    InvoiceResponseMessageDTO.InvoiceNotFound
                );
            }


            // =================================================
            // GET INVOICE ITEMS
            // =================================================

            var items =
                await _unitOfWork
                    .InvoiceItemRepository
                    .WhereSql(
                        i =>
                            i.InvoiceId == invoice.Id
                    )
                    .OrderBy(
                        i => i.Id
                    )
                    .Select(
                        i =>
                            new InvoiceItemDTO
                            {
                                Id =
                                    i.Id,

                                InvoiceId =
                                    i.InvoiceId,

                                MedicineId =
                                    i.MedicineId,

                                MedicalRecordServiceId =
                                    i.MedicalRecordServiceId,

                                Description =
                                    i.Description,

                                Quantity =
                                    i.Quantity,

                                UnitPrice =
                                    i.UnitPrice,

                                DiscountAmount =
                                    i.DiscountAmount,

                                Amount =
                                    i.Amount
                            }
                    )
                    .ToListAsync();


            // =================================================
            // GET PAYMENTS
            // =================================================

            var payments =
                await _unitOfWork
                    .PaymentRepository
                    .WhereSql(
                        p =>
                            p.InvoiceId == invoice.Id
                    )
                    .OrderBy(
                        p => p.PaidAt
                    )
                    .Select(
                        p =>
                            new PaymentDTO
                            {
                                Id =
                                    p.Id,

                                Amount =
                                    p.Amount,

                                Method =
                                    p.Method,

                                PaidAt =
                                    p.PaidAt,

                                Note =
                                    p.Note,

                                ReferenceCode =
                                    p.ReferenceCode,

                                ReceivedBy =
                                    p.ReceivedBy,

                                IsRefund =
                                    p.IsRefund
                            }
                    )
                    .ToListAsync();


            // =================================================
            // CALCULATE REMAINING
            // =================================================

            var payableAmount =
    invoice.TotalAmount
    + invoice.TaxAmount
    - invoice.DiscountAmount
    - invoice.InsuranceAmount;

            var remainingAmount =
                payableAmount
                - invoice.PaidAmount;


            // =================================================
            // PROTECT AGAINST NEGATIVE VALUE
            // =================================================

            if (remainingAmount < 0)
            {
                remainingAmount = 0;
            }


            // =================================================
            // MAP INVOICE DTO
            // =================================================

            var result =
                new InvoiceDTO
                {
                    Id =
                        invoice.Id,

                    InvoiceNo =
                        invoice.InvoiceNo,

                    PatientId =
                        invoice.PatientId,

                    PatientName =
                        invoice.PatientName,

                    AppointmentId =
                        invoice.AppointmentId,

                    MedicalRecordId =
                        invoice.MedicalRecordId,

                    TotalAmount =
                        invoice.TotalAmount,

                    DiscountAmount =
                        invoice.DiscountAmount,

                    TaxAmount =
                        invoice.TaxAmount,

                    InsuranceAmount =
                        invoice.InsuranceAmount,

                    PaidAmount =
                        invoice.PaidAmount,

                    RemainingAmount =
                        remainingAmount,

                    Status =
                        invoice.Status,

                    CreatedAt =
                        invoice.CreatedAt,

                    CancelledAt =
                        invoice.CancelledAt,

                    CancelReason =
                        invoice.CancelReason,

                    Items =
                        items,

                    Payments =
                        payments
                };


            // =================================================
            // SUCCESS
            // =================================================

            return Response(
                200,
                InvoiceResponseMessageDTO.GetSuccess,
                result
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get invoice. " +
                "InvoiceId: {InvoiceId}",
                invoiceId
            );

            return Response(
                500,
                InvoiceResponseMessageDTO.GetFailed
            );
        }
    }


    // =====================================================
    // RESPONSE
    // =====================================================

    private static HttpResponseData<InvoiceDTO>
        Response(
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


    // =====================================================
    // Create Payment
    // =====================================================

    public async Task<HttpResponseData<InvoiceDTO>>
    CreatePaymentAsync(
        int invoiceId,
        PaymentRequestDTO request,
        int currentUserId)
    {
        bool transactionStarted = false;

        try
        {
            // =====================================================
            // VALIDATE INVOICE ID
            // =====================================================

            if (invoiceId <= 0)
            {
                return Response(
                    400,
                    InvoiceResponseMessageDTO.InvalidInvoiceId
                );
            }


            // =====================================================
            // VALIDATE USER
            // =====================================================

            if (currentUserId <= 0)
            {
                return Response(
                    401,
                    InvoiceResponseMessageDTO.PaymentUserNotFound
                );
            }


            // =====================================================
            // VALIDATE REQUEST
            // =====================================================

            if (request == null)
            {
                return Response(
                    400,
InvoiceResponseMessageDTO.PaymentRequestRequired);
            }


            // =====================================================
            // CHECK REFUND
            //
            // Method này chỉ xử lý THU TIỀN.
            // Refund sẽ được xử lý riêng ở bước tiếp theo.
            // =====================================================

            if (request.IsRefund)
            {
                return Response(
                    400,
InvoiceResponseMessageDTO.RefundMustUseRefundFunction);
            }


            // =====================================================
            // CHECK AMOUNT
            // =====================================================

            if (request.Amount <= 0)
            {
                return Response(
                    400,
                    InvoiceResponseMessageDTO.RefundAmountInvalid
                );
            }


            // =====================================================
            // CHECK PAYMENT METHOD
            // =====================================================

            if (request.Method >
                (byte)PaymentMethod.EWallet)
            {
                return Response(
                    400,
                    InvoiceResponseMessageDTO.PaymentMethodInvalid
                );
            }


            // =====================================================
            // GET INVOICE
            // =====================================================

            var invoice =
                await _unitOfWork
                    .InvoiceRepository
                    .WhereSql(
                        i =>
                            i.Id == invoiceId
                    )
                    .FirstOrDefaultAsync();


            if (invoice == null)
            {
                return Response(
                    404,
                    InvoiceResponseMessageDTO.InvoiceNotFound
                );
            }


            // =====================================================
            // CHECK CANCELLED
            // =====================================================

            if (invoice.Status ==
                (byte)InvoiceStatus.Cancelled)
            {
                return Response(
                    400,
                    InvoiceResponseMessageDTO.PaymentInvoiceCancelled
                );
            }


            // =====================================================
            // CALCULATE REMAINING
            // =====================================================

            var payableAmount =
    invoice.TotalAmount
    + invoice.TaxAmount
    - invoice.DiscountAmount
    - invoice.InsuranceAmount;

            var remainingAmount =
                payableAmount
                - invoice.PaidAmount;


            if (remainingAmount <= 0)
            {
                return Response(
                    400,
                    InvoiceResponseMessageDTO.InvoiceAlreadyPaid
                );
            }


            // =====================================================
            // CHECK PAYMENT AMOUNT
            // =====================================================

            if (request.Amount >
                remainingAmount)
            {
                return Response(
                    400,
                    $"Số tiền thanh toán không được vượt quá " +
                    $"{remainingAmount:N0}."
                );
            }


            // =====================================================
            // BEGIN TRANSACTION
            // =====================================================

            await _unitOfWork
                .BeginTransactionAsync();

            transactionStarted = true;


            // =====================================================
            // CREATE PAYMENT
            // =====================================================

            var payment =
                new Payment
                {
                    InvoiceId =
                        invoice.Id,

                    Amount =
                        request.Amount,

                    Method =
                        request.Method,

                    PaidAt =
                        DateTime.Now,

                    Note =
                        request.Note,

                    ReferenceCode =
                        request.ReferenceCode,

                    ReceivedBy =
                        currentUserId,

                    IsRefund =
                        false
                };


            // =====================================================
            // UPDATE INVOICE PAID AMOUNT
            // =====================================================

            invoice.PaidAmount +=
                request.Amount;

            invoice.UpdatedAt =
                DateTime.Now;


            // =====================================================
            // UPDATE INVOICE STATUS
            //
            // 0 = Unpaid
            // 1 = PartiallyPaid
            // 2 = Paid
            // =====================================================

            if (invoice.PaidAmount >= payableAmount)
            {
                invoice.Status =
                    (byte)InvoiceStatus.Paid;
            }
            else
            {
                invoice.Status =
                    (byte)InvoiceStatus.PartiallyPaid;
            }


            // =====================================================
            // SAVE PAYMENT
            // =====================================================

            await _unitOfWork
                .PaymentRepository
                .AddAsync(
                    payment
                );


            // =====================================================
            // AUDIT LOG
            // =====================================================

            var auditLog =
                new AuditLog
                {
                    UserId =
                        currentUserId,

                    Action =
                        "CREATE_PAYMENT",

                    EntityName =
                        "Invoice",

                    EntityId =
                        invoice.Id,

                    Details =
                        $"Thu tiền hóa đơn " +
                        $"{invoice.InvoiceNo}. " +
                        $"Số tiền: {request.Amount:N0}. " +
                        $"Phương thức: {request.Method}.",

                    IpAddress =
                        null,

                    Succeeded =
                        true,

                    OccurredAt =
                        DateTime.Now
                };


            await _unitOfWork
                .AuditLogRepository
                .AddAsync(
                    auditLog
                );


            // =====================================================
            // SAVE ALL
            // =====================================================

            await _unitOfWork
                .SaveChangesAsync();


            // =====================================================
            // COMMIT
            // =====================================================

            await _unitOfWork
                .CommitTransactionAsync();

            transactionStarted = false;


            // =====================================================
            // GET UPDATED INVOICE
            // =====================================================

            var result =
                await GetInvoiceAsync(
                    invoiceId
                );


            if (result.StatusCode != 200)
            {
                return result;
            }


            result.Message =
                InvoiceResponseMessageDTO.PaymentSuccess;

            return result;
        }
        catch (Exception ex)
        {
            // =====================================================
            // ROLLBACK
            // =====================================================

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
                        "Failed to rollback payment transaction. " +
                        "InvoiceId: {InvoiceId}",
                        invoiceId
                    );
                }
            }


            // =====================================================
            // LOG ERROR
            // =====================================================

            _logger.LogError(
                ex,
                "Failed to create payment. " +
                "InvoiceId: {InvoiceId}, " +
                "UserId: {UserId}",
                invoiceId,
                currentUserId
            );


            return Response(
                500,
                InvoiceResponseMessageDTO.PaymentFailed
            );
        }
    }


    public async Task<HttpResponseData<InvoiceDTO>>
    RefundPaymentAsync(
        int invoiceId,
        PaymentRequestDTO request,
        int currentUserId)
    {
        bool transactionStarted = false;

        try
        {
            // =====================================================
            // VALIDATE INVOICE ID
            // =====================================================

            if (invoiceId <= 0)
            {
                return Response(
                    400,
                    InvoiceResponseMessageDTO.InvoiceNotFound
                );
            }


            // =====================================================
            // VALIDATE USER
            // =====================================================

            if (currentUserId <= 0)
            {
                return Response(
                    401,
                    InvoiceResponseMessageDTO.PaymentUserNotFound
                );
            }


            // =====================================================
            // VALIDATE REQUEST
            // =====================================================

            if (request == null)
            {
                return Response(
                    400,
                    InvoiceResponseMessageDTO.PaymentRequestRequired
                );
            }


            // =====================================================
            // MUST BE REFUND
            // =====================================================

            if (!request.IsRefund)
            {
                return Response(
                    400,
                    InvoiceResponseMessageDTO.RefundFlagInvalid
                );
            }


            // =====================================================
            // AMOUNT
            //
            // Frontend gửi số dương:
            //
            // 100000
            //
            // Backend sẽ tạo:
            //
            // -100000
            // =====================================================

            if (request.Amount <= 0)
            {
                return Response(
                    400,
                    InvoiceResponseMessageDTO.PaymentAmountInvalid
                );
            }


            // =====================================================
            // PAYMENT METHOD
            // =====================================================

            if (request.Method >
                (byte)PaymentMethod.EWallet)
            {
                return Response(
                    400,
                    InvoiceResponseMessageDTO.PaymentMethodInvalid
                );
            }


            // =====================================================
            // GET INVOICE
            // =====================================================

            var invoice =
                await _unitOfWork
                    .InvoiceRepository
                    .WhereSql(
                        i =>
                            i.Id == invoiceId
                    )
                    .FirstOrDefaultAsync();


            if (invoice == null)
            {
                return Response(
                    404,
                    InvoiceResponseMessageDTO.InvoiceNotFound
                );
            }


            // =====================================================
            // CHECK CANCELLED
            // =====================================================

            if (invoice.Status ==
                (byte)InvoiceStatus.Cancelled)
            {
                return Response(
                    400,
                    InvoiceResponseMessageDTO.PaymentInvoiceCancelled
                );
            }


            // =====================================================
            // CHECK PAID AMOUNT
            // =====================================================

            if (invoice.PaidAmount <= 0)
            {
                return Response(
                    400,
                    InvoiceResponseMessageDTO.RefundNotAvailable
                );
            }


            // =====================================================
            // CHECK REFUND AMOUNT
            //
            // Không được hoàn vượt quá số tiền
            // hiện đang được ghi nhận là đã thanh toán.
            // =====================================================

            if (request.Amount >
                invoice.PaidAmount)
            {
                return Response(
                    400,
                    $"Số tiền hoàn không được vượt quá " +
                    $"{invoice.PaidAmount:N0}."
                );
            }


            // =====================================================
            // BEGIN TRANSACTION
            // =====================================================

            await _unitOfWork
                .BeginTransactionAsync();

            transactionStarted = true;


            // =====================================================
            // CREATE REFUND PAYMENT
            //
            // QUAN TRỌNG:
            //
            // Không sửa Payment cũ.
            // Tạo một Payment mới với Amount âm.
            // =====================================================

            var refundPayment =
                new Payment
                {
                    InvoiceId =
                        invoice.Id,

                    Amount =
                        -request.Amount,

                    Method =
                        request.Method,

                    PaidAt =
                        DateTime.Now,

                    Note =
                        request.Note,

                    ReferenceCode =
                        request.ReferenceCode,

                    ReceivedBy =
                        currentUserId,

                    IsRefund =
                        true
                };


            // =====================================================
            // UPDATE PAID AMOUNT
            // =====================================================

            invoice.PaidAmount -=
                request.Amount;

            invoice.UpdatedAt =
                DateTime.Now;


            // =====================================================
            // UPDATE STATUS
            //
            // 0 = Unpaid
            // 1 = PartiallyPaid
            // 2 = Paid
            // =====================================================

            var payableAmount =
    invoice.TotalAmount
    + invoice.TaxAmount
    - invoice.DiscountAmount
    - invoice.InsuranceAmount;

            if (invoice.PaidAmount <= 0)
            {
                invoice.PaidAmount = 0;

                invoice.Status =
                    (byte)InvoiceStatus.Unpaid;
            }
            else if (
                invoice.PaidAmount <
                payableAmount)
            {
                invoice.Status =
                    (byte)InvoiceStatus.PartiallyPaid;
            }
            else
            {
                invoice.Status =
                    (byte)InvoiceStatus.Paid;
            }


            // =====================================================
            // SAVE REFUND PAYMENT
            // =====================================================

            await _unitOfWork
                .PaymentRepository
                .AddAsync(
                    refundPayment
                );


            // =====================================================
            // AUDIT LOG
            // =====================================================

            var auditLog =
                new AuditLog
                {
                    UserId =
                        currentUserId,

                    Action =
                        "REFUND_INVOICE",

                    EntityName =
                        "Invoice",

                    EntityId =
                        invoice.Id,

                    Details =
                        $"Hoàn tiền hóa đơn " +
                        $"{invoice.InvoiceNo}. " +
                        $"Số tiền: {-request.Amount:N0}. " +
                        $"Phương thức: {request.Method}.",

                    IpAddress =
                        null,

                    Succeeded =
                        true,

                    OccurredAt =
                        DateTime.Now
                };


            await _unitOfWork
                .AuditLogRepository
                .AddAsync(
                    auditLog
                );


            // =====================================================
            // SAVE
            // =====================================================

            await _unitOfWork
                .SaveChangesAsync();


            // =====================================================
            // COMMIT
            // =====================================================

            await _unitOfWork
                .CommitTransactionAsync();

            transactionStarted = false;


            // =====================================================
            // GET UPDATED INVOICE
            // =====================================================

            var result =
                await GetInvoiceAsync(
                    invoiceId
                );


            if (result.StatusCode != 200)
            {
                return result;
            }


            result.Message = InvoiceResponseMessageDTO.RefundSuccess;


            return result;
        }
        catch (Exception ex)
        {
            // =====================================================
            // ROLLBACK
            // =====================================================

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
                        "Failed to rollback refund transaction. " +
                        "InvoiceId: {InvoiceId}",
                        invoiceId
                    );
                }
            }


            // =====================================================
            // LOG ERROR
            // =====================================================

            _logger.LogError(
                ex,
                "Failed to refund invoice. " +
                "InvoiceId: {InvoiceId}, " +
                "UserId: {UserId}",
                invoiceId,
                currentUserId
            );


            return Response(
                500,
                InvoiceResponseMessageDTO.RefundFailed
            );
        }
    }


    public Task<HttpResponseData<InvoiceDTO>>
        CancelInvoiceAsync(
            int invoiceId,
            CancelInvoiceRequestDTO request,
            int currentUserId)
    {
        throw new NotImplementedException();
    }
}
