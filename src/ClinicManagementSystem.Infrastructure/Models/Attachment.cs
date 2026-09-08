using System;
using System.Collections.Generic;

namespace ClinicManagementSystem.Infrastructure.Models;

public partial class Attachment
{
    public int Id { get; set; }

    public int MedicalRecordId { get; set; }

    public string FileUrl { get; set; } = null!;

    public string? FileType { get; set; }

    public DateTime UploadedAt { get; set; }

    public int? UploadedBy { get; set; }

    public virtual MedicalRecord MedicalRecord { get; set; } = null!;

    public virtual User? UploadedByNavigation { get; set; }
}
