using System.Security.Claims;
using ClinicManagementSystem.Application.DTOs.Appointment;
using ClinicManagementSystem.Application.Enums;
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
    public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentRequestDTO request)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var currentUserId))
        {
            return Unauthorized();
        }

        var result = await _appointmentService.CreateAppointmentAsync(request, currentUserId);
        return StatusCode(result.StatusCode, result);
    }

    // =====================================================
    // GET MY APPOINTMENTS
    //
    // GET:
    // /api/appointments/my?filter=All
    // =====================================================

    [HttpGet("my")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetMyAppointments([FromQuery] MyAppointmentFilter filter = MyAppointmentFilter.All)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var currentUserId))
        {
            return Unauthorized();
        }

        var result = await _appointmentService.GetMyAppointmentsAsync(currentUserId, filter);

        return StatusCode(result.StatusCode, result);
    }

    // =====================================================
    // CANCEL APPOINTMENT
    //
    // PATCH:
    // /api/appointments/{id}/cancel
    // =====================================================

    [HttpPatch("{id}/cancel")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> CancelAppointment(int id, [FromBody] CancelAppointmentRequestDTO request)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var currentUserId))
        {
            return Unauthorized();
        }

        var result = await _appointmentService.CancelAppointmentAsync(id, request, currentUserId);

        return StatusCode(result.StatusCode, result);
    }

    // =====================================================
    // RESCHEDULE APPOINTMENT
    //
    // PATCH:
    // /api/appointments/{id}/reschedule
    // =====================================================

    [HttpPatch("{id}/reschedule")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> RescheduleAppointment(int id, [FromBody] RescheduleAppointmentRequestDTO request)
    {

        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var currentUserId))
        {
            return Unauthorized();
        }

        var result = await _appointmentService.RescheduleAppointmentAsync(id, request, currentUserId);

        return StatusCode(result.StatusCode, result);
    }


    // =====================================================
    // GET RECEPTION APPOINTMENTS
    //
    // GET:
    // /api/appointments?date=2026-09-24
    //
    // Filters:
    // doctorId
    // specialtyId
    // status
    // =====================================================

    [HttpGet]
    [Authorize(Roles = "Receptionist")]
    public async Task<IActionResult> GetReceptionAppointments(
        [FromQuery] DateOnly? date,
        [FromQuery] int? doctorId = null,
        [FromQuery] int? specialtyId = null,
        [FromQuery] AppointmentStatus? status = null)
    {
        var selectedDate = date ?? DateOnly.FromDateTime(DateTime.Now);

        var result =
            await _appointmentService
                .GetReceptionAppointmentsAsync(
                    selectedDate,
                    doctorId,
                    specialtyId,
                    status
                );


        return StatusCode(result.StatusCode, result);
    }

}