namespace ClinicManagementSystem.Application.DTOs.Pharmacy;

// =====================================================
// PHARMACY PRESCRIPTION DETAIL RESPONSE DTO
// =====================================================
//
// Chi tiết một đơn thuốc dành cho Pharmacist.
//
// GET:
// /api/pharmacy/prescriptions/{id}
// =====================================================

public class PharmacyPrescriptionDetailResponseDTO
{
    // =================================================
    // PRESCRIPTION
    // =================================================

    public int PrescriptionId { get; set; }

    public int MedicalRecordId { get; set; }

    public byte Status { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DispensedAt { get; set; }


    // =================================================
    // PATIENT
    // =================================================

    public int PatientId { get; set; }

    public string PatientCode { get; set; } = string.Empty;

    public string PatientName { get; set; } = string.Empty;


    // =================================================
    // DOCTOR
    // =================================================

    public string DoctorName { get; set; } = string.Empty;


    // =================================================
    // ITEMS
    // =================================================

    public List<PharmacyPrescriptionDetailItemDTO> Items { get; set; }
        = new();
}