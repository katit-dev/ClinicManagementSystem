using System.Security.Claims;
using ClinicManagementSystem.Application.Constants;
using ClinicManagementSystem.Application.DTOs.Service;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Api.Controllers;

[ApiController]
[Route("api/admin/services")]
[Authorize(Roles = UserRoleConstant.Admin)]
public class AdminServiceController(IAdminServiceCatalogService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] AdminServiceQueryDTO query)
    {
        if (!TryGetActorId(out _)) return Unauthorized();
        var result = await service.GetListAsync(query);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id:int:min(1)}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (!TryGetActorId(out _)) return Unauthorized();
        var result = await service.GetByIdAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveServiceRequestDTO request)
    {
        if (!TryGetActorId(out var actorId)) return Unauthorized();
        var result = await service.CreateAsync(request, actorId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{id:int:min(1)}")]
    public async Task<IActionResult> Update(int id, [FromBody] SaveServiceRequestDTO request)
    {
        if (!TryGetActorId(out var actorId)) return Unauthorized();
        var result = await service.UpdateAsync(id, request, actorId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{id:int:min(1)}/deactivate")]
    public async Task<IActionResult> Deactivate(int id)
    {
        if (!TryGetActorId(out var actorId)) return Unauthorized();
        var result = await service.DeactivateAsync(id, actorId);
        return StatusCode(result.StatusCode, result);
    }

    private bool TryGetActorId(out int actorId)
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out actorId) && actorId > 0;
}
