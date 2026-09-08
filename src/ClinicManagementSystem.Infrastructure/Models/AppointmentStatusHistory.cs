using System;
using System.Collections.Generic;

namespace ClinicManagementSystem.Infrastructure.Models;

public partial class AppointmentStatusHistory
{
    public int Id { get; set; }

    public int AppointmentId { get; set; }

    public byte? FromStatus { get; set; }

    public byte ToStatus { get; set; }

    public int? ChangedBy { get; set; }

    public string? Reason { get; set; }

    public DateTime ChangedAt { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;

    public virtual User? ChangedByNavigation { get; set; }
}
