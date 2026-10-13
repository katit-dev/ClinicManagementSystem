using System.Security.Claims;
using ClinicManagementSystem.Application.Constants;
using ClinicManagementSystem.Application.DTOs.User;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = UserRoleConstant.Admin)]
public class AdminUserController(IAdminUserManagementService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] AdminUserQueryDTO query)
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

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        if (!TryGetActorId(out _)) return Unauthorized();

        var result = await service.GetRolesAsync();
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{id:int:min(1)}/roles")]
    public async Task<IActionResult> SetRoles(int id, [FromBody] SetUserRolesRequestDTO request)
    {
        if (!TryGetActorId(out var actorId)) return Unauthorized();

        var result = await service.SetRolesAsync(id, request, actorId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{id:int:min(1)}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] AdminResetPasswordRequestDTO request)
    {
        if (!TryGetActorId(out var actorId)) return Unauthorized();

        var result = await service.ResetPasswordAsync(id, request, actorId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{id:int:min(1)}/lock")]
    public async Task<IActionResult> SetLock(int id, [FromBody] SetUserLockRequestDTO request)
    {
        if (!TryGetActorId(out var actorId)) return Unauthorized();

        var result = await service.SetLockAsync(id, request, actorId);
        return StatusCode(result.StatusCode, result);
    }

    private bool TryGetActorId(out int id)
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out id) && id > 0;
}
