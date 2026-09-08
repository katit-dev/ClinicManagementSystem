using System;
using System.Collections.Generic;

namespace ClinicManagementSystem.Infrastructure.Models;

public partial class PatientVital
{
    public int Id { get; set; }

    public int MedicalRecordId { get; set; }

    public decimal? Temperature { get; set; }

    public int? Pulse { get; set; }

    public string? BloodPressure { get; set; }

    public decimal? Weight { get; set; }

    public decimal? Height { get; set; }

    public virtual MedicalRecord MedicalRecord { get; set; } = null!;
}
