using System;
using System.Collections.Generic;

namespace ClinicManagementSystem.Infrastructure.Models;

public partial class AuditLog
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string Action { get; set; } = null!;

    public string EntityName { get; set; } = null!;

    public int? EntityId { get; set; }

    public string? Details { get; set; }

    public string? IpAddress { get; set; }

    public bool Succeeded { get; set; }

    public DateTime OccurredAt { get; set; }

    public virtual User? User { get; set; }
}
