using System.Security.Claims;
using ClinicManagementSystem.Application.DTOs.Invoice;
using ClinicManagementSystem.Application.DTOs.Payment;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Api.Controllers;

// =====================================================
// INVOICE CONTROLLER
// =====================================================

[ApiController]
[Route("api/invoices")]
[Authorize(Roles = "Receptionist")]
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public InvoiceController(
        IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }


    // =====================================================
    // GET INVOICE
    //
    // GET:
    // /api/invoices/{id}
    // =====================================================

    [HttpGet("{id}")]
    public async Task<IActionResult> GetInvoice(
        int id)
    {
        var result = await _invoiceService.GetInvoiceAsync(id);

        return StatusCode(result.StatusCode, result);
    }


    // =====================================================
    // CREATE PAYMENT
    //
    // POST:
    // /api/invoices/{id}/payments
    // =====================================================

    [HttpPost("{id}/payments")]
    public async Task<IActionResult> CreatePayment(
        int id,
        [FromBody] PaymentRequestDTO request)
    {
        var userIdValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );


        if (!int.TryParse(
            userIdValue,
            out var currentUserId))
        {
            return Unauthorized();
        }


        var result = await _invoiceService.CreatePaymentAsync(id, request, currentUserId);

        return StatusCode(result.StatusCode, result);
    }

    // =====================================================
    // REFUND PAYMENT
    //
    // POST:
    // /api/invoices/{id}/refunds
    //
    // Receptionist hoàn tiền cho hóa đơn
    // =====================================================

    [HttpPost("{id}/refunds")]
    [Authorize(Roles = "Receptionist")]
    public async Task<IActionResult> RefundPayment(
        int id,
        [FromBody] PaymentRequestDTO request)
    {
        var userIdValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

        if (!int.TryParse(
            userIdValue,
            out var currentUserId))
        {
            return Unauthorized();
        }

        var result =
            await _invoiceService
                .RefundPaymentAsync(
                    id,
                    request,
                    currentUserId
                );

        return StatusCode(
            result.StatusCode,
            result
        );
    }

    // =====================================================
    // CANCEL INVOICE
    //
    // PATCH:
    // /api/invoices/{id}/cancel
    //
    // Receptionist hủy hóa đơn
    // Chỉ được hủy khi paid_amount = 0
    // =====================================================

    [HttpPatch("{id}/cancel")]
    [Authorize(Roles = "Receptionist")]
    public async Task<IActionResult> CancelInvoice(
        int id,
        [FromBody] CancelInvoiceRequestDTO request)
    {
        var userIdValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

        if (!int.TryParse(
            userIdValue,
            out var currentUserId))
        {
            return Unauthorized();
        }

        var result =
            await _invoiceService
                .CancelInvoiceAsync(
                    id,
                    request,
                    currentUserId
                );

        return StatusCode(
            result.StatusCode,
            result
        );
    }
}