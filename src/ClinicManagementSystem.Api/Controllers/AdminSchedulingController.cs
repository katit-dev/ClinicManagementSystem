using System.Security.Claims;
using ClinicManagementSystem.Application.Constants;
using ClinicManagementSystem.Application.DTOs.Scheduling;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = UserRoleConstant.Admin)]
public class AdminSchedulingController : ControllerBase
{
    private readonly IAdminSchedulingService _service;
    public AdminSchedulingController(IAdminSchedulingService service) => _service = service;

    [HttpGet("scheduling/doctors")]
    public async Task<IActionResult> GetDoctors() => Result(await _service.GetDoctorsAsync());

    [HttpGet("doctor-schedules")]
    public async Task<IActionResult> GetSchedules([FromQuery] AdminScheduleQueryDTO query)
        => Result(await _service.GetSchedulesAsync(query));

    [HttpPost("doctor-schedules")]
    public async Task<IActionResult> CreateSchedule([FromBody] ScheduleRequestDTO request)
        => TryActor(out var actorId) ? Result(await _service.SaveScheduleAsync(null, request, actorId)) : Unauthorized();

    [HttpPut("doctor-schedules/{id:int:min(1)}")]
    public async Task<IActionResult> UpdateSchedule(int id, [FromBody] ScheduleRequestDTO request)
        => TryActor(out var actorId) ? Result(await _service.SaveScheduleAsync(id, request, actorId)) : Unauthorized();

    [HttpGet("time-off")]
    public async Task<IActionResult> GetTimeOffs([FromQuery] AdminTimeOffQueryDTO query)
        => Result(await _service.GetTimeOffsAsync(query));

    [HttpPost("time-off")]
    public async Task<IActionResult> CreateTimeOff([FromBody] TimeOffRequestDTO request)
        => TryActor(out var actorId) ? Result(await _service.CreateTimeOffAsync(request, actorId)) : Unauthorized();

    [HttpDelete("time-off/{id:int:min(1)}")]
    public async Task<IActionResult> DeleteTimeOff(int id)
        => TryActor(out var actorId) ? Result(await _service.DeleteTimeOffAsync(id, actorId)) : Unauthorized();

    private IActionResult Result<T>(ClinicManagementSystem.Application.DTOs.HttpResponseData<T> response)
        => StatusCode(response.StatusCode, response);
    private bool TryActor(out int id) => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out id) && id > 0;
}
