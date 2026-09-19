using System.Security.Claims;
using ClinicManagementSystem.Application.DTOs.Appointment;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Api.Controllers;


// =====================================================
// APPOINTMENT CONTROLLER
// =====================================================

[ApiController]
[Route("api/appointments")]
public class AppointmentController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public AppointmentController(
        IAppointmentService appointmentService)
    {
        _appointmentService =
            appointmentService;
    }


    // =====================================================
    // CREATE APPOINTMENT
    //
    // POST:
    // /api/appointments
    // =====================================================

    [HttpPost]
    [Authorize(Roles = "Patient")]
public async Task<IActionResult>CreateAppointment([FromBody] CreateAppointmentRequestDTO request)
{
    var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (!int.TryParse(userIdValue, out var currentUserId))
    {
        return Unauthorized();
    }

    var result = await _appointmentService.CreateAppointmentAsync(request, currentUserId);
    return StatusCode(result.StatusCode, result);
}
}