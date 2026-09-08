using System;
using System.Collections.Generic;

namespace ClinicManagementSystem.Infrastructure.Models;

public partial class MedicalRecord
{
    public int Id { get; set; }

    public int AppointmentId { get; set; }

    public int PatientId { get; set; }

    public int DoctorId { get; set; }

    public string? Symptoms { get; set; }

    public string? Diagnosis { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? Icd10Code { get; set; }

    public string? TreatmentPlan { get; set; }

    public DateOnly? FollowUpDate { get; set; }

    public byte Status { get; set; }

    public DateTime? FinalizedAt { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;

    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public virtual Doctor Doctor { get; set; } = null!;

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<MedicalRecordService> MedicalRecordServices { get; set; } = new List<MedicalRecordService>();

    public virtual Patient Patient { get; set; } = null!;

    public virtual PatientVital? PatientVital { get; set; }

    public virtual Prescription? Prescription { get; set; }
}
