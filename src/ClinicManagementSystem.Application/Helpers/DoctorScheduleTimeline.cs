using ClinicManagementSystem.Application.DTOs.Doctor;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Application.Helpers;

public static class DoctorScheduleTimeline
{
    public static List<DoctorScheduleDayDTO> Build(int doctorId, DateOnly from, DateOnly to,
        IEnumerable<DoctorSchedule> schedules, IEnumerable<DoctorTimeOff> timeOffs, DateTime now)
    {
        var shifts = schedules.Where(s => s.DoctorId == doctorId && s.IsActive && s.EndTime > s.StartTime && s.SlotMinutes > 0).ToList();
        var leave = timeOffs.Where(t => (t.DoctorId == doctorId || t.DoctorId == null) && t.EndAt > t.StartAt).ToList();
        var days = new List<DoctorScheduleDayDTO>();
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var start = date.ToDateTime(TimeOnly.MinValue);
            var end = date.AddDays(1).ToDateTime(TimeOnly.MinValue);
            var day = new DoctorScheduleDayDTO { Date = date, IsToday = date == DateOnly.FromDateTime(now) };
            var dailyLeave = leave.Where(t => t.StartAt < end && t.EndAt > start).OrderBy(t => t.StartAt).ThenBy(t => t.Id).ToList();
            day.TimeOffs = dailyLeave.Select(t => Map(t, now)).ToList();
            foreach (var t in dailyLeave)
                day.Blocks.Add(new() { Kind = t.DoctorId == null ? "closure" : "leave", StartAt = Max(start, t.StartAt), EndAt = Min(end, t.EndAt), TimeOffId = t.Id });

            // Match booking's latest-version policy for legacy overlapping schedule versions.
            var effective = shifts.Where(s => s.DayOfWeek == (byte)date.DayOfWeek && s.EffectiveFrom <= date
                    && (!s.EffectiveTo.HasValue || s.EffectiveTo >= date))
                .GroupBy(s => s.StartTime).Select(g => g.OrderByDescending(s => s.EffectiveFrom).ThenByDescending(s => s.Id).First())
                .OrderBy(s => s.StartTime);
            foreach (var s in effective)
            {
                day.Shifts.Add(new() { Id = s.Id, StartTime = s.StartTime, EndTime = s.EndTime, BreakStart = s.BreakStart,
                    BreakEnd = s.BreakEnd, SlotMinutes = s.SlotMinutes, EffectiveFrom = s.EffectiveFrom, EffectiveTo = s.EffectiveTo });
                var shiftStart = date.ToDateTime(s.StartTime);
                var shiftEnd = date.ToDateTime(s.EndTime);
                var exclusions = dailyLeave.Select(t => (Start: Max(shiftStart, t.StartAt), End: Min(shiftEnd, t.EndAt)))
                    .Where(t => t.End > t.Start).ToList();
                if (s.BreakStart.HasValue && s.BreakEnd.HasValue && s.BreakEnd > s.BreakStart)
                {
                    var breakStart = Max(shiftStart, date.ToDateTime(s.BreakStart.Value));
                    var breakEnd = Min(shiftEnd, date.ToDateTime(s.BreakEnd.Value));
                    if (breakEnd > breakStart)
                    {
                        exclusions.Add((breakStart, breakEnd));
                        day.Blocks.Add(new() { Kind = "break", StartAt = breakStart, EndAt = breakEnd, ShiftId = s.Id });
                    }
                }
                var cursor = shiftStart;
                foreach (var span in exclusions.OrderBy(t => t.Start).ThenBy(t => t.End))
                {
                    if (span.Start > cursor) day.Blocks.Add(new() { Kind = "work", StartAt = cursor, EndAt = span.Start, ShiftId = s.Id });
                    cursor = Max(cursor, span.End);
                }
                if (cursor < shiftEnd) day.Blocks.Add(new() { Kind = "work", StartAt = cursor, EndAt = shiftEnd, ShiftId = s.Id });
            }
            day.Blocks = day.Blocks.OrderBy(b => b.StartAt).ThenBy(b => b.Kind).ThenBy(b => b.ShiftId).ThenBy(b => b.TimeOffId).ToList();
            days.Add(day);
        }
        return days;
    }

    public static DoctorTimeOffDTO Map(DoctorTimeOff t, DateTime now) => new()
    {
        Id = t.Id, StartAt = t.StartAt, EndAt = t.EndAt, Type = t.Type, Reason = t.Reason,
        IsFullDay = t.IsFullDay, IsClinicWide = t.DoctorId == null,
        PeriodStatus = t.EndAt <= now ? "ended" : t.StartAt > now ? "upcoming" : "ongoing"
    };
    private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;
    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;
}
