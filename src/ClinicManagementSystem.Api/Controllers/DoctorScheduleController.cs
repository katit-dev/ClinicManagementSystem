using System.Security.Claims;
using ClinicManagementSystem.Application.DTOs.Doctor;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Api.Controllers;

[ApiController]
[Route("api/doctors/me")]
[Authorize(Roles = "Doctor")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class DoctorScheduleController(IDoctorScheduleService service) : ControllerBase
{
    [HttpGet("schedule")]
    public async Task<IActionResult> GetMySchedule([FromQuery] DoctorScheduleQueryDTO query)
    {
        if (!TryUser(out var userId)) return Unauthorized();
        var result = await service.GetMyScheduleAsync(query, userId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("time-off")]
    public async Task<IActionResult> GetMyTimeOff([FromQuery] DoctorTimeOffQueryDTO query)
    {
        if (!TryUser(out var userId)) return Unauthorized();
        var result = await service.GetMyTimeOffAsync(query, userId);
        return StatusCode(result.StatusCode, result);
    }

    private bool TryUser(out int id) => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out id) && id > 0;
}
