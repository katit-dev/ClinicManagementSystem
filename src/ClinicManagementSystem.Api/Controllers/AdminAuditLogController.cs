using System.Security.Claims;
using ClinicManagementSystem.Application.Constants;
using ClinicManagementSystem.Application.DTOs.Audit;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Api.Controllers;

[ApiController]
[Route("api/admin/audit-logs")]
[Authorize(Roles = UserRoleConstant.Admin)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AdminAuditLogController(IAdminAuditLogService service) : ControllerBase
{
    [HttpGet("accesses")]
    public async Task<IActionResult> GetAccesses([FromQuery] AuditLogQueryDTO query)
    {
        if (!ValidActor()) return Unauthorized();
        var result = await service.GetAccessesAsync(query);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("accesses/{id:int:min(1)}")]
    public async Task<IActionResult> GetAccessById(int id)
    {
        if (!ValidActor()) return Unauthorized();
        var result = await service.GetAccessByIdAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] AuditLogQueryDTO query)
    {
        if (!ValidActor()) return Unauthorized();
        var result = await service.GetListAsync(query);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id:int:min(1)}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (!ValidActor()) return Unauthorized();
        var result = await service.GetByIdAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] AuditUserQueryDTO query)
    {
        if (!ValidActor()) return Unauthorized();
        var result = await service.GetUsersAsync(query);
        return StatusCode(result.StatusCode, result);
    }

    private bool ValidActor() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id > 0;
}
