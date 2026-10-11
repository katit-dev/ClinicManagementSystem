using System.Security.Claims;
using ClinicManagementSystem.Application.Constants;
using ClinicManagementSystem.Application.DTOs.Specialty;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Api.Controllers;

[ApiController]
[Route("api/admin/specialties")]
[Authorize(Roles = UserRoleConstant.Admin)]
public class AdminSpecialtyController : ControllerBase
{
    private readonly IAdminSpecialtyService _service;

    public AdminSpecialtyController(IAdminSpecialtyService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] AdminSpecialtyQueryDTO query)
    {
        var result = await _service.GetListAsync(query);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveSpecialtyRequestDTO request)
    {
        if (!TryGetActorId(out var actorId))
            return Unauthorized();

        var result = await _service.CreateAsync(request, actorId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{id:int:min(1)}")]
    public async Task<IActionResult> Update(int id, [FromBody] SaveSpecialtyRequestDTO request)
    {
        if (!TryGetActorId(out var actorId))
            return Unauthorized();

        var result = await _service.UpdateAsync(id, request, actorId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{id:int:min(1)}/status")]
    public async Task<IActionResult> SetStatus(int id, [FromBody] SetSpecialtyStatusRequestDTO request)
    {
        if (!TryGetActorId(out var actorId))
            return Unauthorized();

        var result = await _service.SetStatusAsync(id, request.IsActive!.Value, actorId);
        return StatusCode(result.StatusCode, result);
    }

    private bool TryGetActorId(out int actorId)
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out actorId) && actorId > 0;
}
