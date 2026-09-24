using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Application.DTOs.Appointment;


// =====================================================
// NO SHOW APPOINTMENT REQUEST DTO
// =====================================================

public class NoShowAppointmentRequestDTO
{
    [MaxLength(500)]
    public string? Note { get; set; }
}