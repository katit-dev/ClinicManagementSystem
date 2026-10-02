namespace ClinicManagementSystem.Application.DTOs.Notification;

public class NotificationDTO
{
    public int Id { get; set; }

    public int AppointmentId { get; set; }

    public string Channel { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public byte Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? SentAt { get; set; }
}