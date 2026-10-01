namespace ClinicManagementSystem.Application.DTOs.Medicine;

// =====================================================
// MEDICINE RESPONSE MESSAGE DTO
// =====================================================

public static class MedicineResponseMessageDTO
{
    // =================================================
    // VALIDATION
    // =================================================

    public const string InvalidPatientId =
        "Mã bệnh nhân không hợp lệ.";


    // =================================================
    // PATIENT
    // =================================================

    public const string PatientNotFound =
        "Không tìm thấy bệnh nhân.";


    // =================================================
    // SEARCH
    // =================================================

    public const string SearchSuccess =
        "Tìm thuốc thành công.";

    public const string SearchFailed =
        "Không thể tìm kiếm thuốc.";


    // =================================================
    // ALLERGY
    // =================================================

    public const string AllergyWarning =
        "Bệnh nhân có ghi nhận dị ứng với {0}.";
}