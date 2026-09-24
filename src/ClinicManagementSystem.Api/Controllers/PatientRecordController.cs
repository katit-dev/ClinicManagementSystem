using System.Security.Claims;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Api.Controllers;


// =====================================================
// PATIENT RECORD CONTROLLER
// =====================================================

[ApiController]
[Route("api/patients")]
public class PatientRecordController : ControllerBase
{
    private readonly IPatientRecordService _patientRecordService;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public PatientRecordController(
        IPatientRecordService patientRecordService)
    {
        _patientRecordService = patientRecordService;
    }


    // =====================================================
    // GET CURRENT PATIENT RECORD HISTORY
    //
    // GET:
    // /api/patients/me/records
    // =====================================================

    [HttpGet("me/records")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetPatientRecords()
    {
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var currentUserId))
        {
            return Unauthorized();
        }

        var result =
            await _patientRecordService
                .GetPatientRecordsAsync(currentUserId);

        return StatusCode(
            result.StatusCode,
            result
        );
    }


    // =====================================================
    // GET PATIENT RECORD HISTORY FOR RECEPTION
    //
    // GET:
    // /api/patients/{id}/records
    // =====================================================

    [HttpGet("{id:int}/records")]
    [Authorize(Roles = "Receptionist")]
    public async Task<IActionResult> GetPatientRecordsForReception(
        int id)
    {
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var currentUserId))
        {
            return Unauthorized();
        }

        var result =
            await _patientRecordService
                .GetPatientRecordsByPatientIdAsync(
                    id,
                    currentUserId
                );

        return StatusCode(
            result.StatusCode,
            result
        );
    }
}