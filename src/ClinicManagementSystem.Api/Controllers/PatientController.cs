using ClinicManagementSystem.Application.DTOs.Patient;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
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

    // =====================================================
    // SEARCH PATIENTS
    //
    // GET:
    // /api/patients?keyword=&page=
    // =====================================================

    [HttpGet]
    [Authorize(Roles = "Receptionist")]
    public async Task<IActionResult> SearchPatients(
        [FromQuery] string? keyword = null,
        [FromQuery] int page = 1)
    {
        var result = await _patientService.SearchPatientsAsync(keyword, page);

        return StatusCode(result.StatusCode, result);
    }

    // =====================================================
    // CREATE PATIENT
    //
    // POST:
    // /api/patients
    // =====================================================

    [HttpPost]
    [Authorize(Roles = "Receptionist")]
    public async Task<IActionResult> CreatePatient(
        [FromBody] PatientRequestDTO request)
    {
        var result = await _patientService.CreatePatientAsync(request);

        return StatusCode(result.StatusCode, result);
    }

    // =====================================================
    // UPDATE PATIENT
    //
    // PUT:
    // /api/patients/{id}
    // =====================================================

    [HttpPut("{id}")]
    [Authorize(Roles = "Receptionist")]
    public async Task<IActionResult> UpdatePatient(
        int id,
        [FromBody] PatientRequestDTO request)
    {
        var result = await _patientService.UpdatePatientAsync(id, request);

        return StatusCode(result.StatusCode, result);
    }


}