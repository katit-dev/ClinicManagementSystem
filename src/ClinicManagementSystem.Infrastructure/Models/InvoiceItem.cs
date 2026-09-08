using System;
using System.Collections.Generic;

namespace ClinicManagementSystem.Infrastructure.Models;

public partial class InvoiceItem
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }

    public int? MedicineId { get; set; }

    public string? Description { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Amount { get; set; }

    public int? MedicalRecordServiceId { get; set; }

    public decimal DiscountAmount { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;

    public virtual MedicalRecordService? MedicalRecordService { get; set; }

    public virtual Medicine? Medicine { get; set; }
}
