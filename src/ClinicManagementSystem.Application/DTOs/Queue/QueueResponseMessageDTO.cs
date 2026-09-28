namespace ClinicManagementSystem.Application.DTOs.Queue;

public static class QueueResponseMessageDTO
{
    // =====================================================
    // GET DOCTOR QUEUE
    // =====================================================

    public const string GetSuccess =
        "Lấy hàng chờ bác sĩ thành công.";

    public const string GetFailed =
        "Không thể lấy hàng chờ bác sĩ.";


    // =====================================================
    // DOCTOR
    // =====================================================

    public const string DoctorNotFound =
        "Không tìm thấy bác sĩ.";


    // =====================================================
    // RECALL
    // =====================================================

    public const string RecallPatientNotFound =
        "Không tìm thấy bệnh nhân trong hàng chờ.";

    public const string RecallSuccess =
        "Gọi bệnh nhân thành công.";

    public const string RecallFailed =
        "Không thể gọi lại số.";


    // =====================================================
    // DEFER
    // =====================================================

    public const string AppointmentNotFound =
        "Không tìm thấy lịch hẹn.";

    public const string DeferInvalidStatus =
        "Chỉ có thể chuyển bệnh nhân đang trong hàng chờ.";

    public const string DeferQueueNumberMissing =
        "Bệnh nhân chưa được cấp số thứ tự.";

    public const string DeferCurrentPatient =
        "Không thể chuyển bệnh nhân đang được khám xuống cuối hàng.";

    public const string DeferOnlyToday =
        "Chỉ có thể chuyển hàng chờ trong ngày hôm nay.";

    public const string DeferSuccess =
        "Đã chuyển bệnh nhân xuống cuối hàng.";

    public const string DeferFailed =
        "Không thể chuyển bệnh nhân xuống cuối hàng.";
}