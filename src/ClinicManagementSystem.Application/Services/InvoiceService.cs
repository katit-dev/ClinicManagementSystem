using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Invoice;
using ClinicManagementSystem.Application.DTOs.Payment;

namespace ClinicManagementSystem.Application.Services;

public interface IInvoiceService
{
    Task<HttpResponseData<InvoiceDTO>> GetInvoiceAsync(int invoiceId);

    Task<HttpResponseData<InvoiceDTO>> CreatePaymentAsync(int invoiceId, PaymentRequestDTO request, int currentUserId);

    Task<HttpResponseData<InvoiceDTO>> RefundPaymentAsync(int invoiceId, PaymentRequestDTO request, int currentUserId);

    Task<HttpResponseData<InvoiceDTO>> CancelInvoiceAsync(int invoiceId, CancelInvoiceRequestDTO request, int currentUserId);
}