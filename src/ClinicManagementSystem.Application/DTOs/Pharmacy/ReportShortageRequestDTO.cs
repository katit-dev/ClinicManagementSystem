namespace ClinicManagementSystem.Application.DTOs.Pharmacy;

// =====================================================
// REPORT SHORTAGE REQUEST DTO
// =====================================================

public class ReportShortageRequestDTO
{
    public int MedicineId { get; set; }

    public string? Note { get; set; }
}