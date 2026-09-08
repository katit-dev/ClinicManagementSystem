using System;
using System.Collections.Generic;

namespace ClinicManagementSystem.Infrastructure.Models;

public partial class RefreshToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? DeviceInfo { get; set; }

    public string? IpAddress { get; set; }

    public string? ReplacedByTokenHash { get; set; }

    public virtual User User { get; set; } = null!;
}
