using System.Text.Json;
using ClinicManagementSystem.Application.DTOs.Appointment;
using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.Repositories;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ClinicManagementSystem.UnitTests.Application.Reception;

public class ReceptionAffectedAppointmentServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IAppointmentRepository> _appointments = new();
    private readonly Mock<IDoctorScheduleRepository> _schedules = new();
    private readonly Mock<IDoctorTimeOffRepository> _leave = new();
    private readonly Mock<IAuditLogRepository> _audit = new();
    private readonly Mock<INotificationRepository> _notices = new();
    private readonly Mock<IAppointmentStatusHistoryRepository> _history = new();
    private readonly List<Appointment> _bookings = new();
    private readonly List<DoctorTimeOff> _timeOff = new();
    private bool _hasSchedule = true;
    private int _slotMinutes = 30;
    private readonly ReceptionAffectedAppointmentService _service;
    private static readonly DateTime Start = new(2026, 10, 19, 9, 0, 0);

    public ReceptionAffectedAppointmentServiceTests()
    {
        _uow.Setup(x => x.AppointmentRepository).Returns(_appointments.Object);
        _uow.Setup(x => x.DoctorScheduleRepository).Returns(_schedules.Object);
        _uow.Setup(x => x.DoctorTimeOffRepository).Returns(_leave.Object);
        _uow.Setup(x => x.AuditLogRepository).Returns(_audit.Object);
        _uow.Setup(x => x.NotificationRepository).Returns(_notices.Object);
        _uow.Setup(x => x.AppointmentStatusHistoryRepository).Returns(_history.Object);
        _appointments.Setup(x => x.GetUpcomingInRangeAsync(It.IsAny<int?>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync((int? doctor, DateTime from, DateTime to) => _bookings.Where(a => (!doctor.HasValue || a.DoctorId == doctor)
                && a.StartTime < to && a.EndTime > from && a.Status < 4).ToList());
        _appointments.Setup(x => x.GetReceptionDetailAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => _bookings.FirstOrDefault(a => a.Id == id));
        _appointments.Setup(x => x.GetBlockingInRangeAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync((int doctor, DateTime from, DateTime to) => _bookings.Where(a => a.DoctorId == doctor
                && a.StartTime < to && a.EndTime > from && a.Status < 4).ToList());
        _schedules.Setup(x => x.GetEffectiveAsync(It.IsAny<int>(), It.IsAny<DateOnly>()))
            .ReturnsAsync((int doctor, DateOnly date) => _hasSchedule ? new List<DoctorSchedule>
            {
                new() { Id = 1, DoctorId = doctor, DayOfWeek = (byte)date.DayOfWeek, StartTime = new(8, 0), EndTime = new(17, 0),
                    BreakStart = new(11, 30), BreakEnd = new(13, 30), SlotMinutes = _slotMinutes, EffectiveFrom = new(2026, 1, 1), IsActive = true }
            } : new());
        _leave.Setup(x => x.GetOverlappingAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync((int doctor, DateTime from, DateTime to) => _timeOff.Where(t => (t.DoctorId == null || t.DoctorId == doctor)
                && t.StartAt < to && t.EndAt > from).ToList());
        _timeOff.Add(new() { Id = 1, DoctorId = 1, StartAt = Start.Date, EndAt = Start.Date.AddDays(1), Reason = "Nghỉ" });
        _service = new(_uow.Object, NullLogger<ReceptionAffectedAppointmentService>.Instance, new FixedClock());
    }

    private Appointment Booking(byte status = 0, int doctorId = 1, DateTime? start = null)
    {
        var at = start ?? Start;
        var appointment = new Appointment
        {
            Id = _bookings.Count + 1, AppointmentCode = $"AP{_bookings.Count + 1}", DoctorId = doctorId,
            Doctor = new() { Id = doctorId, UserId = 10, FullName = "Bác sĩ test", IsActive = true },
            PatientId = 2, Patient = new() { Id = 2, UserId = 20, FullName = "Bệnh nhân test", Phone = "0909090909", IsActive = true },
            StartTime = at, EndTime = at.AddMinutes(30), Status = status, FeeSnapshot = 150000,
            CreatedAt = new(2026, 10, 1), CreatedBy = 20
        };
        _bookings.Add(appointment);
        return appointment;
    }
    private async Task<string> Version(int id)
        => (await _service.GetAffectedAsync(new() { From = new(2026, 10, 1), To = new(2026, 10, 31) })).Content!.Items.Single(a => a.Id == id).Version;
    private async Task<ReceptionRescheduleAppointmentRequestDTO> Move(Appointment appointment)
        => new() { ExpectedVersion = await Version(appointment.Id), NewStartTime = Start.AddDays(1), Reason = " Đã thống nhất với bệnh nhân " };

    [Fact]
    public async Task Reschedule_PreservesIdentityStatusAndFee_RecordsBeforeAfterAndNotifiesOwners()
    {
        var appointment = Booking(status: 1);
        var sent = new Notification { Status = 1, Title = "Lịch nghỉ ảnh hưởng lịch hẹn", Content = "Lịch sử", SentAt = Start.AddDays(-2) };
        var pending = new Notification { Status = 0, Title = "Nhắc lịch khám", Content = "Giờ cũ", ScheduledAt = Start.AddHours(-24) };
        appointment.Notifications.Add(sent); appointment.Notifications.Add(pending);
        var request = await Move(appointment);
        var result = await _service.RescheduleAsync(appointment.Id, request, 7);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal(appointment.Id, result.Content!.Id);
        Assert.Equal("AP1", result.Content.AppointmentCode);
        Assert.Equal(1, appointment.Status);
        Assert.Equal(request.NewStartTime, appointment.StartTime);
        Assert.Equal(150000, appointment.FeeSnapshot);
        Assert.Equal(20, appointment.CreatedBy);
        Assert.Equal(Start.AddDays(1).AddHours(-24), pending.ScheduledAt);
        Assert.Equal("Lịch sử", sent.Content);
        _audit.Verify(x => x.AddAsync(It.Is<AuditLog>(a => a.UserId == 7 && a.EntityId == 1 && a.Action == "ReceptionRescheduleAppointment"
            && a.Details!.Contains("2026-10-19T09:00:00") && a.Details.Contains("2026-10-20T09:00:00"))), Times.Once);
        _notices.Verify(x => x.AddListItemsAsync(It.Is<List<Notification>>(n => n.Count == 2
            && n.Any(t => t.UserId == 10) && n.Any(t => t.UserId == 20) && n.All(t => t.AppointmentId == 1 && t.Status == 1 && t.Channel == "InApp"))), Times.Once);
        _history.Verify(x => x.AddAsync(It.IsAny<AppointmentStatusHistory>()), Times.Never);
        _schedules.Verify(x => x.AcquireAdminLockAsync(), Times.Once);
        _uow.Verify(x => x.CommitTransactionAsync(), Times.Once);
        Assert.NotEqual(request.ExpectedVersion, result.Content.Version);
        Assert.Equal(409, (await _service.RescheduleAsync(appointment.Id, request, 8)).StatusCode);
        _notices.Verify(x => x.AddListItemsAsync(It.IsAny<List<Notification>>()), Times.Once);
        Assert.Empty((await _service.GetAffectedAsync(new() { From = new(2026, 10, 19), To = new(2026, 10, 20) })).Content!.Items);
    }

    [Fact]
    public async Task Cancel_RequiresReason_RecordsStatusHistoryAndSuppressesOnlyPendingReminder()
    {
        var appointment = Booking();
        var pending = new Notification { Status = 0, Title = "Nhắc lịch khám", ScheduledAt = Start.AddHours(-24) };
        var historyNotice = new Notification { Status = 1, Title = "Lịch nghỉ", Content = "Lịch sử" };
        appointment.Notifications.Add(pending); appointment.Notifications.Add(historyNotice);
        var result = await _service.CancelAsync(1, new() { ExpectedVersion = await Version(1), Reason = " Bệnh nhân đồng ý hủy " }, 7);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal(4, appointment.Status);
        Assert.Equal("Bệnh nhân đồng ý hủy", appointment.CancelReason);
        Assert.Equal(3, pending.Status);
        Assert.Equal(1, historyNotice.Status);
        _history.Verify(x => x.AddAsync(It.Is<AppointmentStatusHistory>(h => h.AppointmentId == 1 && h.FromStatus == 0
            && h.ToStatus == 4 && h.ChangedBy == 7 && h.Reason == "Bệnh nhân đồng ý hủy")), Times.Once);
        _audit.Verify(x => x.AddAsync(It.Is<AuditLog>(a => a.Action == "ReceptionCancelAppointment")), Times.Once);
        Assert.Empty((await _service.GetAffectedAsync(new())).Content!.Items);
    }

    [Theory]
    [InlineData("busy")]
    [InlineData("leave")]
    [InlineData("break")]
    [InlineData("off-grid")]
    [InlineData("inactive-doctor")]
    [InlineData("inactive-patient")]
    public async Task InvalidNewSlot_IsConflictWithoutWrites(string cause)
    {
        var appointment = Booking(); var request = await Move(appointment);
        switch (cause)
        {
            case "busy": Booking(start: request.NewStartTime); break;
            case "leave": _timeOff.Add(new() { DoctorId = null, StartAt = Start.AddDays(1), EndAt = Start.AddDays(1).AddHours(1) }); break;
            case "break": request.NewStartTime = Start.AddDays(1).Date.AddHours(12); break;
            case "off-grid": request.NewStartTime = Start.AddDays(1).AddMinutes(5); break;
            case "inactive-doctor": appointment.Doctor.IsActive = false; break;
            case "inactive-patient": appointment.Patient.IsActive = false; break;
        }
        Assert.Equal(409, (await _service.RescheduleAsync(1, request, 7)).StatusCode);
        Assert.Equal(Start, appointment.StartTime);
        _uow.Verify(x => x.SaveChangesAsync(), Times.Never);
        _uow.Verify(x => x.RollbackTransactionAsync(), Times.Once);
    }

    [Theory]
    [InlineData("version")]
    [InlineData("changed-status")]
    [InlineData("changed-time")]
    [InlineData("no-longer-affected")]
    [InlineData("paid")]
    public async Task StaleOrUnsafeCancellation_IsConflictWithoutWrites(string cause)
    {
        var appointment = Booking();
        var request = new ReceptionCancelAppointmentRequestDTO { ExpectedVersion = await Version(1), Reason = "Đã liên hệ" };
        switch (cause)
        {
            case "version": request.ExpectedVersion = new('0', 64); break;
            case "changed-status": appointment.Status = 4; break;
            case "changed-time": appointment.StartTime = appointment.StartTime.AddMinutes(30); break;
            case "no-longer-affected": _timeOff.Clear(); break;
            case "paid": appointment.Invoices.Add(new() { PaidAmount = 100000 }); break;
        }
        Assert.Equal(409, (await _service.CancelAsync(1, request, 7)).StatusCode);
        _uow.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task UnsupportedStatus_CannotProcess(byte status)
    {
        var appointment = Booking(); var request = await Move(appointment);
        appointment.Status = status;
        // Return a fresh version where available so status checks are tested separately from stale-form checks.
        if (status == 2) request.ExpectedVersion = await Version(1);
        Assert.Equal(409, (await _service.RescheduleAsync(1, request, 7)).StatusCode);
        _uow.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task OverdueAndCheckedIn_RemainVisibleButCannotProcess()
    {
        Booking(status: 2);
        var overdue = Booking(start: new(2026, 10, 11, 9, 0, 0));
        _timeOff.Add(new() { DoctorId = 1, StartAt = overdue.StartTime.Date, EndAt = overdue.StartTime.Date.AddDays(1) });
        var result = await _service.GetAffectedAsync(new());
        Assert.Equal(2, result.Content!.TotalItems);
        Assert.All(result.Content.Items, a => { Assert.False(a.CanProcess); Assert.NotEmpty(a.CannotProcessReason!); });
        Assert.Equal(409, (await _service.CancelAsync(overdue.Id, new() { ExpectedVersion = await Version(overdue.Id), Reason = "Quá giờ" }, 7)).StatusCode);
    }

    [Fact]
    public async Task CurrentConflicts_AreDistinctAndPaginated_AfterFiltering()
    {
        Booking(); Booking(start: Start.AddMinutes(30)); Booking(doctorId: 2); Booking(status: 3);
        _timeOff.Add(new() { DoctorId = null, StartAt = Start, EndAt = Start.AddHours(2) });
        var result = await _service.GetAffectedAsync(new() { DoctorId = 1, PageSize = 1, Page = 2 });
        Assert.Equal(2, result.Content!.TotalItems);
        Assert.Equal(2, result.Content.TotalPages);
        Assert.Equal(2, Assert.Single(result.Content.Items).Id);
    }

    [Theory]
    [InlineData("removed-leave")]
    [InlineData("restored-schedule")]
    public async Task HistoricalNotices_DoNotKeepResolvedAppointmentsInQueue(string scenario)
    {
        var appointment = Booking();
        appointment.Notifications.Add(new() { Title = "Lịch nghỉ", Status = 1 });
        _timeOff.Clear();
        if (scenario == "restored-schedule")
        {
            _hasSchedule = false;
            Assert.Single((await _service.GetAffectedAsync(new())).Content!.Items);
            _hasSchedule = true;
        }
        Assert.Empty((await _service.GetAffectedAsync(new())).Content!.Items);
        Assert.Single(appointment.Notifications);
    }

    [Fact]
    public async Task ChangedSlotDuration_IsAffectedEvenWithoutLeave()
    {
        Booking(); _timeOff.Clear(); _slotMinutes = 60;
        var item = Assert.Single((await _service.GetAffectedAsync(new())).Content!.Items);
        Assert.Contains(item.AffectedReasons, r => r.Contains("thời lượng slot"));
    }

    [Theory]
    [InlineData("actor")]
    [InlineData("reason")]
    [InlineData("missing-time")]
    [InlineData("past-time")]
    [InlineData("utc-time")]
    public async Task InvalidRequest_DoesNotStartTransaction(string cause)
    {
        Booking(); var request = await Move(_bookings[0]); var actor = 7;
        switch (cause)
        {
            case "actor": actor = 0; break;
            case "reason": request.Reason = " "; break;
            case "missing-time": request.NewStartTime = null; break;
            case "past-time": request.NewStartTime = new(2026, 10, 1); break;
            case "utc-time": request.NewStartTime = DateTime.SpecifyKind(Start.AddDays(1), DateTimeKind.Utc); break;
        }
        Assert.Equal(cause == "actor" ? 401 : 400, (await _service.RescheduleAsync(1, request, actor)).StatusCode);
        _uow.Verify(x => x.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task MissingAppointment_ReturnsNotFound()
    {
        Assert.Equal(404, (await _service.CancelAsync(99, new() { ExpectedVersion = new('0', 64), Reason = "Test" }, 7)).StatusCode);
        _uow.Verify(x => x.RollbackTransactionAsync(), Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SaveFailure_RollsBackAuditNotificationsAndAppointment(bool concurrency)
    {
        var appointment = Booking(); var request = await Move(appointment);
        _uow.Setup(x => x.SaveChangesAsync()).ThrowsAsync(concurrency ? new DbUpdateConcurrencyException() : new InvalidOperationException());
        Assert.Equal(concurrency ? 409 : 500, (await _service.RescheduleAsync(1, request, 7)).StatusCode);
        _uow.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        _uow.Verify(x => x.CommitTransactionAsync(), Times.Never);
    }

    [Theory]
    [InlineData("page")]
    [InlineData("date")]
    [InlineData("wide")]
    [InlineData("overflow")]
    public async Task InvalidQuery_IsRejectedBeforeDatabase(string cause)
    {
        var query = new ReceptionAffectedQueryDTO();
        switch (cause)
        {
            case "page": query.Page = 0; break;
            case "date": query.From = new(2026, 10, 20); query.To = new(2026, 10, 19); break;
            case "wide": query.From = new(2026, 1, 1); query.To = new(2026, 10, 1); break;
            case "overflow": query.From = DateOnly.MaxValue; break;
        }
        Assert.Equal(400, (await _service.GetAffectedAsync(query)).StatusCode);
        _appointments.Verify(x => x.GetUpcomingInRangeAsync(It.IsAny<int?>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public void AppointmentConcurrency_UsesExistingColumnsWithoutSchemaChanges()
    {
        using var context = new ClinicManagementDbContext(new DbContextOptionsBuilder<ClinicManagementDbContext>()
            .UseSqlServer("Server=localhost;Database=UnusedMetadataOnly;Integrated Security=true").Options);
        var entity = context.Model.FindEntityType(typeof(Appointment))!;
        Assert.All(new[] { nameof(Appointment.Status), nameof(Appointment.StartTime), nameof(Appointment.EndTime), nameof(Appointment.UpdatedAt) },
            name => Assert.True(entity.FindProperty(name)!.IsConcurrencyToken));
        Assert.Null(entity.FindProperty("RowVersion"));
    }

    private sealed class FixedClock : TimeProvider
    { public override DateTimeOffset GetUtcNow() => new(2026, 10, 12, 0, 0, 0, TimeSpan.Zero); }
}
