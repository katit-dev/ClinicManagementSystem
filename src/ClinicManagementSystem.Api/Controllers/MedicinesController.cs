using ClinicManagementSystem.Application.DTOs.Medicine;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Api.Controllers;

[ApiController]
[Route("api/medicines")]
[Authorize(Roles = "Doctor")]
public class MedicinesController : ControllerBase
{
    private readonly IMedicineService _medicineService;


    public MedicinesController(
        IMedicineService medicineService)
    {
        _medicineService =
            medicineService;
    }


    // =====================================================
    // SEARCH MEDICINES
    //
    // GET:
    // /api/medicines/search?keyword=para&patientId=1
    // =====================================================

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? keyword,
        [FromQuery] int patientId)
    {
        var result =
            await _medicineService
                .SearchMedicinesAsync(
                    keyword,
                    patientId
                );

        return StatusCode(
            result.StatusCode,
            result
        );
    }
}