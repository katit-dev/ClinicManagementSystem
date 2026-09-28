namespace ClinicManagementSystem.Application.DTOs.Queue;

public class DoctorQueueItemDTO
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

    public int Age { get; set; }


    // =====================================================
    // APPOINTMENT
    // =====================================================

    public DateTime StartTime { get; set; }


    // =====================================================
    // REASON
    // =====================================================

    public string? Reason { get; set; }


    // =====================================================
    // ALLERGY WARNING
    // =====================================================

    public string? AllergyWarning { get; set; }
}