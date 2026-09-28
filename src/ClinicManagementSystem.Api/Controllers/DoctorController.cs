using System.Security.Claims;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Api.Controllers;

[ApiController]
[Route("api/doctors")]
public class DoctorController : ControllerBase
{
    private readonly IDoctorService _doctorService;
    private readonly IAppointmentService _appointmentService;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public DoctorController(
    IDoctorService doctorService,
    IAppointmentService appointmentService)
    {
        _doctorService = doctorService;
        _appointmentService = appointmentService;
    }


    // =====================================================
    // GET DOCTORS BY SPECIALTY
    //
    // GET /api/doctors?specialtyId=1
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> GetDoctors(
        [FromQuery] int specialtyId)
    {
        var result =
            await _doctorService
                .GetDoctorsBySpecialtyAsync(
                    specialtyId
                );

        return StatusCode(
            result.StatusCode,
            result
        );
    }


    // =====================================================
    // GET AVAILABLE SLOTS
    //
    // GET
    // /api/doctors/1/available-slots?date=2026-09-21
    // =====================================================

    [HttpGet("{id}/available-slots")]
    public async Task<IActionResult> GetAvailableSlots(
        [FromRoute] int id,
        [FromQuery] DateOnly date)
    {
        var result =
            await _doctorService
                .GetAvailableSlotsAsync(
                    id,
                    date
                );

        return StatusCode(
            result.StatusCode,
            result
        );
    }

    // =====================================================
    // GET MY QUEUE
    //
    // GET:
    // /api/doctors/me/queue?date=2026-09-28
    //
    // DoctorId lấy từ JWT
    // Date do service xử lý
    // =====================================================

    [HttpGet("me/queue")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> GetMyQueue(
        [FromQuery] DateOnly? date)
    {
        // =================================================
        // GET CURRENT USER ID
        // =================================================

        var userIdValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );


        if (!int.TryParse(
            userIdValue,
            out var currentUserId))
        {
            return Unauthorized();
        }


        // =================================================
        // GET DOCTOR QUEUE
        // =================================================

        var result =
            await _appointmentService
                .GetMyDoctorQueueAsync(
                    currentUserId,
                    date
                );


        return StatusCode(
            result.StatusCode,
            result
        );
    }

}