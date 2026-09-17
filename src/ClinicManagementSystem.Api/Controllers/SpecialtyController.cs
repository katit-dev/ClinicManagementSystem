using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Api.Controllers;

[ApiController]
[Route("api/specialties")]
public class SpecialtyController : ControllerBase
{
    private readonly ISpecialtyService _specialtyService;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public SpecialtyController(
        ISpecialtyService specialtyService)
    {
        _specialtyService =
            specialtyService;
    }


    // =====================================================
    // GET SPECIALTIES
    //
    // GET /api/specialties
    //
    // GET /api/specialties?active=true
    //
    // GET /api/specialties?active=false
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> GetSpecialties(
        [FromQuery] bool? active)
    {
        var result = await _specialtyService.GetSpecialtiesAsync(active);

        return StatusCode(result.StatusCode, result);
    }
}