using System.Security.Claims;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Api.Controllers;


// =====================================================
// ATTACHMENT CONTROLLER
// =====================================================

[ApiController]
[Route("api/attachments")]
public class AttachmentController : ControllerBase
{
    private readonly IAttachmentService _attachmentService;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public AttachmentController(
        IAttachmentService attachmentService)
    {
        _attachmentService = attachmentService;
    }


    // =====================================================
    // DOWNLOAD ATTACHMENT
    //
    // GET:
    // /api/attachments/{id}/download
    // =====================================================

    [HttpGet("{id:int}/download")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> DownloadAttachment(int id)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);


        if (!int.TryParse(userIdValue, out var currentUserId))
        {
            return Unauthorized();
        }

        var result = await _attachmentService.DownloadAttachmentAsync(id, currentUserId);

        if (result == null)
        {
            return NotFound();
        }

        return File(
            result.Value.FileBytes,
            result.Value.ContentType,
            result.Value.FileName
        );
    }
}