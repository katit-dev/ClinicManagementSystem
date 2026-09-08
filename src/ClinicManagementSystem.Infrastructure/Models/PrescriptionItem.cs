using System;
using System.Collections.Generic;

namespace ClinicManagementSystem.Infrastructure.Models;

public partial class PrescriptionItem
{
    public int Id { get; set; }

    public int PrescriptionId { get; set; }

    public int MedicineId { get; set; }

    public int Quantity { get; set; }

    public string? Dosage { get; set; }

    public string? Instruction { get; set; }

    public string MedicineNameSnapshot { get; set; } = null!;

    public decimal UnitPriceSnapshot { get; set; }

    public int? DurationDays { get; set; }

    public string? Frequency { get; set; }

    public virtual Medicine Medicine { get; set; } = null!;

    public virtual Prescription Prescription { get; set; } = null!;
}
