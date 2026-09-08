using System;
using System.Collections.Generic;

namespace ClinicManagementSystem.Infrastructure.Models;

public partial class InsurancePolicy
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public string PolicyNumber { get; set; } = null!;

    public string? ProviderName { get; set; }

    public decimal? CoveragePercent { get; set; }

    public byte? RouteType { get; set; }

    public DateOnly? ValidFrom { get; set; }

    public DateOnly? ValidTo { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Patient Patient { get; set; } = null!;
}
