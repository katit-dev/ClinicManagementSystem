using System;
using System.Collections.Generic;

namespace ClinicManagementSystem.Infrastructure.Models;

public partial class MedicalRecordService
{
    public int Id { get; set; }

    public int MedicalRecordId { get; set; }

    public int ServiceId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPriceSnapshot { get; set; }

    public byte Status { get; set; }

    public int? PerformedBy { get; set; }

    public DateTime OrderedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();

    public virtual LabResult? LabResult { get; set; }

    public virtual MedicalRecord MedicalRecord { get; set; } = null!;

    public virtual User? PerformedByNavigation { get; set; }

    public virtual Service Service { get; set; } = null!;
}
