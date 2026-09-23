using System.Security.Claims;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Api.Controllers;


// =====================================================
// PATIENT RECORD CONTROLLER
// =====================================================

[ApiController]
[Route("api/patients/me/records")]
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
    // GET PATIENT RECORD HISTORY
    //
    // GET:
    // /api/patients/me/records
    // =====================================================

    [HttpGet]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetPatientRecords()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var currentUserId))
        {
            return Unauthorized();
        }

        var result = await _patientRecordService.GetPatientRecordsAsync(currentUserId);

        return StatusCode(result.StatusCode, result);
    }
}