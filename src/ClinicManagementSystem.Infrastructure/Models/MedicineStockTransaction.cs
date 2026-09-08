using System;
using System.Collections.Generic;

namespace ClinicManagementSystem.Infrastructure.Models;

public partial class MedicineStockTransaction
{
    public int Id { get; set; }

    public int MedicineId { get; set; }

    public int BatchId { get; set; }

    public byte Type { get; set; }

    public int Quantity { get; set; }

    public int QuantityBefore { get; set; }

    public int QuantityAfter { get; set; }

    public int? PrescriptionId { get; set; }

    public int? ReversalOfTransactionId { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? Note { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<MedicineStockTransaction> InverseReversalOfTransaction { get; set; } = new List<MedicineStockTransaction>();

    public virtual Medicine Medicine { get; set; } = null!;

    public virtual MedicineBatch MedicineBatch { get; set; } = null!;

    public virtual Prescription? Prescription { get; set; }

    public virtual MedicineStockTransaction? ReversalOfTransaction { get; set; }
}
