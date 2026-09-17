namespace ClinicManagementSystem.Application.DTOs.Doctor;

public static class DoctorResponseMessageDTO
{
    public const string GetListSuccess =
        "Lấy danh sách bác sĩ thành công.";

    public const string GetListFailed =
        "Không thể lấy danh sách bác sĩ.";

    public const string InvalidSpecialty =
        "Chuyên khoa không hợp lệ.";

    // cho available slot
    public const string GetAvailableSlotsSuccess =
    "Lấy danh sách khung giờ khám thành công.";

    public const string GetAvailableSlotsFailed =
        "Không thể lấy danh sách khung giờ khám.";

    public const string DoctorNotFound =
        "Không tìm thấy bác sĩ.";

    public const string InvalidAppointmentDate =
        "Ngày khám không hợp lệ.";
}