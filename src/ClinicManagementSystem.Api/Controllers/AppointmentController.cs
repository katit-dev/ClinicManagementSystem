using ClinicManagementSystem.Application.DTOs.Appointment;
using ClinicManagementSystem.Application.Services;
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
    public async Task<IActionResult>CreateAppointment([FromBody] CreateAppointmentRequestDTO request)
    {
        var result =
            await _appointmentService
                .CreateAppointmentAsync(
                    request
                );

        return StatusCode(
            result.StatusCode,
            result
        );
    }
}