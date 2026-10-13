using System.Security.Claims;
using ClinicManagementSystem.Application.Constants;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Report;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Api.Controllers;

[ApiController]
[Route("api/admin/reports")]
[Authorize(Roles = UserRoleConstant.Admin)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AdminReportController(IAdminReportService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetReport([FromQuery] ClinicReportQueryDTO query)
    {
        if (!ValidActor()) return Unauthorized();
        var result = await service.GetReportAsync(query);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("export")]
    [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK, ClinicReportExportDTO.ContentType)]
    [ProducesResponseType(typeof(HttpResponseData<ClinicReportExportDTO>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(HttpResponseData<ClinicReportExportDTO>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(HttpResponseData<ClinicReportExportDTO>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Export([FromQuery] ClinicReportQueryDTO query)
    {
        if (!ValidActor()) return Unauthorized();
        var result = await service.ExportAsync(query);
        if (result.StatusCode != 200 || result.Content == null) return StatusCode(result.StatusCode, result);
        return File(result.Content.Bytes, ClinicReportExportDTO.ContentType, result.Content.FileName);
    }

    private bool ValidActor() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id > 0;
}
