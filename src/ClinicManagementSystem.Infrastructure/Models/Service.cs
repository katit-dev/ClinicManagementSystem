using System;
using System.Collections.Generic;

namespace ClinicManagementSystem.Infrastructure.Models;

public partial class Service
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? Code { get; set; }

    public int? SpecialtyId { get; set; }

    public int? DurationMinutes { get; set; }

    public virtual ICollection<MedicalRecordService> MedicalRecordServices { get; set; } = new List<MedicalRecordService>();

    public virtual Specialty? Specialty { get; set; }
}
