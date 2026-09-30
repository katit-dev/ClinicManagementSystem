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

    // =====================================================
    // SAVE DRAFT
    // =====================================================

    public const string SaveDraftSuccess =
        "Lưu nháp bệnh án thành công.";

    public const string SaveDraftFailed =
        "Lưu nháp bệnh án thất bại.";

    public const string MedicalRecordNotDraft =
        "Bệnh án không ở trạng thái nháp, không thể chỉnh sửa.";

    public const string MedicalRecordAccessDenied =
        "Bạn không có quyền chỉnh sửa bệnh án này.";

    // =====================================================
    // FINALIZE MEDICAL RECORD
    // =====================================================

    public const string FinalizeSuccess =
        "Chốt bệnh án thành công.";

    public const string FinalizeFailed =
        "Không thể chốt bệnh án.";

    public const string DiagnosisRequired =
        "Chẩn đoán là bắt buộc để chốt bệnh án.";

    public const string MedicalRecordAlreadyFinalized =
        "Bệnh án đã được chốt.";


    public const string MedicalRecordServicesNotCompleted =
        "Các chỉ định cận lâm sàng phải hoàn tất hoặc hủy trước khi chốt bệnh án.";

    // VC 14- CLS 
    public const string InvalidMedicalRecordServiceRequest =
        "Dữ liệu chỉ định dịch vụ không hợp lệ.";

    public const string ServiceNotFound =
        "Không tìm thấy dịch vụ.";

    public const string InvalidServiceQuantity =
        "Số lượng dịch vụ phải lớn hơn 0.";

    public const string AddServiceSuccess =
        "Chỉ định dịch vụ cận lâm sàng thành công.";

    public const string AddServiceFailed =
        "Không thể chỉ định dịch vụ cận lâm sàng.";

    public const string MedicalRecordServiceNotFound =
    "Không tìm thấy chỉ định cận lâm sàng.";

    public const string MedicalRecordServiceNotOrdered =
        "Chỉ có thể hủy chỉ định đang ở trạng thái Ordered.";

    public const string CancelServiceSuccess =
        "Hủy chỉ định cận lâm sàng thành công.";

    public const string CancelServiceFailed =
        "Không thể hủy chỉ định cận lâm sàng.";

    //add medical record service result
    public const string InvalidLabResultRequest =
    "Dữ liệu kết quả cận lâm sàng không hợp lệ.";

    public const string LabResultValueRequired =
        "Kết quả cận lâm sàng không được để trống.";

    public const string MedicalRecordServiceCancelled =
        "Chỉ định cận lâm sàng đã bị hủy.";

    public const string MedicalRecordServiceCompleted =
        "Chỉ định cận lâm sàng đã hoàn tất.";

    public const string LabResultAlreadyExists =
        "Chỉ định cận lâm sàng đã có kết quả.";

    public const string AddLabResultSuccess =
        "Nhập kết quả cận lâm sàng thành công.";

    public const string AddLabResultFailed =
        "Không thể nhập kết quả cận lâm sàng.";

    // upload attachment
    public const string AttachmentFileRequired =
    "Vui lòng chọn file.";

    public const string AttachmentFileTypeNotAllowed =
        "Định dạng file không được hỗ trợ. Chỉ chấp nhận jpg, png, pdf, dicom.";

    public const string AttachmentUploadSuccess =
        "Đính kèm file thành công.";

    public const string AttachmentUploadFailed =
        "Không thể đính kèm file.";

}