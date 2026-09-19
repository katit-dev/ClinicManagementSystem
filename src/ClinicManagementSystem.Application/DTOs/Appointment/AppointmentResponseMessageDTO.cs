namespace ClinicManagementSystem.Application.DTOs.Appointment;


// =====================================================
// APPOINTMENT RESPONSE MESSAGE
// =====================================================

public static class AppointmentResponseMessageDTO
{
    public const string CreateSuccess =
        "Đặt lịch khám thành công.";

    public const string CreateFailed =
        "Đặt lịch khám thất bại.";

    public const string PatientNotFound =
        "Không tìm thấy bệnh nhân.";

    public const string DoctorNotFound =
        "Không tìm thấy bác sĩ.";

    public const string InvalidAppointmentDate =
        "Thời gian đặt lịch không hợp lệ.";

    public const string SlotNotAvailable =
        "Khung giờ này không còn khả dụng.";

    // cho phan GetMyAppointmentAsync
    public const string GetMyAppointmentsSuccess =
    "Lấy danh sách lịch hẹn thành công.";

    public const string GetMyAppointmentsFailed =
        "Lấy danh sách lịch hẹn thất bại.";

    // cho cancel appointment
    public const string CancelSuccess =
    "Hủy lịch hẹn thành công.";

    public const string CancelFailed =
        "Hủy lịch hẹn thất bại.";

    public const string AppointmentNotFound =
        "Không tìm thấy lịch hẹn.";

    public const string CannotCancelAppointment =
        "Lịch hẹn này không thể hủy.";


}