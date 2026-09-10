using ClinicManagementSystem.Application.DTOs.Auth;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    // POST: /api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] UserRegisterDTO request)
    {
        var result = await _userService.RegisterUserAsync(request);

        return StatusCode(result.StatusCode, result);
    }
}