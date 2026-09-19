namespace ClinicManagementSystem.Application.DTOs.Appointment;


// =====================================================
// MY APPOINTMENT DTO
// =====================================================

public class MyAppointmentDTO
{
    // =================================================
    // APPOINTMENT
    // =================================================

    public int Id { get; set; }

    public string AppointmentCode { get; set; }
        = string.Empty;


    // =================================================
    // DOCTOR
    // =================================================

    public string DoctorName { get; set; }
        = string.Empty;


    // =================================================
    // SPECIALTY
    // =================================================

    public string SpecialtyName { get; set; }
        = string.Empty;


    // =================================================
    // APPOINTMENT TIME
    // =================================================

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }


    // =================================================
    // STATUS
    // =================================================

    public byte Status { get; set; }


    // =================================================
    // QUEUE
    // =================================================

    public int? QueueNumber { get; set; }
}