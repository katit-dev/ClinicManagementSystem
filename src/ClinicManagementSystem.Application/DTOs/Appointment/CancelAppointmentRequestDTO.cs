using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Application.DTOs.Appointment;


// =====================================================
// CANCEL APPOINTMENT REQUEST
// =====================================================

public class CancelAppointmentRequestDTO
{
    // =================================================
    // CANCEL REASON
    // =================================================

    [Required(ErrorMessage = "Vui lòng nhập lý do hủy lịch.")]
    [MaxLength(500, ErrorMessage = "Lý do hủy không được vượt quá 500 ký tự.")]
    public string CancelReason { get; set; } = string.Empty;
}