using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Application.DTOs.Prescription;

// =====================================================
// PRESCRIPTION REQUEST DTO
// =====================================================

public class PrescriptionRequestDTO
{
    // =================================================
    // NOTE
    // =================================================

    public string? Note { get; set; }


    // =================================================
    // PRESCRIPTION ITEMS
    //
    // Tối thiểu phải có 1 thuốc.
    // =================================================

    [MinLength(1)]
    public List<PrescriptionItemRequestDTO> Items { get; set; }
        = new();
}