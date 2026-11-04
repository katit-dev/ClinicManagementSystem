using System.Security.Claims;
using ClinicManagementSystem.Application.DTOs.Support;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace ClinicManagementSystem.Api.Controllers;
[ApiController, Route("api/support/me"), Authorize(Roles="Patient")]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class PatientSupportController(ISupportChatService service):ControllerBase
{
    private int UserId=>int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:0;
    [HttpGet("conversations")]
    public async Task<IActionResult> List([FromQuery] SupportListQuery query) { var r=await service.ListAsync(UserId,"me",query);return StatusCode(r.StatusCode,r); }
    [HttpPost("conversations"),EnableRateLimiting("SupportCreate")]
    public async Task<IActionResult> Create(SupportCreateRequest request) { var r=await service.CreateAsync(UserId,request);return StatusCode(r.StatusCode,r); }
    [HttpGet("unread-summary")]
    public async Task<IActionResult> Unread() { var r=await service.UnreadAsync(UserId);return StatusCode(r.StatusCode,r); }
    [HttpPost("conversations/{id:long}/subject")]
    public async Task<IActionResult> Subject(long id,SupportSubjectRequest request) { var r=await service.UpdateSubjectAsync(UserId,id,request);return StatusCode(r.StatusCode,r); }
    [HttpGet("conversations/{id:long}/messages")]
    public async Task<IActionResult> Messages(long id,[FromQuery] SupportCursorQuery query) { var r=await service.MessagesAsync(UserId,"me",id,query);return StatusCode(r.StatusCode,r); }
    [HttpPost("conversations/{id:long}/messages"),EnableRateLimiting("SupportSend")]
    public async Task<IActionResult> Send(long id,SupportMessageRequest request) { var r=await service.SendAsync(UserId,"me",id,request);return StatusCode(r.StatusCode,r); }
    [HttpPost("conversations/{id:long}/read")]
    public async Task<IActionResult> Read(long id,SupportReadRequest request) { var r=await service.ReadAsync(UserId,"me",id,request);return StatusCode(r.StatusCode,r); }
    [HttpPost("conversations/{id:long}/close")]
    public async Task<IActionResult> Close(long id,SupportActionRequest request) { var r=await service.ActAsync(UserId,"me",id,"close",request);return StatusCode(r.StatusCode,r); }
}
[ApiController, Route("api/support/reception"), Authorize(Roles="Receptionist")]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class ReceptionSupportController(ISupportChatService service):ControllerBase
{
    private int UserId=>int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:0;
    [HttpGet("conversations")]
    public async Task<IActionResult> List([FromQuery] SupportListQuery query) { var r=await service.ListAsync(UserId,"reception",query);return StatusCode(r.StatusCode,r); }
    [HttpGet("conversations/{id:long}/messages")]
    public async Task<IActionResult> Messages(long id,[FromQuery] SupportCursorQuery query) { var r=await service.MessagesAsync(UserId,"reception",id,query);return StatusCode(r.StatusCode,r); }
    [HttpPost("conversations/{id:long}/messages"),EnableRateLimiting("SupportSend")]
    public async Task<IActionResult> Send(long id,SupportMessageRequest request) { var r=await service.SendAsync(UserId,"reception",id,request);return StatusCode(r.StatusCode,r); }
    [HttpPost("conversations/{id:long}/read")]
    public async Task<IActionResult> Read(long id,SupportReadRequest request) { var r=await service.ReadAsync(UserId,"reception",id,request);return StatusCode(r.StatusCode,r); }
    [HttpGet("conversations/{id:long}/events")]
    public async Task<IActionResult> Events(long id,[FromQuery] SupportCursorQuery query) { var r=await service.EventsAsync(UserId,"reception",id,query);return StatusCode(r.StatusCode,r); }
    [HttpPost("conversations/{id:long}/claim")]
    public async Task<IActionResult> Claim(long id,SupportActionRequest request) { var r=await service.ActAsync(UserId,"reception",id,"claim",request);return StatusCode(r.StatusCode,r); }
    [HttpPost("conversations/{id:long}/release")]
    public async Task<IActionResult> Release(long id,SupportActionRequest request) { var r=await service.ActAsync(UserId,"reception",id,"release",request);return StatusCode(r.StatusCode,r); }
    [HttpPost("conversations/{id:long}/close")]
    public async Task<IActionResult> Close(long id,SupportActionRequest request) { var r=await service.ActAsync(UserId,"reception",id,"close",request);return StatusCode(r.StatusCode,r); }
}
[ApiController, Route("api/support/admin"), Authorize(Roles="Admin")]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class AdminSupportController(ISupportChatService service):ControllerBase
{
    private int UserId=>int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:0;
    [HttpGet("conversations")]
    public async Task<IActionResult> List([FromQuery] SupportListQuery query) { var r=await service.ListAsync(UserId,"admin",query);return StatusCode(r.StatusCode,r); }
    [HttpGet("conversations/{id:long}/events")]
    public async Task<IActionResult> Events(long id,[FromQuery] SupportCursorQuery query) { var r=await service.EventsAsync(UserId,"admin",id,query);return StatusCode(r.StatusCode,r); }
    [HttpGet("receptionists")]
    public async Task<IActionResult> Receptionists() { var r=await service.ReceptionistsAsync(UserId);return StatusCode(r.StatusCode,r); }
    [HttpPost("conversations/{id:long}/assign")]
    public async Task<IActionResult> Assign(long id,SupportActionRequest request) { var r=await service.ActAsync(UserId,"admin",id,"assign",request);return StatusCode(r.StatusCode,r); }
    [HttpPost("conversations/{id:long}/transfer")]
    public async Task<IActionResult> Transfer(long id,SupportActionRequest request) { var r=await service.ActAsync(UserId,"admin",id,"transfer",request);return StatusCode(r.StatusCode,r); }
    [HttpPost("conversations/{id:long}/release")]
    public async Task<IActionResult> Release(long id,SupportActionRequest request) { var r=await service.ActAsync(UserId,"admin",id,"release",request);return StatusCode(r.StatusCode,r); }
    [HttpPost("conversations/{id:long}/close")]
    public async Task<IActionResult> Close(long id,SupportActionRequest request) { var r=await service.ActAsync(UserId,"admin",id,"close",request);return StatusCode(r.StatusCode,r); }
}
[ApiController, Route("api/support/settings")]
public sealed class SupportSettingsController(ISupportChatService service):ControllerBase
{
    [HttpGet,AllowAnonymous]
    public ActionResult<SupportSettingsDto> Settings()=>service.Settings();
}
