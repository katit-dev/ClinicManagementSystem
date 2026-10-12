using ClinicManagementSystem.Application.DTOs.Appointment;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Application.Helpers;

public static class DoctorSlotGenerator
{
    // Appointment and schedule times are clinic wall time; audit timestamps remain UTC.
    public static DateTime ClinicNow(TimeProvider? clock = null)
        => (clock ?? TimeProvider.System).GetUtcNow().ToOffset(TimeSpan.FromHours(7)).DateTime;

    public static List<SlotDTO> Generate(DateOnly date, IEnumerable<DoctorSchedule> schedules,
        IEnumerable<DoctorTimeOff> timeOffs, IEnumerable<Appointment> appointments, DateTime now)
    {
        var result = new List<SlotDTO>();
        var leave = timeOffs.ToList();
        var booked = appointments.Where(a => a.Status < 4).ToList();
        var effective = schedules.Where(s => s.IsActive && s.DayOfWeek == (byte)date.DayOfWeek
            && s.EffectiveFrom <= date && (!s.EffectiveTo.HasValue || s.EffectiveTo >= date));
        // Legacy seed data may leave prior versions open; choose the latest version of each shift.
        foreach (var schedule in effective.GroupBy(s => (s.DoctorId, s.StartTime))
            .Select(g => g.OrderByDescending(s => s.EffectiveFrom).ThenByDescending(s => s.Id).First()))
        {
            if (schedule.SlotMinutes <= 0 || schedule.EndTime <= schedule.StartTime) continue;
            var end = date.ToDateTime(schedule.EndTime);
            // DateTime prevents TimeOnly.AddMinutes wrapping at midnight and looping forever.
            for (var start = date.ToDateTime(schedule.StartTime);
                 (end - start).TotalMinutes >= schedule.SlotMinutes;
                 start = start.AddMinutes(schedule.SlotMinutes))
            {
                var slotEnd = start.AddMinutes(schedule.SlotMinutes);
                if (start <= now) continue;
                if (schedule.BreakStart.HasValue && schedule.BreakEnd.HasValue
                    && start < date.ToDateTime(schedule.BreakEnd.Value) && slotEnd > date.ToDateTime(schedule.BreakStart.Value)) continue;
                if (leave.Any(t => (t.DoctorId == null || t.DoctorId == schedule.DoctorId) && start < t.EndAt && slotEnd > t.StartAt)) continue;
                if (booked.Any(a => a.DoctorId == schedule.DoctorId && start < a.EndTime && slotEnd > a.StartTime)) continue;
                result.Add(new SlotDTO { StartTime = start, EndTime = slotEnd, IsAvailable = true });
            }
        }
        return result.DistinctBy(s => (s.StartTime, s.EndTime)).OrderBy(s => s.StartTime).ToList();
    }
}
