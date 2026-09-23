using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Invoice;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Services;


// =====================================================
// PATIENT INVOICE SERVICE CONTRACT
// =====================================================

public interface IPatientInvoiceService
{
    Task<HttpResponseData<List<PatientInvoiceDTO>>> GetPatientInvoicesAsync(int currentUserId);
}

// =====================================================
// PATIENT INVOICE SERVICE
// =====================================================

public class PatientInvoiceService : IPatientInvoiceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PatientInvoiceService> _logger;


    // =================================================
    // CONSTRUCTOR
    // =================================================

    public PatientInvoiceService(IUnitOfWork unitOfWork, ILogger<PatientInvoiceService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }


    // =================================================
    // GET PATIENT INVOICES
    // =================================================

    public async Task<HttpResponseData<List<PatientInvoiceDTO>>> GetPatientInvoicesAsync(
        int currentUserId)
    {
        try
        {
            // =============================================
            // RESOLVE CURRENT PATIENT
            // =============================================

            var patient = await _unitOfWork.PatientRepository
                .WhereSql(p =>
                    p.UserId == currentUserId &&
                    p.IsActive)
                .FirstOrDefaultAsync();

            if (patient == null)
            {
                return Response(
                    404,
                    "Không tìm thấy thông tin bệnh nhân."
                );
            }


            // =============================================
            // GET PATIENT INVOICES
            // =============================================

            var invoices = await _unitOfWork.InvoiceRepository
                .WhereSql(i => i.PatientId == patient.Id)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();


            // =============================================
            // NO INVOICES
            // =============================================

            if (invoices.Count == 0)
            {
                return Response(
                    200,
                    "Bệnh nhân chưa có hóa đơn."
                );
            }


            // =============================================
            // MAP INVOICES
            // =============================================

            var invoiceDtos = new List<PatientInvoiceDTO>();

            foreach (var invoice in invoices)
            {
                // GET INVOICE ITEMS
                var invoiceItems = await _unitOfWork.InvoiceItemRepository
                    .WhereSql(item => item.InvoiceId == invoice.Id)
                    .ToListAsync();

                // MAP INVOICE ITEMS
                var itemDtos = invoiceItems
                    .Select(item => new PatientInvoiceItemDTO
                    {
                        Id = item.Id,
                        Description = item.Description,
                        MedicineId = item.MedicineId,
                        MedicalRecordServiceId = item.MedicalRecordServiceId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        DiscountAmount = item.DiscountAmount,
                        Amount = item.Amount
                    })
                    .ToList();

                // =========================================
                // GET PAYMENTS
                // =========================================

                var payments = await _unitOfWork.PaymentRepository
                    .WhereSql(payment => payment.InvoiceId == invoice.Id)
                    .OrderByDescending(payment => payment.PaidAt)
                    .ToListAsync();


                // =========================================
                // MAP PAYMENTS
                // =========================================

                var paymentDtos = payments
                    .Select(payment => new PatientPaymentDTO
                    {
                        Id = payment.Id,
                        Amount = payment.Amount,
                        Method = payment.Method,
                        PaidAt = payment.PaidAt,
                        ReferenceCode = payment.ReferenceCode,
                        Note = payment.Note,
                        IsRefund = payment.IsRefund
                    })
                    .ToList();

                // =========================================
                // MAP INVOICE
                // =========================================

                invoiceDtos.Add(new PatientInvoiceDTO
                {
                    Id = invoice.Id,
                    InvoiceNo = invoice.InvoiceNo,
                    AppointmentId = invoice.AppointmentId,
                    MedicalRecordId = invoice.MedicalRecordId,
                    PatientName = invoice.PatientName,

                    TotalAmount = invoice.TotalAmount,
                    DiscountAmount = invoice.DiscountAmount,
                    TaxAmount = invoice.TaxAmount,
                    InsuranceAmount = invoice.InsuranceAmount,
                    PaidAmount = invoice.PaidAmount,

                    Status = invoice.Status,

                    CreatedAt = invoice.CreatedAt,
                    UpdatedAt = invoice.UpdatedAt,

                    CancelledAt = invoice.CancelledAt,
                    CancelReason = invoice.CancelReason,

                    Items = itemDtos,
                    Payments = paymentDtos
                });
            }
            // =============================================
            // RESPONSE
            // =============================================

            return Response(
    200,
    "Lấy danh sách hóa đơn thành công.",
    invoiceDtos
);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting invoices for user {UserId}",
                currentUserId
            );

            return Response(
                500,
                "Đã xảy ra lỗi khi lấy hóa đơn."
            );
        }
    }

    // =====================================================
    // RESPONSE HELPER
    // =====================================================

    private static HttpResponseData<List<PatientInvoiceDTO>> Response(
        int statusCode,
        string message,
        List<PatientInvoiceDTO>? content = null)
    {
        return new HttpResponseData<List<PatientInvoiceDTO>>
        {
            StatusCode = statusCode,
            Message = message,
            Content = content ?? new List<PatientInvoiceDTO>()
        };
    }
}