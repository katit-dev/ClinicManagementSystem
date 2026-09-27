using System.Security.Claims;
using ClinicManagementSystem.Application.DTOs.Queue;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Api.Controllers;

[ApiController]
[Route("api/queue")]
[Authorize(Roles = "Receptionist")]
public class QueueController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public QueueController(
        IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }


    // =====================================================
    // GET QUEUE
    //
    // GET:
    // /api/queue?doctorId=1&date=2026-09-28
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> GetQueue(
        [FromQuery] int doctorId,
        [FromQuery] DateOnly? date = null)
    {
        var selectedDate =
            date ??
            DateOnly.FromDateTime(
                DateTime.Now
            );


        var result = await _appointmentService.GetQueueAsync(doctorId, selectedDate);


        return StatusCode(result.StatusCode, result);
    }


    // =====================================================
    // RECALL
    //
    // POST:
    // /api/queue/{appointmentId}/recall
    // =====================================================

    [HttpPost("{appointmentId}/recall")]
    public async Task<IActionResult> Recall(int appointmentId)
    {
        var result = await _appointmentService.RecallQueueAsync(appointmentId);


        return StatusCode(result.StatusCode, result);
    }


    // =====================================================
    // DEFER
    //
    // PATCH:
    // /api/queue/{appointmentId}/defer
    // =====================================================

    [HttpPatch("{appointmentId}/defer")]
    public async Task<IActionResult> Defer(int appointmentId)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier );


        if (!int.TryParse(
            userIdValue,
            out var currentUserId))
        {
            return Unauthorized();
        }


        var result = await _appointmentService.DeferQueueAsync(appointmentId, currentUserId);


        return StatusCode(result.StatusCode, result);
    }
}