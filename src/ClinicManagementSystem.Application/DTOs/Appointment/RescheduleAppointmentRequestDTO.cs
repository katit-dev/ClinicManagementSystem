using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Application.DTOs.Appointment;


// =====================================================
// RESCHEDULE APPOINTMENT REQUEST
// =====================================================

public class RescheduleAppointmentRequestDTO
{
    // =================================================
    // NEW START TIME
    // =================================================

    [Required(ErrorMessage ="Vui lòng chọn thời gian khám mới.")]
    public DateTime NewStartTime { get; set; }
}