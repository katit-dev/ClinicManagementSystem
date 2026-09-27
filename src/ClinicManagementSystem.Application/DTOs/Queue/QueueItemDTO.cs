namespace ClinicManagementSystem.Application.DTOs.Queue;

public class QueueItemDTO
{
    // =====================================================
    // APPOINTMENT
    // =====================================================

    public int AppointmentId { get; set; }

    public string AppointmentCode { get; set; }
        = string.Empty;


    // =====================================================
    // QUEUE
    // =====================================================

    public int QueueNumber { get; set; }


    // =====================================================
    // PATIENT
    // =====================================================

    public int PatientId { get; set; }

    public string PatientCode { get; set; }
        = string.Empty;

    public string PatientName { get; set; }
        = string.Empty;


    // =====================================================
    // APPOINTMENT TIME
    // =====================================================

    public DateTime StartTime { get; set; }

    public DateTime? CheckedInAt { get; set; }
}