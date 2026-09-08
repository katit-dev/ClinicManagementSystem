using System;
using System.Collections.Generic;

namespace ClinicManagementSystem.Infrastructure.Models;

public partial class Prescription
{
    public int Id { get; set; }

    public int MedicalRecordId { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public int DoctorId { get; set; }

    public byte Status { get; set; }

    public DateTime? DispensedAt { get; set; }

    public int? DispensedBy { get; set; }

    public virtual User? DispensedByNavigation { get; set; }

    public virtual Doctor Doctor { get; set; } = null!;

    public virtual MedicalRecord MedicalRecord { get; set; } = null!;

    public virtual ICollection<MedicineStockTransaction> MedicineStockTransactions { get; set; } = new List<MedicineStockTransaction>();

    public virtual ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new List<PrescriptionItem>();
}
