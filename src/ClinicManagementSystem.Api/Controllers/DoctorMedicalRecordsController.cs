using System.Security.Claims;
using ClinicManagementSystem.Application.DTOs.Doctor;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Api.Controllers;

[ApiController]
[Route("api/doctors/me/medical-records")]
[Authorize(Roles = "Doctor")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class DoctorMedicalRecordsController(IDoctorMedicalRecordService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMyMedicalRecords([FromQuery] DoctorMedicalRecordQueryDTO query)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId <= 0)
            return Unauthorized();
        var result = await service.GetMyRecordsAsync(query, userId);
        return StatusCode(result.StatusCode, result);
    }
}
