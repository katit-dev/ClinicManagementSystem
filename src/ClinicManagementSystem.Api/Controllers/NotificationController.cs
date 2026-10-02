using System.Security.Claims;

using ClinicManagementSystem.Application.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Api.Controllers;


// =====================================================
// NOTIFICATION CONTROLLER
// =====================================================

[ApiController]
[Route("api/notifications")]
public class NotificationController
    : ControllerBase
{
    private readonly INotificationService
        _notificationService;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public NotificationController(
        INotificationService notificationService)
    {
        _notificationService =
            notificationService;
    }


    // =====================================================
    // GET MY NOTIFICATIONS
    //
    // GET:
    // /api/notifications
    //
    // Chỉ Doctor hiện tại.
    // =====================================================

    [HttpGet]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult>
        GetMyNotifications()
    {
        // =================================================
        // GET CURRENT USER ID
        // =================================================

        var userIdValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );


        if (!int.TryParse(
            userIdValue,
            out var currentUserId))
        {
            return Unauthorized();
        }


        // =================================================
        // GET NOTIFICATIONS
        // =================================================

        var result =
            await _notificationService
                .GetMyNotificationsAsync(
                    currentUserId
                );


        // =================================================
        // RESPONSE
        // =================================================

        return StatusCode(
            result.StatusCode,
            result
        );
    }
}