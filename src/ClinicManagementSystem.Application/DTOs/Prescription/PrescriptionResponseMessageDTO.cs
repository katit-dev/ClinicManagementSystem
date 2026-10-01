namespace ClinicManagementSystem.Application.DTOs.Prescription;

// =====================================================
// PRESCRIPTION RESPONSE MESSAGE DTO
// =====================================================

public static class PrescriptionResponseMessageDTO
{
    public const string InvalidRequest =
        "Thông tin kê đơn không hợp lệ.";

    public const string MedicalRecordNotFound =
        "Không tìm thấy bệnh án.";

    public const string MedicalRecordAccessDenied =
        "Bác sĩ không có quyền thao tác với bệnh án này.";

    public const string MedicalRecordNotDraft =
        "Bệnh án không còn ở trạng thái nháp.";

    public const string PrescriptionAlreadyExists =
        "Bệnh án đã có đơn thuốc.";

    public const string MedicineNotFound =
        "Không tìm thấy thuốc.";

    public const string MedicineInactive =
        "Thuốc không còn được sử dụng.";

    public const string MedicineOutOfStock =
        "Số lượng thuốc trong kho không đủ.";

    public const string MedicineExpired =
        "Thuốc đã hết hạn.";

    public const string AllergyConfirmationRequired =
        "Thuốc có liên quan đến dị ứng của bệnh nhân. " +
        "Bác sĩ phải xác nhận trước khi kê.";

    public const string PrescriptionCreateSuccess =
        "Kê đơn thuốc thành công.";

    public const string PrescriptionCreateFailed =
        "Kê đơn thuốc thất bại.";
}