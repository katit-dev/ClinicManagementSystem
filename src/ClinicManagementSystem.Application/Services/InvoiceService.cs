using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Invoice;
using ClinicManagementSystem.Application.DTOs.Payment;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

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
                    "Mã hóa đơn không hợp lệ."
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
                    "Không tìm thấy hóa đơn."
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
            //
            // Requirement:
            //
            // Remaining =
            // Total
            // - Insurance
            // - Paid
            // =================================================

            var remainingAmount =
                invoice.TotalAmount
                - invoice.InsuranceAmount
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
                "Lấy hóa đơn thành công.",
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
                "Không thể lấy thông tin hóa đơn."
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
    // NOT IMPLEMENTED YET
    // =====================================================

    public Task<HttpResponseData<InvoiceDTO>>
        CreatePaymentAsync(
            int invoiceId,
            PaymentRequestDTO request,
            int currentUserId)
    {
        throw new NotImplementedException();
    }


    public Task<HttpResponseData<InvoiceDTO>>
        RefundPaymentAsync(
            int invoiceId,
            PaymentRequestDTO request,
            int currentUserId)
    {
        throw new NotImplementedException();
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
