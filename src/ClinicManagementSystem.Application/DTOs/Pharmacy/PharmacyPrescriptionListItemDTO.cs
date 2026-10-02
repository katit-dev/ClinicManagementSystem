namespace ClinicManagementSystem.Application.DTOs.Pharmacy;

// =====================================================
// PHARMACY PRESCRIPTION LIST ITEM DTO
// =====================================================
//
// Dùng cho:
// GET /api/pharmacy/prescriptions
//
// Mỗi item đại diện cho một đơn thuốc đang chờ phát.
// =====================================================

public class PharmacyPrescriptionListItemDTO
{
    // =================================================
    // PRESCRIPTION
    // =================================================

    public int PrescriptionId { get; set; }

    public int MedicalRecordId { get; set; }

    public byte Status { get; set; }

    public DateTime CreatedAt { get; set; }


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
    // PRESCRIPTION SUMMARY
    // =================================================

    public int ItemCount { get; set; }
}