namespace ClinicManagementSystem.Application.DTOs.MedicalRecord;


// =====================================================
// MEDICAL RECORD RESPONSE MESSAGE
// =====================================================

public static class MedicalRecordResponseMessageDTO
{
    public const string GetSuccess =
        "Lấy bệnh án thành công.";

    public const string GetFailed =
        "Lấy bệnh án thất bại.";

    public const string MedicalRecordNotFound =
        "Không tìm thấy bệnh án.";

    public const string MedicalRecordNotFinalized =
        "Bệnh án chưa được hoàn tất.";
}