using System;
using System.Collections.Generic;

namespace ClinicManagementSystem.Infrastructure.Models;

public partial class Appointment
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public int DoctorId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public byte Status { get; set; }

    public string? Reason { get; set; }

    public string? Note { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string AppointmentCode { get; set; } = null!;

    public int? QueueNumber { get; set; }

    public DateTime? CheckedInAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? CancelReason { get; set; }

    public byte Source { get; set; }

    public decimal FeeSnapshot { get; set; }

    public virtual ICollection<AppointmentStatusHistory> AppointmentStatusHistories { get; set; } = new List<AppointmentStatusHistory>();

    public virtual User? CreatedByNavigation { get; set; }

    public virtual Doctor Doctor { get; set; } = null!;

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual MedicalRecord? MedicalRecord { get; set; }

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual Patient Patient { get; set; } = null!;
}
