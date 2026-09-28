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

    // =================================================
    // START EXAM
    // =================================================

    public const string StartExamSuccess =
        "Bắt đầu khám thành công.";

    public const string StartExamFailed =
        "Không thể bắt đầu khám.";

    public const string AppointmentNotFound =
        "Không tìm thấy lịch hẹn.";

    public const string AppointmentNotCheckedIn =
        "Lịch hẹn chưa được check-in.";

    public const string DoctorNotFound =
        "Không tìm thấy bác sĩ.";

    public const string AppointmentNotBelongToDoctor =
        "Lịch hẹn không thuộc bác sĩ hiện tại.";

    // =====================================================
    // PATIENT HISTORY
    // =====================================================

    public const string PatientNotFound =
        "Không tìm thấy bệnh nhân.";

    public const string HistorySuccess =
        "Lấy lịch sử khám thành công.";

    public const string HistoryFailed =
        "Lấy lịch sử khám thất bại.";
}