using System;
using System.Collections.Generic;

namespace ClinicManagementSystem.Infrastructure.Models;

public partial class LabResult
{
    public int Id { get; set; }

    public int MedicalRecordServiceId { get; set; }

    public string? ResultValue { get; set; }

    public string? ReferenceRange { get; set; }

    public string? Conclusion { get; set; }

    public DateTime? ResultedAt { get; set; }

    public int? RecordedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual MedicalRecordService MedicalRecordService { get; set; } = null!;

    public virtual User? RecordedByNavigation { get; set; }
}
