using ClinicManagementSystem.Application.DTOs.Patient;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Api.Controllers;

[ApiController]
[Route("api/patients")]
public class PatientController : ControllerBase
{
    private readonly IPatientService _patientService;

    public PatientController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    [HttpGet("lookup")]
    public async Task<IActionResult> LookupByPhone(
    [FromQuery] PatientLookupRequestDTO request)
    {
        var result = await _patientService.LookupByPhoneAsync(request.Phone);

        return StatusCode(result.StatusCode, result);
    }
}