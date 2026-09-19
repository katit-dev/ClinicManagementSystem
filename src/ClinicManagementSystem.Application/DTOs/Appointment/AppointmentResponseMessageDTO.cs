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
}