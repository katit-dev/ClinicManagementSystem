using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Api.Controllers;

[ApiController]
[Route("api/doctors")]
public class DoctorController : ControllerBase
{
    private readonly IDoctorService _doctorService;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public DoctorController(
        IDoctorService doctorService)
    {
        _doctorService =
            doctorService;
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
            await _doctorService.GetDoctorsBySpecialtyAsync(specialtyId);

        return StatusCode(result.StatusCode, result);
    }
}