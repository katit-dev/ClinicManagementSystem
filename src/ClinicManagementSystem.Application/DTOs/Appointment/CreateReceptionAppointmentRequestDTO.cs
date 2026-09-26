using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Application.DTOs.Appointment;


public class CreateReceptionAppointmentRequestDTO
{
    // =====================================================
    // PATIENT
    // =====================================================

    [Required(
        ErrorMessage = "Bệnh nhân không được để trống")]
    public int PatientId { get; set; }



    // =====================================================
    // DOCTOR
    // =====================================================

    [Required(
        ErrorMessage = "Bác sĩ không được để trống")]
    public int DoctorId { get; set; }



    // =====================================================
    // START TIME
    // =====================================================

    [Required(
        ErrorMessage = "Thời gian khám không được để trống")]
    public DateTime StartTime { get; set; }



    // =====================================================
    // REASON
    // =====================================================

    [MaxLength(
        500,
        ErrorMessage =
        "Lý do khám không được vượt quá 500 ký tự")]
    public string? Reason { get; set; }



    // =====================================================
    // SOURCE
    //
    // WalkIn = 1
    // Phone  = 2
    // =====================================================

    [Required]
    public byte Source { get; set; }
}