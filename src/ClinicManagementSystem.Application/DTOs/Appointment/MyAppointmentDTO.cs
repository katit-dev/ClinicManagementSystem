namespace ClinicManagementSystem.Application.DTOs.Appointment;

public class MyAppointmentDTO
{
    public int Id { get; set; }

    public string AppointmentCode { get; set; } = string.Empty;

    public int DoctorId { get; set; }

    public string DoctorName { get; set; } = string.Empty;

    public string SpecialtyName { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public byte Status { get; set; }

    public int? QueueNumber { get; set; }
}