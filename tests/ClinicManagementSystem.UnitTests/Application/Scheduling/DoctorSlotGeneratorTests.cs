using ClinicManagementSystem.Application.Helpers;
using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.Repositories;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ClinicManagementSystem.UnitTests.Application.AdminScheduling;

public class DoctorSlotGeneratorTests
{
    private static readonly DateOnly Date = new(2026, 10, 19);
    private static readonly DateTime BeforeDate = new(2026, 10, 12, 9, 0, 0);
    private static DoctorSchedule Shift(int hourFrom = 8, int hourTo = 10) => new()
    {
        DoctorId = 1, DayOfWeek = 1, IsActive = true, StartTime = new(hourFrom, 0), EndTime = new(hourTo, 0),
        SlotMinutes = 30, EffectiveFrom = new(2026, 10, 1)
    };

    [Fact]
    public void MultipleShifts_ReturnAllSlotsInChronologicalOrder()
    {
        var result = DoctorSlotGenerator.Generate(Date, new[] { Shift(13, 15), Shift() }, Array.Empty<DoctorTimeOff>(), Array.Empty<Appointment>(), BeforeDate);
        Assert.Equal(8, result.Count);
        Assert.Equal(8, result[0].StartTime.Hour);
        Assert.Equal(14, result[^1].StartTime.Hour);
    }

    [Fact]
    public void ClinicWideLeave_RemovesAllDoctorsSlots()
    {
        var leave = new DoctorTimeOff { DoctorId = null, StartAt = Date.ToDateTime(TimeOnly.MinValue), EndAt = Date.AddDays(1).ToDateTime(TimeOnly.MinValue) };
        Assert.Empty(DoctorSlotGenerator.Generate(Date, new[] { Shift() }, new[] { leave }, Array.Empty<Appointment>(), BeforeDate));
        var other = Shift(); other.DoctorId = 2;
        Assert.Empty(DoctorSlotGenerator.Generate(Date, new[] { other }, new[] { leave }, Array.Empty<Appointment>(), BeforeDate));
    }

    [Fact]
    public void PartialLeave_RemovesOverlappingSlotsOnlyAndHonorsExclusiveEnd()
    {
        var leave = new DoctorTimeOff { DoctorId = 1, StartAt = new(2026, 10, 19, 8, 15, 0), EndAt = new(2026, 10, 19, 9, 0, 0) };
        var result = DoctorSlotGenerator.Generate(Date, new[] { Shift() }, new[] { leave }, Array.Empty<Appointment>(), BeforeDate);
        Assert.Equal(2, result.Count);
        Assert.Equal(new DateTime(2026, 10, 19, 9, 0, 0), result[0].StartTime);
    }

    [Fact]
    public void AnotherDoctorsLeave_DoesNotRemoveSlots()
    {
        var leave = new DoctorTimeOff { DoctorId = 2, StartAt = new(2026, 10, 19), EndAt = new(2026, 10, 20) };
        Assert.Equal(4, DoctorSlotGenerator.Generate(Date, new[] { Shift() }, new[] { leave }, Array.Empty<Appointment>(), BeforeDate).Count);
    }

