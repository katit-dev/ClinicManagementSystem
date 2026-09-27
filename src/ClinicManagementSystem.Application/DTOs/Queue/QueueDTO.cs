namespace ClinicManagementSystem.Application.DTOs.Queue;

public class QueueDTO
{
    // =====================================================
    // DOCTOR
    // =====================================================

    public int DoctorId { get; set; }

    public string DoctorName { get; set; }
        = string.Empty;

    public string SpecialtyName { get; set; }
        = string.Empty;


    // =====================================================
    // DATE
    // =====================================================

    public DateOnly Date { get; set; }


    // =====================================================
    // CURRENT
    // =====================================================

    public QueueItemDTO? Current { get; set; }


    // =====================================================
    // NEXT
    // =====================================================

    public QueueItemDTO? Next { get; set; }


    // =====================================================
    // WAITING
    // =====================================================

    public int WaitingCount { get; set; }

    public List<QueueItemDTO> Waiting { get; set; }
        = new();
}