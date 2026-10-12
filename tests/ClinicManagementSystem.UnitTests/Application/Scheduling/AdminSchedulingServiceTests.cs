using ClinicManagementSystem.Application.DTOs.Scheduling;
using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.Repositories;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ClinicManagementSystem.UnitTests.Application.AdminScheduling;

public class AdminSchedulingServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IDoctorScheduleRepository> _schedules = new();
    private readonly Mock<IDoctorTimeOffRepository> _leave = new();
    private readonly Mock<IDoctorRepository> _doctors = new();
    private readonly Mock<IAppointmentRepository> _appointments = new();
    private readonly Mock<IAuditLogRepository> _audit = new();
    private readonly Mock<IUserRoleRepository> _roles = new();
    private readonly Mock<INotificationRepository> _notices = new();
    private readonly List<DoctorSchedule> _existing = new();
    private readonly List<Appointment> _bookings = new();
    private readonly Doctor _doctor = new() { Id = 1, UserId = 10, FullName = "Bác sĩ test", IsActive = true };
    private readonly AdminSchedulingService _service;

    public AdminSchedulingServiceTests()
    {
        _uow.Setup(x => x.DoctorScheduleRepository).Returns(_schedules.Object);
        _uow.Setup(x => x.DoctorTimeOffRepository).Returns(_leave.Object);
        _uow.Setup(x => x.DoctorRepository).Returns(_doctors.Object);
        _uow.Setup(x => x.AppointmentRepository).Returns(_appointments.Object);
        _uow.Setup(x => x.AuditLogRepository).Returns(_audit.Object);
        _uow.Setup(x => x.UserRoleRepository).Returns(_roles.Object);
        _uow.Setup(x => x.NotificationRepository).Returns(_notices.Object);
        _doctors.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(_doctor);
        _schedules.Setup(x => x.GetForDoctorDayAsync(1, 1)).ReturnsAsync(_existing);
        _schedules.Setup(x => x.AddAsync(It.IsAny<DoctorSchedule>())).Callback<DoctorSchedule>(s => s.Id = 100).Returns(Task.CompletedTask);
        _leave.Setup(x => x.AddAsync(It.IsAny<DoctorTimeOff>())).Callback<DoctorTimeOff>(t => t.Id = 200).Returns(Task.CompletedTask);
        _appointments.Setup(x => x.GetUpcomingInRangeAsync(It.IsAny<int?>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync((int? doctorId, DateTime from, DateTime to) => _bookings.Where(a => (!doctorId.HasValue || a.DoctorId == doctorId)
                && a.EndTime > from && a.StartTime < to).ToList());
        _roles.Setup(x => x.GetActiveReceptionistUserIdsAsync()).ReturnsAsync(new List<int> { 20, 20, 10 });
        _service = new AdminSchedulingService(_uow.Object, NullLogger<AdminSchedulingService>.Instance, new FixedClock());
    }

    private static ScheduleRequestDTO Request() => new()
    {
        DoctorId = 1, DayOfWeek = 1, StartTime = new(8, 0), EndTime = new(17, 0),
        BreakStart = new(11, 30), BreakEnd = new(13, 30), SlotMinutes = 30,
        EffectiveFrom = new(2026, 10, 19), EffectiveTo = new(2026, 12, 31)
    };

    private DoctorSchedule Old(DateOnly? to = null)
    {
        var old = new DoctorSchedule
        {
            Id = 3, DoctorId = 1, Doctor = _doctor, DayOfWeek = 1, StartTime = new(8, 0), EndTime = new(17, 0),
            BreakStart = new(11, 30), BreakEnd = new(13, 30), SlotMinutes = 30,
            EffectiveFrom = new(2026, 10, 1), EffectiveTo = to, IsActive = true
        };
        _existing.Add(old);
        _schedules.Setup(x => x.GetByIdAsync(3)).ReturnsAsync(old);
        return old;
    }

    private Appointment Booking(DateTime? start = null, byte status = 0, int doctorId = 1)
    {
        var at = start ?? new DateTime(2026, 10, 26, 8, 0, 0);
        var booking = new Appointment
        {
            Id = 40 + _bookings.Count, DoctorId = doctorId, Doctor = _doctor,
            Patient = new Patient { FullName = "Bệnh nhân test" }, AppointmentCode = "AP_TEST",
            StartTime = at, EndTime = at.AddMinutes(30), Status = status
        };
        _bookings.Add(booking);
        return booking;
    }

    private static TimeOffRequestDTO LeaveRequest(int? doctorId = 1) => new()
    {
        DoctorId = doctorId, StartAt = new DateTime(2026, 10, 26, 8, 0, 0), EndAt = new DateTime(2026, 10, 26, 10, 0, 0),
        Type = 0, Reason = "  Việc gia đình  "
    };

    [Fact]
    public async Task CreateSchedule_PersistsAuditWithActorAndGeneratedId()
    {
        var result = await _service.SaveScheduleAsync(null, Request(), 7);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal(100, result.Content!.Schedule!.Id);
        _audit.Verify(x => x.AddAsync(It.Is<AuditLog>(a => a.UserId == 7 && a.EntityId == 100 && a.Action == "CreateDoctorSchedule")), Times.Once);
        _schedules.Verify(x => x.AcquireAdminLockAsync(), Times.Once);
        _uow.Verify(x => x.CommitTransactionAsync(), Times.Once);
    }

    [Theory]
    [InlineData("missing-day")]
    [InlineData("invalid-day")]
    [InlineData("missing-start")]
    [InlineData("reversed-time")]
    [InlineData("one-break")]
    [InlineData("outside-break")]
    [InlineData("slot-too-short")]
    [InlineData("slot-too-long")]
    [InlineData("missing-effective")]
    [InlineData("reversed-dates")]
    [InlineData("short-shift")]
    [InlineData("full-shift-break")]
    public async Task InvalidSchedule_IsRejectedBeforeTransaction(string test)
    {
        var request = Request();
        switch (test)
        {
            case "missing-day": request.DayOfWeek = null; break;
            case "invalid-day": request.DayOfWeek = 7; break;
            case "missing-start": request.StartTime = null; break;
            case "reversed-time": request.EndTime = new(7, 0); break;
            case "one-break": request.BreakEnd = null; break;
            case "outside-break": request.BreakEnd = new(18, 0); break;
            case "slot-too-short": request.SlotMinutes = 4; break;
            case "slot-too-long": request.SlotMinutes = 121; break;
            case "missing-effective": request.EffectiveFrom = null; break;
            case "reversed-dates": request.EffectiveTo = new(2026, 10, 18); break;
            case "short-shift": request.EndTime = new(8, 10); request.BreakStart = request.BreakEnd = null; break;
            case "full-shift-break": request.BreakStart = request.StartTime; request.BreakEnd = request.EndTime; break;
        }
        Assert.Equal(400, (await _service.SaveScheduleAsync(null, request, 7)).StatusCode);
        _uow.Verify(x => x.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task Schedule_OverlappingDateAndTime_IsRejectedWithoutWrite()
    {
        Old();
        Assert.Equal(409, (await _service.SaveScheduleAsync(null, Request(), 7)).StatusCode);
        _schedules.Verify(x => x.AddAsync(It.IsAny<DoctorSchedule>()), Times.Never);
        _uow.Verify(x => x.RollbackTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task Schedule_AdjacentShift_IsAllowed()
    {
        Old();
        var request = Request(); request.StartTime = new(17, 0); request.EndTime = new(19, 0); request.BreakStart = request.BreakEnd = null;
        Assert.Equal(201, (await _service.SaveScheduleAsync(null, request, 7)).StatusCode);
    }

    [Fact]
    public async Task Schedule_NonOverlappingEffectiveDates_IsAllowed()
    {
        Old(new(2026, 10, 18));
        Assert.Equal(201, (await _service.SaveScheduleAsync(null, Request(), 7)).StatusCode);
    }

    [Fact]
    public async Task Schedule_InactiveVersionWithSameUniqueKey_IsRejected()
    {
        var old = Old(); old.IsActive = false; old.EffectiveFrom = Request().EffectiveFrom!.Value;
        Assert.Equal(409, (await _service.SaveScheduleAsync(null, Request(), 7)).StatusCode);
    }

    [Fact]
    public async Task Update_LaterEffectiveDate_ClosesOldVersionAndCreatesNew()
    {
        var old = Old();
        var result = await _service.SaveScheduleAsync(3, Request(), 7);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal(new DateOnly(2026, 10, 18), old.EffectiveTo);
        Assert.Equal(new DateOnly(2026, 10, 1), old.EffectiveFrom);
        Assert.Equal(3, result.Content!.PreviousVersionId);
        Assert.Equal(100, result.Content.Schedule!.Id);
        _appointments.Verify(x => x.UpdateAsync(It.IsAny<Appointment>()), Times.Never);
    }

    [Fact]
    public async Task Update_SameFutureEffectiveDate_UpdatesSameRow()
    {
        var old = Old(); old.EffectiveFrom = Request().EffectiveFrom!.Value;
        var request = Request(); request.EndTime = new(18, 0);
        var result = await _service.SaveScheduleAsync(3, request, 7);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal(3, result.Content!.Schedule!.Id);
        Assert.Equal(new TimeOnly(18, 0), old.EndTime);
        _schedules.Verify(x => x.AddAsync(It.IsAny<DoctorSchedule>()), Times.Never);
    }

    [Fact]
    public async Task Update_CannotRewritePastOrChangeDoctorOrDay()
    {
        var old = Old();
        var request = Request(); request.EffectiveFrom = old.EffectiveFrom;
        Assert.Equal(409, (await _service.SaveScheduleAsync(3, request, 7)).StatusCode);
        request = Request(); request.DayOfWeek = 2;
        Assert.Equal(400, (await _service.SaveScheduleAsync(3, request, 7)).StatusCode);
        _uow.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task SlotChange_WithFutureBooking_PreviewsWithoutMutation()
    {
        var old = Old(); var booking = Booking();
        var request = Request(); request.SlotMinutes = 20;
        var result = await _service.SaveScheduleAsync(3, request, 7);
        Assert.Equal(409, result.StatusCode);
        Assert.True(result.Content!.RequiresConfirmation);
        Assert.Equal(booking.Id, Assert.Single(result.Content.AffectedAppointments).Id);
        Assert.Null(old.EffectiveTo);
        _uow.Verify(x => x.SaveChangesAsync(), Times.Never);
        _notices.Verify(x => x.AddListItemsAsync(It.IsAny<List<Notification>>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmedSlotChange_KeepsAppointmentAndNotifiesUniqueRecipients()
    {
        Old(); var booking = Booking(); var start = booking.StartTime;
        var request = Request(); request.SlotMinutes = 20; request.ConfirmAffectedAppointments = true;
        var result = await _service.SaveScheduleAsync(3, request, 7);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal(start, booking.StartTime);
        Assert.Equal(start.AddMinutes(30), booking.EndTime);
        Assert.Equal(2, result.Content!.NotificationsCreated);
        _notices.Verify(x => x.AddListItemsAsync(It.Is<List<Notification>>(n => n.Count == 2 && n.All(a => a.AppointmentId == booking.Id && a.Channel == "InApp"))), Times.Once);
        _appointments.Verify(x => x.UpdateAsync(It.IsAny<Appointment>()), Times.Never);
    }

    [Fact]
    public async Task ShorteningValidity_DetectsBookingsBeyondNewEndDate()
    {
        Old(); Booking(new DateTime(2026, 11, 2, 8, 0, 0));
        var request = Request(); request.EffectiveTo = new(2026, 10, 25);
        var result = await _service.SaveScheduleAsync(3, request, 7);
        Assert.Equal(409, result.StatusCode);
        Assert.Single(result.Content!.AffectedAppointments);
    }

    [Fact]
    public async Task BreakChange_DetectsBookingInsideNewBreak()
    {
        Old(); Booking(new DateTime(2026, 10, 26, 10, 0, 0));
        var request = Request(); request.BreakStart = new(10, 0);
        Assert.True((await _service.SaveScheduleAsync(3, request, 7)).Content!.RequiresConfirmation);
    }

    [Fact]
    public async Task UnchangedSlotAndValidBooking_DoesNotRequireConfirmation()
    {
        Old(); Booking();
        Assert.Equal(201, (await _service.SaveScheduleAsync(3, Request(), 7)).StatusCode);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    public async Task CancelledOrNoShowBookings_DoNotBlockChange(byte status)
    {
        Old(); Booking(status: status);
        var request = Request(); request.SlotMinutes = 20;
        Assert.Equal(201, (await _service.SaveScheduleAsync(3, request, 7)).StatusCode);
    }

    [Fact]
    public async Task MissingScheduleAndDoctorOrInactiveDoctor_ReturnExpectedErrors()
    {
        Assert.Equal(404, (await _service.SaveScheduleAsync(99, Request(), 7)).StatusCode);
        var request = Request(); request.DoctorId = 99;
        Assert.Equal(404, (await _service.SaveScheduleAsync(null, request, 7)).StatusCode);
        _doctor.IsActive = false;
        Assert.Equal(409, (await _service.SaveScheduleAsync(null, Request(), 7)).StatusCode);
    }

    [Fact]
    public async Task TimeOff_NoConflict_SavesActorAndTrimmedReason()
    {
        var result = await _service.CreateTimeOffAsync(LeaveRequest(), 7);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal(200, result.Content!.TimeOff!.Id);
        Assert.Equal(7, result.Content.TimeOff.ApprovedBy);
        Assert.Equal("Việc gia đình", result.Content.TimeOff.Reason);
        _audit.Verify(x => x.AddAsync(It.Is<AuditLog>(a => a.Action == "CreateDoctorTimeOff" && a.UserId == 7 && a.EntityId == 200)), Times.Once);
    }

    [Fact]
    public async Task ClinicWideLeave_DetectsBookingsAcrossDoctors_RequiresConfirmation()
    {
        Booking(); Booking(doctorId: 2);
        var result = await _service.CreateTimeOffAsync(LeaveRequest(null), 7);
        Assert.Equal(409, result.StatusCode);
        Assert.Null(result.Content!.TimeOff!.DoctorId);
        Assert.Equal(2, result.Content.AffectedAppointments.Count);
        _leave.Verify(x => x.AddAsync(It.IsAny<DoctorTimeOff>()), Times.Never);
    }

    [Fact]
    public async Task DoctorLeave_OnlyReportsThatDoctorsBookings()
    {
        Booking(); Booking(doctorId: 2);
        var result = await _service.CreateTimeOffAsync(LeaveRequest(), 7);
        Assert.Single(result.Content!.AffectedAppointments);
    }

    [Fact]
    public async Task ConfirmedTimeOff_NotifiesAndPreservesBookings()
    {
        var booking = Booking(); var request = LeaveRequest(); request.ConfirmAffectedAppointments = true;
        var result = await _service.CreateTimeOffAsync(request, 7);
        Assert.Equal(201, result.StatusCode);
        Assert.False(result.Content!.RequiresConfirmation);
        Assert.Equal(2, result.Content.NotificationsCreated);
        Assert.Equal((byte)0, booking.Status);
        _appointments.Verify(x => x.UpdateAsync(It.IsAny<Appointment>()), Times.Never);
    }

    [Fact]
    public async Task TimeOff_Overlap_IsRejectedEvenWhenConfirmed()
    {
        var request = LeaveRequest(); request.ConfirmAffectedAppointments = true;
        _leave.Setup(x => x.HasOverlapAsync(1, request.StartAt!.Value, request.EndAt!.Value)).ReturnsAsync(true);
        Assert.Equal(409, (await _service.CreateTimeOffAsync(request, 7)).StatusCode);
        _leave.Verify(x => x.AddAsync(It.IsAny<DoctorTimeOff>()), Times.Never);
    }

    [Theory]
    [InlineData("missing-start")]
    [InlineData("missing-type")]
    [InlineData("reversed-time")]
    [InlineData("invalid-type")]
    [InlineData("empty-reason")]
    [InlineData("long-reason")]
    [InlineData("half-full-day")]
    [InlineData("utc")]
    [InlineData("offset")]
    public async Task InvalidTimeOff_IsRejected(string test)
    {
        var request = LeaveRequest();
        switch (test)
        {
            case "missing-start": request.StartAt = null; break;
            case "missing-type": request.Type = null; break;
            case "reversed-time": request.EndAt = request.StartAt; break;
            case "invalid-type": request.Type = 3; break;
            case "empty-reason": request.Reason = " "; break;
            case "long-reason": request.Reason = new string('a', 256); break;
            case "half-full-day": request.IsFullDay = true; break;
            case "utc": request.StartAt = DateTime.SpecifyKind(request.StartAt!.Value, DateTimeKind.Utc); break;
            case "offset": request.StartAt = DateTime.SpecifyKind(request.StartAt!.Value, DateTimeKind.Local); break;
        }
        Assert.Equal(400, (await _service.CreateTimeOffAsync(request, 7)).StatusCode);
        _uow.Verify(x => x.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task FullDayLeave_UsesExclusiveEndAtMidnight()
    {
        var request = LeaveRequest(null); request.StartAt = new(2026, 10, 26); request.EndAt = new(2026, 10, 27); request.IsFullDay = true;
        Assert.Equal(201, (await _service.CreateTimeOffAsync(request, 7)).StatusCode);
    }

    [Fact]
    public async Task DeleteTimeOff_PreservesAuditAndAppointments()
    {
        _leave.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(new DoctorTimeOff { Id = 5, DoctorId = null });
        Assert.Equal(200, (await _service.DeleteTimeOffAsync(5, 7)).StatusCode);
        _leave.Verify(x => x.DeleteAsync(5), Times.Once);
        _audit.Verify(x => x.AddAsync(It.Is<AuditLog>(a => a.EntityId == 5 && a.Action == "DeleteDoctorTimeOff")), Times.Once);
        _appointments.Verify(x => x.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteMissingTimeOff_Returns404()
        => Assert.Equal(404, (await _service.DeleteTimeOffAsync(99, 7)).StatusCode);

    [Fact]
    public async Task AuditFailure_RollsBackAndReturnsSafeError()
    {
        _audit.Setup(x => x.AddAsync(It.IsAny<AuditLog>())).ThrowsAsync(new InvalidOperationException("private-db-detail"));
        var result = await _service.SaveScheduleAsync(null, Request(), 7);
        Assert.Equal(500, result.StatusCode);
        Assert.DoesNotContain("private-db-detail", result.Message);
        _uow.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        _uow.Verify(x => x.CommitTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task NotificationFailure_RollsBackWholeTimeOffChange()
    {
        Booking(); var request = LeaveRequest(); request.ConfirmAffectedAppointments = true;
        _notices.Setup(x => x.AddListItemsAsync(It.IsAny<List<Notification>>())).ThrowsAsync(new InvalidOperationException());
        Assert.Equal(500, (await _service.CreateTimeOffAsync(request, 7)).StatusCode);
        _uow.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        _uow.Verify(x => x.CommitTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task InvalidPagingAndActor_AreRejected()
    {
        Assert.Equal(400, (await _service.GetSchedulesAsync(new() { Page = 0 })).StatusCode);
        Assert.Equal(400, (await _service.GetTimeOffsAsync(new() { From = new(2026, 10, 20), To = new(2026, 10, 19) })).StatusCode);
        Assert.Equal(401, (await _service.SaveScheduleAsync(null, Request(), 0)).StatusCode);
        Assert.Equal(401, (await _service.CreateTimeOffAsync(LeaveRequest(), 0)).StatusCode);
        _uow.Verify(x => x.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task ListSchedules_PassesFiltersAndReturnsPagedDoctorNames()
    {
        var item = Old();
        var date = new DateOnly(2026, 10, 19);
        _schedules.Setup(x => x.GetAdminPageAsync(1, date, true, 2, 5)).ReturnsAsync((new List<DoctorSchedule> { item }, 12));
        var response = await _service.GetSchedulesAsync(new() { DoctorId = 1, Date = date, Active = true, Page = 2, PageSize = 5 });
        Assert.Equal(200, response.StatusCode);
        Assert.Equal(3, response.Content!.TotalPages);
        Assert.Equal(2, response.Content.Page);
        Assert.Equal(_doctor.FullName, Assert.Single(response.Content.Items).DoctorName);
    }

    [Fact]
    public async Task ListTimeOffs_ToFilterIncludesEntireLastDay()
    {
        _leave.Setup(x => x.GetAdminPageAsync(1, false, new DateTime(2026, 10, 19), new DateTime(2026, 10, 21), 1, 10))
            .ReturnsAsync((new List<DoctorTimeOff> { new() { Id = 5, DoctorId = null } }, 1));
        var response = await _service.GetTimeOffsAsync(new() { DoctorId = 1, From = new(2026, 10, 19), To = new(2026, 10, 20) });
        Assert.Equal(200, response.StatusCode);
        Assert.Equal("Cả phòng khám", Assert.Single(response.Content!.Items).DoctorName);
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 12, 2, 0, 0, TimeSpan.Zero);
    }
}
