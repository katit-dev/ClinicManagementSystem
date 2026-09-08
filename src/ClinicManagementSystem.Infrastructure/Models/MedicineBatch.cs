using System;
using System.Collections.Generic;

namespace ClinicManagementSystem.Infrastructure.Models;

public partial class MedicineBatch
{
    public int Id { get; set; }

    public int MedicineId { get; set; }

    public string BatchNo { get; set; } = null!;

    public DateOnly ExpiryDate { get; set; }

    public int Quantity { get; set; }

    public decimal? ImportPrice { get; set; }

    public virtual Medicine Medicine { get; set; } = null!;

    public virtual ICollection<MedicineStockTransaction> MedicineStockTransactions { get; set; } = new List<MedicineStockTransaction>();
}
