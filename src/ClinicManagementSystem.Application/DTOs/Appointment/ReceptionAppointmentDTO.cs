namespace ClinicManagementSystem.Application.DTOs.Appointment;


// =====================================================
// RECEPTION APPOINTMENT DTO
// =====================================================

public class ReceptionAppointmentDTO
{
    public int Id { get; set; }

    public string AppointmentCode { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public byte Status { get; set; }

    public int? QueueNumber { get; set; }

    public DateTime? CheckedInAt { get; set; }

    public string? Reason { get; set; }

    public int PatientId { get; set; }

    public string PatientCode { get; set; } = string.Empty;

    public string PatientName { get; set; } = string.Empty;


    public int DoctorId { get; set; }

    public string DoctorName { get; set; } = string.Empty;

    public int SpecialtyId { get; set; }

    public string SpecialtyName { get; set; } = string.Empty;
}