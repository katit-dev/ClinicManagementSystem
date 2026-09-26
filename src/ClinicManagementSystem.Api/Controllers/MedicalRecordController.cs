using System.Security.Claims;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Api.Controllers;


// =====================================================
// MEDICAL RECORD CONTROLLER
// =====================================================

[ApiController]
[Route("api/medical-records")]
public class MedicalRecordController : ControllerBase
{
    private readonly IMedicalRecordService
        _medicalRecordService;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public MedicalRecordController(
        IMedicalRecordService medicalRecordService)
    {
        _medicalRecordService =
            medicalRecordService;
    }


    // =====================================================
    // GET MEDICAL RECORD BY APPOINTMENT
    //
    // GET:
    // /api/medical-records?appointmentId=15
    // =====================================================

    [HttpGet]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetMedicalRecord([FromQuery] int appointmentId)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var currentUserId))
        {
            return Unauthorized();
        }

        var result = await _medicalRecordService.GetMedicalRecordByAppointmentAsync(appointmentId, currentUserId);

        return StatusCode(result.StatusCode, result);
    }

    // =====================================================
// GET PATIENT MEDICAL HISTORY
//
// GET:
// /api/medical-records/patient/{patientId}
// =====================================================

[HttpGet("patient/{patientId}")]
[Authorize(Roles = "Receptionist")]
public async Task<IActionResult> GetPatientMedicalRecords(
    int patientId)
{
    var result = await _medicalRecordService.GetPatientMedicalRecordsAsync(patientId);

    return StatusCode(result.StatusCode,result);
}
}