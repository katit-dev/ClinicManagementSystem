using System.Globalization;
using ClinicManagementSystem.Application.DTOs.Doctor;

namespace ClinicManagementSystem.Web.Services;

public static class DoctorScheduleDisplay
{
    public static string Date(DateOnly date) => date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
    public static string DayName(DateOnly date) => date.DayOfWeek switch
    {
        DayOfWeek.Monday => "Thứ Hai", DayOfWeek.Tuesday => "Thứ Ba", DayOfWeek.Wednesday => "Thứ Tư",
        DayOfWeek.Thursday => "Thứ Năm", DayOfWeek.Friday => "Thứ Sáu", DayOfWeek.Saturday => "Thứ Bảy", _ => "Chủ Nhật"
    };
    public static string Time(TimeOnly value) => value.ToString("HH:mm", CultureInfo.InvariantCulture);
    public static string TimeRange(DateTime start, DateTime end)
        => $"{start:HH:mm} – {(end.Date > start.Date && end.TimeOfDay == TimeSpan.Zero ? "24:00" : end.ToString("HH:mm", CultureInfo.InvariantCulture))}";
    public static string Period(DoctorTimeOffDTO item)
    {
        if (!item.IsFullDay) return $"{item.StartAt:dd/MM/yyyy HH:mm} – {item.EndAt:dd/MM/yyyy HH:mm}";
        // Storage's full-day end is exclusive; show the actual last day of leave.
        var last = item.EndAt.TimeOfDay == TimeSpan.Zero ? item.EndAt.AddDays(-1) : item.EndAt;
        return item.StartAt.Date == last.Date ? $"{item.StartAt:dd/MM/yyyy} · Cả ngày" : $"{item.StartAt:dd/MM/yyyy} – {last:dd/MM/yyyy} · Cả ngày";
    }
    public static string BlockLabel(string kind) => kind switch { "break" => "Nghỉ giữa ca", "leave" => "Nghỉ cá nhân", "closure" => "Phòng khám nghỉ", _ => "Làm việc" };
    public static string TypeLabel(byte? type) => type switch { 0 => "Nghỉ phép", 1 => "Nghỉ lễ", 2 => "Nghỉ khác", _ => "Chưa phân loại" };
    public static string StatusLabel(string status) => status switch { "ongoing" => "Đang diễn ra", "ended" => "Đã kết thúc", _ => "Sắp tới" };
}
