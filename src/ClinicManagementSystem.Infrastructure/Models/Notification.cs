using System;
using System.Collections.Generic;

namespace ClinicManagementSystem.Infrastructure.Models;

public partial class Notification
{
    public int Id { get; set; }

    public int AppointmentId { get; set; }

    public int? UserId { get; set; }

    public string Channel { get; set; } = null!;

    public string? Recipient { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public byte Status { get; set; }

    public DateTime? ScheduledAt { get; set; }

    public DateTime? SentAt { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;

    public virtual User? User { get; set; }
}
