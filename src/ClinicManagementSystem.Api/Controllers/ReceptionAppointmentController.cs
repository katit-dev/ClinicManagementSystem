using System.Security.Claims;
using ClinicManagementSystem.Application.Constants;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Appointment;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Api.Controllers;

[ApiController]
[Route("api/reception/appointments")]
[Authorize(Roles = UserRoleConstant.Receptionist)]
public class ReceptionAppointmentController : ControllerBase
{
    private readonly IReceptionAffectedAppointmentService _service;
    public ReceptionAppointmentController(IReceptionAffectedAppointmentService service) => _service = service;

    [HttpGet("affected")]
    public async Task<IActionResult> GetAffected([FromQuery] ReceptionAffectedQueryDTO query)
        => TryActor(out _) ? Result(await _service.GetAffectedAsync(query)) : Unauthorized();

    [HttpPatch("{id:int:min(1)}/reschedule")]
    public async Task<IActionResult> Reschedule(int id, [FromBody] ReceptionRescheduleAppointmentRequestDTO request)
        => TryActor(out var actorId) ? Result(await _service.RescheduleAsync(id, request, actorId)) : Unauthorized();

    [HttpPatch("{id:int:min(1)}/cancel")]
    public async Task<IActionResult> Cancel(int id, [FromBody] ReceptionCancelAppointmentRequestDTO request)
        => TryActor(out var actorId) ? Result(await _service.CancelAsync(id, request, actorId)) : Unauthorized();

    private bool TryActor(out int id) => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out id) && id > 0;
    private IActionResult Result<T>(HttpResponseData<T> response) => StatusCode(response.StatusCode, response);
}
