namespace ClinicManagementSystem.Application.DTOs.Doctor;

public sealed class DoctorMedicalRecordDTO
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public int PatientId { get; set; }
    public string PatientCode { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public DateTime AppointmentStartTime { get; set; }
    public byte Status { get; set; }
    public byte AppointmentStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public bool CanContinue { get; set; }
    public string? ReadOnlyReason { get; set; }
}
