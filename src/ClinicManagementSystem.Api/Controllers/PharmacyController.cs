using ClinicManagementSystem.Application.DTOs.Pharmacy;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClinicManagementSystem.Web.Controllers;

[ApiController]
[Route("api/prescriptions")]
[Authorize]
public class PharmacyController : ControllerBase
{
    private readonly IPharmacyService _pharmacyService;

    public PharmacyController(
        IPharmacyService pharmacyService)
    {
        _pharmacyService = pharmacyService;
    }


    // =====================================================
    // DISPENSE PRESCRIPTION
    //
    // POST:
    // /api/prescriptions/{id}/dispense
    // =====================================================

    [HttpPost("{id}/dispense")]
    [Authorize(Roles = "Pharmacist")]

    public async Task<IActionResult> Dispense(
        int id,
        [FromBody] DispenseRequestDTO request)
    {
        // =================================================
        // GET CURRENT USER
        // =================================================

        var userIdClaim =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );


        if (!int.TryParse(
                userIdClaim,
                out var currentUserId))
        {
            return Unauthorized(
                new
                {
                    statusCode = 401,
                    message =
                        "Không xác định được người dùng."
                }
            );
        }


        // =================================================
        // CALL SERVICE
        // =================================================

        var result =
            await _pharmacyService
                .DispensePrescriptionAsync(
                    id,
                    currentUserId,
                    request
                );


        // =================================================
        // RETURN RESPONSE
        // =================================================

        return StatusCode(
            result.StatusCode,
            result
        );
    }

    // =====================================================
    // REPORT SHORTAGE
    //
    // POST:
    // /api/prescriptions/{id}/report-shortage
    // =====================================================

    [HttpPost("{id}/report-shortage")]
    public async Task<IActionResult> ReportShortage(
        int id,
        [FromBody] ReportShortageRequestDTO request)
    {
        // =================================================
        // GET CURRENT USER
        // =================================================

        var userIdClaim =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );


        if (!int.TryParse(
                userIdClaim,
                out var currentUserId))
        {
            return Unauthorized(
                new
                {
                    statusCode = 401,
                    message =
                        PharmacyResponseMessageDTO
                            .UserNotFound
                }
            );
        }


        // =================================================
        // CALL SERVICE
        // =================================================

        var result =
            await _pharmacyService
                .ReportShortageAsync(
                    id,
                    currentUserId,
                    request
                );


        // =================================================
        // RETURN RESPONSE
        // =================================================

        return StatusCode(
            result.StatusCode,
            result
        );
    }
}