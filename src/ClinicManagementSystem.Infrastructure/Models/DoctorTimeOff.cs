using System;
using System.Collections.Generic;

namespace ClinicManagementSystem.Infrastructure.Models;

public partial class DoctorTimeOff
{
    public int Id { get; set; }

    public int? DoctorId { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }

    public byte? Type { get; set; }

    public bool IsFullDay { get; set; }

    public int? ApprovedBy { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual Doctor? Doctor { get; set; }
}
