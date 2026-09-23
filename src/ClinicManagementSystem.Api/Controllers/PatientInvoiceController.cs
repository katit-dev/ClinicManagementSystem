using System.Security.Claims;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Api.Controllers;


// =====================================================
// PATIENT INVOICE CONTROLLER
// =====================================================

[ApiController]
[Route("api/patients/me/invoices")]
public class PatientInvoiceController : ControllerBase
{
    private readonly IPatientInvoiceService _patientInvoiceService;


    // =================================================
    // CONSTRUCTOR
    // =================================================

    public PatientInvoiceController(
        IPatientInvoiceService patientInvoiceService)
    {
        _patientInvoiceService = patientInvoiceService;
    }


    // =================================================
    // GET PATIENT INVOICES
    // =================================================

    [HttpGet]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetPatientInvoicesAsync()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var currentUserId))
        {
            return Unauthorized();
        }

        var result = await _patientInvoiceService.GetPatientInvoicesAsync(currentUserId);

        return StatusCode(result.StatusCode, result);
    }
}