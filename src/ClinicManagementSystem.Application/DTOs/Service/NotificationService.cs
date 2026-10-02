using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Notification;
using ClinicManagementSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Application.Services;


// =====================================================
// NOTIFICATION SERVICE CONTRACT
// =====================================================

public interface INotificationService
{
    Task<
        HttpResponseData<List<NotificationDTO>>>
        GetMyNotificationsAsync(
            int currentUserId);
}


// =====================================================
// NOTIFICATION SERVICE
// =====================================================

public class NotificationService
    : INotificationService
{
    private readonly ClinicManagementDbContext _context;

    private readonly ILogger<NotificationService> _logger;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public NotificationService(
        ClinicManagementDbContext context,
        ILogger<NotificationService> logger)
    {
        _context = context;

        _logger = logger;
    }


    // =====================================================
    // GET MY NOTIFICATIONS
    //
    // GET:
    // /api/notifications
    //
    // Chỉ lấy notification của user hiện tại.
    //
    // Hiện tại dùng cho InApp notification.
    // =====================================================

    public async Task<
        HttpResponseData<List<NotificationDTO>>>
        GetMyNotificationsAsync(
            int currentUserId)
    {
        try
        {
            // =================================================
            // VALIDATE USER
            // =================================================

            if (currentUserId <= 0)
            {
                return new HttpResponseData<
                    List<NotificationDTO>>
                {
                    StatusCode = 401,

                    Message =
                        NotificationResponseMessageDTO
                            .GetFailed
                };
            }


            // =================================================
            // GET NOTIFICATIONS
            // =================================================

            var notifications =
                await _context
                    .Notifications
                    .AsNoTracking()
                    .Where(
                        n =>
                            n.UserId ==
                                currentUserId
                            &&
                            n.Channel ==
                                "InApp"
                    )
                    .OrderByDescending(
                        n => n.CreatedAt
                    )
                    .ThenByDescending(
                        n => n.Id
                    )
                    .Select(
                        n =>
                            new NotificationDTO
                            {
                                Id =
                                    n.Id,

                                AppointmentId =
                                    n.AppointmentId,

                                Channel =
                                    n.Channel,

                                Title =
                                    n.Title,

                                Content =
                                    n.Content,

                                Status =
                                    n.Status,

                                CreatedAt =
                                    n.CreatedAt,

                                SentAt =
                                    n.SentAt
                            }
                    )
                    .ToListAsync();


            // =================================================
            // SUCCESS
            // =================================================

            return new HttpResponseData<
                List<NotificationDTO>>
            {
                StatusCode = 200,

                Message =
                    NotificationResponseMessageDTO
                        .GetSuccess,

                Content =
                    notifications
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get notifications. " +
                "UserId: {UserId}",
                currentUserId
            );


            return new HttpResponseData<
                List<NotificationDTO>>
            {
                StatusCode = 500,

                Message =
                    NotificationResponseMessageDTO
                        .GetFailed
            };
        }
    }
}