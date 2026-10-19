using ClinicManagementSystem.Application.Enums;

namespace ClinicManagementSystem.Application.Helpers;

public static class MedicalRecordEditPolicy
{
    public static string? ReadOnlyReason(byte recordStatus, byte? appointmentStatus)
    {
        if (recordStatus == (byte)MedicalRecordStatus.Finalized)
            return "Bệnh án đã chốt, chỉ được xem thông tin.";
        if (recordStatus != (byte)MedicalRecordStatus.Draft)
            return "Bệnh án không ở trạng thái cho phép chỉnh sửa.";
        return appointmentStatus switch
        {
            (byte)AppointmentStatus.CheckedIn => null,
            (byte)AppointmentStatus.Cancelled => "Lịch hẹn đã hủy, bệnh án chỉ được xem thông tin.",
            (byte)AppointmentStatus.NoShow => "Bệnh nhân không đến, bệnh án chỉ được xem thông tin.",
            (byte)AppointmentStatus.Completed => "Lịch khám đã hoàn tất, bệnh án chỉ được xem thông tin.",
            null => "Không tìm thấy lịch hẹn của bệnh án.",
            _ => "Lịch hẹn chưa được check-in, chưa thể chỉnh sửa bệnh án."
        };
    }
}
