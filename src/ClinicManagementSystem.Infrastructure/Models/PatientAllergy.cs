using System;
using System.Collections.Generic;

namespace ClinicManagementSystem.Infrastructure.Models;

public partial class PatientAllergy
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public string Allergen { get; set; } = null!;

    public byte? Severity { get; set; }

    public string? Note { get; set; }

    public virtual Patient Patient { get; set; } = null!;
}
