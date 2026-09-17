namespace ClinicManagementSystem.Application.DTOs.Appointment;

public class SlotDTO
{
    // =====================================================
    // START TIME
    // =====================================================

    public DateTime StartTime { get; set; }


    // =====================================================
    // END TIME
    // =====================================================

    public DateTime EndTime { get; set; }


    // =====================================================
    // AVAILABLE
    // =====================================================

    public bool IsAvailable { get; set; }
}