    [Fact]
    public void BreakAndBookingAndPastTime_AllExcludeSlots()
    {
        var shift = Shift(); shift.BreakStart = new(8, 30); shift.BreakEnd = new(9, 0);
        var booking = new Appointment { DoctorId = 1, Status = 0, StartTime = new(2026, 10, 19, 9, 0, 0), EndTime = new(2026, 10, 19, 9, 30, 0) };
        var result = DoctorSlotGenerator.Generate(Date, new[] { shift }, Array.Empty<DoctorTimeOff>(), new[] { booking }, new(2026, 10, 19, 8, 0, 0));
        Assert.Equal(new DateTime(2026, 10, 19, 9, 30, 0), Assert.Single(result).StartTime);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    public void CancelledAndNoShowAppointments_DoNotBlockSlots(byte status)
    {
        var booking = new Appointment { DoctorId = 1, Status = status, StartTime = new(2026, 10, 19, 8, 0, 0), EndTime = new(2026, 10, 19, 10, 0, 0) };
        Assert.Equal(4, DoctorSlotGenerator.Generate(Date, new[] { Shift() }, Array.Empty<DoctorTimeOff>(), new[] { booking }, BeforeDate).Count);
    }

    [Fact]
    public void VersionBoundary_SelectsEffectiveScheduleAndKeepsPreviousDates()
    {
        var old = Shift(); old.EffectiveTo = new(2026, 10, 18);
        var next = Shift(13, 15); next.EffectiveFrom = Date;
        var current = DoctorSlotGenerator.Generate(Date, new[] { old, next }, Array.Empty<DoctorTimeOff>(), Array.Empty<Appointment>(), BeforeDate);
        Assert.All(current, slot => Assert.True(slot.StartTime.Hour >= 13));
        var previousDate = Date.AddDays(-7);
        var previous = DoctorSlotGenerator.Generate(previousDate, new[] { old, next }, Array.Empty<DoctorTimeOff>(), Array.Empty<Appointment>(), BeforeDate.AddDays(-7));
        Assert.Equal(4, previous.Count);
        Assert.All(previous, slot => Assert.True(slot.StartTime.Hour < 10));
    }

    [Fact]
    public void NearMidnight_DoesNotWrapOrGeneratePartialSlot()
    {
        var shift = Shift(23, 23); shift.EndTime = new(23, 59); shift.SlotMinutes = 60;
        Assert.Empty(DoctorSlotGenerator.Generate(Date, new[] { shift }, Array.Empty<DoctorTimeOff>(), Array.Empty<Appointment>(), BeforeDate));
        shift.SlotMinutes = 30;
        Assert.Single(DoctorSlotGenerator.Generate(Date, new[] { shift }, Array.Empty<DoctorTimeOff>(), Array.Empty<Appointment>(), BeforeDate));
    }

    [Fact]
    public void LegacyOpenVersions_UseLatestVersionForEachShift()
    {
        var old = Shift(); old.SlotMinutes = 30;
        var next = Shift(); next.EffectiveFrom = Date; next.SlotMinutes = 20;
        var result = DoctorSlotGenerator.Generate(Date, new[] { old, next, Shift(13, 15) }, Array.Empty<DoctorTimeOff>(), Array.Empty<Appointment>(), BeforeDate);
        Assert.Equal(10, result.Count);
        Assert.All(result.Where(s => s.StartTime.Hour < 10), s => Assert.Equal(20, (s.EndTime - s.StartTime).TotalMinutes));
    }

    [Fact]
    public void InvalidOrInactiveSchedules_AreIgnored()
    {
        var zero = Shift(); zero.SlotMinutes = 0;
        var inactive = Shift(); inactive.IsActive = false;
        var otherDay = Shift(); otherDay.DayOfWeek = 2;
        Assert.Empty(DoctorSlotGenerator.Generate(Date, new[] { zero, inactive, otherDay }, Array.Empty<DoctorTimeOff>(), Array.Empty<Appointment>(), BeforeDate));
    }

    [Fact]
    public void ClinicNow_UsesUtcPlusSevenIndependentOfServerTimeZone()
    {
        var clock = new Clock();
        var result = DoctorSlotGenerator.ClinicNow(clock);
        Assert.Equal(new DateTime(2026, 10, 13, 2, 0, 0), result);
        Assert.Equal(DateTimeKind.Unspecified, result.Kind);
    }

    [Fact]
    public async Task ExistingAvailableSlotsApi_UsesAllShiftsAndClinicWideLeave()
    {
        var date = DateOnly.FromDateTime(DoctorSlotGenerator.ClinicNow()).AddDays(7);
        var uow = new Mock<IUnitOfWork>();
        var doctors = new Mock<IDoctorRepository>();
        var schedules = new Mock<IDoctorScheduleRepository>();
        var leave = new Mock<IDoctorTimeOffRepository>();
        var appointments = new Mock<IAppointmentRepository>();
        uow.Setup(x => x.DoctorRepository).Returns(doctors.Object);
        uow.Setup(x => x.DoctorScheduleRepository).Returns(schedules.Object);
        uow.Setup(x => x.DoctorTimeOffRepository).Returns(leave.Object);
        uow.Setup(x => x.AppointmentRepository).Returns(appointments.Object);
        doctors.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new Doctor { Id = 1, IsActive = true });
        var shifts = new[] { Shift(), Shift(13, 15) }.ToList();
        foreach (var shift in shifts) { shift.DayOfWeek = (byte)date.DayOfWeek; shift.EffectiveFrom = date; }
        schedules.Setup(x => x.GetEffectiveAsync(1, date)).ReturnsAsync(shifts);
        var dayStart = date.ToDateTime(TimeOnly.MinValue); var dayEnd = dayStart.AddDays(1);
        leave.Setup(x => x.GetOverlappingAsync(1, dayStart, dayEnd)).ReturnsAsync(new List<DoctorTimeOff>());
        appointments.Setup(x => x.GetBlockingInRangeAsync(1, dayStart, dayEnd)).ReturnsAsync(new List<Appointment>());
        var service = new DoctorService(uow.Object, NullLogger<DoctorService>.Instance);
        Assert.Equal(8, (await service.GetAvailableSlotsAsync(1, date)).Content!.Count);
        leave.Setup(x => x.GetOverlappingAsync(1, dayStart, dayEnd)).ReturnsAsync(new List<DoctorTimeOff>
            { new() { StartAt = dayStart, EndAt = dayEnd, DoctorId = null } });
        var result = await service.GetAvailableSlotsAsync(1, date);
        Assert.Equal(200, result.StatusCode);
        Assert.Empty(result.Content!);
    }

    private sealed class Clock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 12, 19, 0, 0, TimeSpan.Zero);
    }
}
