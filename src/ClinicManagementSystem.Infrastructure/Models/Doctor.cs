using System;
using System.Collections.Generic;

namespace ClinicManagementSystem.Infrastructure.Models;

public partial class Doctor
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int SpecialtyId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Title { get; set; }

    public string? LicenseNumber { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Room { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public decimal ConsultationFee { get; set; }

    public string? Bio { get; set; }

    public string? AvatarUrl { get; set; }

    public int? ExperienceYears { get; set; }

    public int? MaxPatientsPerDay { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<DoctorSchedule> DoctorSchedules { get; set; } = new List<DoctorSchedule>();

    public virtual ICollection<DoctorTimeOff> DoctorTimeOffs { get; set; } = new List<DoctorTimeOff>();

    public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();

    public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();

    public virtual Specialty Specialty { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
