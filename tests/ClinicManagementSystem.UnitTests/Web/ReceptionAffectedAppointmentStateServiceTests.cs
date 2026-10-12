using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Appointment;
using ClinicManagementSystem.Application.DTOs.Doctor;
using ClinicManagementSystem.Application.DTOs.Notification;
using ClinicManagementSystem.Application.DTOs.Specialty;
using ClinicManagementSystem.Web.Services;
using Moq;

namespace ClinicManagementSystem.UnitTests.Web;

public class ReceptionAffectedAppointmentStateServiceTests
{
    [Fact]
    public async Task Reschedule_SendsReviewedVersionAndWallTime_UsingOnlyAvailableSlots()
    {
        var handler = new ReceptionUiHandler();
        handler.Responses.Enqueue(ReceptionUiFixture.Envelope(new List<SlotDTO>
        {
            new() { StartTime = ReceptionUiFixture.OldTime, IsAvailable = true },
            new() { StartTime = ReceptionUiFixture.NewTime, IsAvailable = true },
            new() { StartTime = ReceptionUiFixture.NewTime.AddMinutes(30), IsAvailable = false }
        }));
        handler.Responses.Enqueue(ReceptionUiFixture.Envelope(ReceptionUiFixture.Appointment(), message: "Đã đổi lịch hẹn."));
        var state = ReceptionUiFixture.CreateState(handler);
        var original = ReceptionUiFixture.Appointment();
        Assert.True(state.OpenEditor(original));
        original.Version = new('B', 64);
        original.Id = 999;
        await state.LoadSlotsAsync(new(2026, 10, 20));
        Assert.Equal(ReceptionUiFixture.NewTime, Assert.Single(state.Slots).StartTime);
        Assert.True(await state.RescheduleAsync(ReceptionUiFixture.NewTime, "  Bệnh nhân đồng ý  "));

        Assert.Equal(HttpMethod.Patch, handler.Methods.Last());
        Assert.EndsWith("/api/reception/appointments/7/reschedule", handler.Uris.Last());
        using var json = JsonDocument.Parse(handler.Bodies.Last());
        Assert.Equal(new string('A', 64), json.RootElement.GetProperty("expectedVersion").GetString());
        Assert.Equal("2026-10-20T09:00:00", json.RootElement.GetProperty("newStartTime").GetString());
        Assert.Equal("Bệnh nhân đồng ý", json.RootElement.GetProperty("reason").GetString());
        Assert.Null(state.Target);
        Assert.Empty(state.Slots);
        Assert.Equal("Đã đổi lịch hẹn.", state.ActionMessage);
    }

    [Fact]
    public async Task Cancel_UsesSeparateReceptionRouteAndVersion_NoPatientOrDoctorMutation()
    {
        var handler = new ReceptionUiHandler();
        handler.Responses.Enqueue(ReceptionUiFixture.Envelope(ReceptionUiFixture.Appointment()));
        var state = ReceptionUiFixture.CreateState(handler);
        state.OpenEditor(ReceptionUiFixture.Appointment());
        Assert.True(await state.CancelAsync("  Đã thống nhất hủy  "));
        Assert.EndsWith("/api/reception/appointments/7/cancel", Assert.Single(handler.Uris));
        using var json = JsonDocument.Parse(Assert.Single(handler.Bodies));
        Assert.Equal(2, json.RootElement.EnumerateObject().Count());
        Assert.Equal("Đã thống nhất hủy", json.RootElement.GetProperty("reason").GetString());
        Assert.Equal(new string('A', 64), json.RootElement.GetProperty("expectedVersion").GetString());
    }

    [Theory]
    [InlineData(409)]
    [InlineData(404)]
    public async Task ConflictOrRemovedAppointment_BlocksRetryUntilEditorIsReopened(int status)
    {
        var handler = new ReceptionUiHandler();
        handler.Responses.Enqueue(ReceptionUiFixture.Envelope<ReceptionAffectedAppointmentDTO>(null, status, "Lịch đã thay đổi."));
        var state = ReceptionUiFixture.CreateState(handler);
        state.OpenEditor(ReceptionUiFixture.Appointment());
        Assert.False(await state.CancelAsync("Bệnh nhân đồng ý"));
        Assert.True(state.NeedsReload);
        Assert.False(await state.CancelAsync("Thử lại"));
        Assert.Single(handler.Uris);
        Assert.Equal("Lịch đã thay đổi.", state.ActionError);
        Assert.Empty(state.ActionMessage);
        state.CloseEditor();
        var fresh = ReceptionUiFixture.Appointment(); fresh.Version = new('C', 64);
        Assert.True(state.OpenEditor(fresh));
        Assert.False(state.NeedsReload);
        Assert.Equal(new string('C', 64), state.Target!.Version);
    }

    [Fact]
    public async Task UnknownSlot_DoesNotSubmitArbitraryTime()
    {
        var handler = new ReceptionUiHandler();
        var state = ReceptionUiFixture.CreateState(handler);
        state.OpenEditor(ReceptionUiFixture.Appointment());
        Assert.False(await state.RescheduleAsync(ReceptionUiFixture.NewTime, "Đổi ngày"));
        Assert.Empty(handler.Uris);
        Assert.Contains("giờ khám còn trống", state.ActionError);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(501)]
    public async Task InvalidReason_DoesNotSendMutation(int length)
    {
        var handler = new ReceptionUiHandler();
        var state = ReceptionUiFixture.CreateState(handler);
        state.OpenEditor(ReceptionUiFixture.Appointment());
        Assert.False(await state.CancelAsync(length == 0 ? "   " : new('x', length)));
        Assert.Empty(handler.Uris);
        Assert.Contains("500", state.ActionError);
    }

    [Fact]
    public void CheckedInReadonly_DoesNotOpenEditor()
    {
        var state = ReceptionUiFixture.CreateState(new ReceptionUiHandler());
        var item = ReceptionUiFixture.Appointment(); item.CanProcess = false; item.Status = 2;
        Assert.False(state.OpenEditor(item));
        Assert.Null(state.Target);
    }

    [Fact]
    public async Task ValidationProblem_KeepsFormAndDisplaysServerErrors()
    {
        var handler = new ReceptionUiHandler();
        handler.Responses.Enqueue(new(HttpStatusCode.BadRequest) { Content = JsonContent.Create(new { errors = new { Reason = new[] { "Lý do không hợp lệ." } } }) });
        var state = ReceptionUiFixture.CreateState(handler);
        state.OpenEditor(ReceptionUiFixture.Appointment());
        Assert.False(await state.CancelAsync("Lý do mẫu"));
        Assert.Equal("Lý do không hợp lệ.", state.ActionError);
        Assert.NotNull(state.Target);
        Assert.False(state.NeedsReload);
        Assert.False(state.IsSubmitting);
    }

    [Fact]
    public async Task DoubleSubmit_SendsOnePatchAndKeepsBusyUntilCompletion()
    {
        var handler = new ReceptionUiHandler { Hold = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        handler.Responses.Enqueue(ReceptionUiFixture.Envelope(ReceptionUiFixture.Appointment()));
        var state = ReceptionUiFixture.CreateState(handler);
        state.OpenEditor(ReceptionUiFixture.Appointment());
        var first = state.CancelAsync("Đã liên hệ");
        Assert.True(state.IsSubmitting);
        Assert.False(await state.CancelAsync("Đã liên hệ"));
        state.CloseEditor();
        Assert.NotNull(state.Target);
        handler.Hold.SetResult(true);
        Assert.True(await first);
        Assert.Single(handler.Uris);
        Assert.False(state.IsBusy);
    }

    [Fact]
    public async Task NetworkFailure_DoesNotClaimSuccess_AndKeepsReviewedVersion()
    {
        var handler = new ReceptionUiHandler { ThrowNetwork = true };
        var state = ReceptionUiFixture.CreateState(handler);
        state.OpenEditor(ReceptionUiFixture.Appointment());
        Assert.False(await state.CancelAsync("Đã liên hệ"));
        Assert.Contains("Tải lại danh sách", state.ActionError);
        Assert.Empty(state.ActionMessage);
        Assert.Equal(new string('A', 64), state.Target!.Version);
        Assert.False(state.IsSubmitting);
    }

    [Fact]
    public async Task ReloadFailure_ClearsStaleActionableRows_AndReleasesLoadingFlag()
    {
        var handler = new ReceptionUiHandler();
        handler.Responses.Enqueue(ReceptionUiFixture.Envelope(new PagedResult<ReceptionAffectedAppointmentDTO> { Items = [ReceptionUiFixture.Appointment()] }));
        handler.Responses.Enqueue(new(HttpStatusCode.Forbidden));
        var state = ReceptionUiFixture.CreateState(handler);
        await state.LoadAppointmentsAsync(new());
        await state.LoadAppointmentsAsync(new());
        Assert.Empty(state.Appointments.Items);
        Assert.Contains("không có quyền", state.ListError);
        Assert.False(state.IsLoading);
    }

    [Fact]
    public async Task Filters_SendAppliedDatesDoctorAndPage()
    {
        var handler = new ReceptionUiHandler();
        handler.Responses.Enqueue(ReceptionUiFixture.Envelope(new PagedResult<ReceptionAffectedAppointmentDTO> { Page = 2, PageSize = 10 }));
        var state = ReceptionUiFixture.CreateState(handler);
        await state.LoadAppointmentsAsync(new() { DoctorId = 5, From = new(2026, 10, 19), To = new(2026, 10, 20), Page = 2 });
        Assert.EndsWith("/api/reception/appointments/affected?page=2&pageSize=10&from=2026-10-19&to=2026-10-20&doctorId=5", Assert.Single(handler.Uris));
    }

    [Fact]
    public async Task DoctorOptions_ReuseAllowedApisAndDeduplicate()
    {
        var handler = new ReceptionUiHandler();
        handler.Responses.Enqueue(ReceptionUiFixture.Envelope(new List<SpecialtyDTO> { new() { Id = 1 }, new() { Id = 2 } }));
        handler.Responses.Enqueue(ReceptionUiFixture.Envelope(new List<DoctorDTO> { new() { Id = 5, FullName = "B" } }));
        handler.Responses.Enqueue(ReceptionUiFixture.Envelope(new List<DoctorDTO> { new() { Id = 5, FullName = "B" }, new() { Id = 6, FullName = "A" } }));
        var state = ReceptionUiFixture.CreateState(handler);
        await state.LoadDoctorsAsync();
        Assert.Equal(new[] { 6, 5 }, state.Doctors.Select(d => d.Id));
        Assert.DoesNotContain(handler.Uris, u => u.Contains("/api/admin"));
        Assert.EndsWith("/api/doctors?specialtyId=2", handler.Uris.Last());
    }

    [Fact]
    public async Task InactiveDoctorFromQueue_RemainsAFilterOptionAfterEmptyReload()
    {
        var handler = new ReceptionUiHandler();
        handler.Responses.Enqueue(ReceptionUiFixture.Envelope(new PagedResult<ReceptionAffectedAppointmentDTO> { Items = [ReceptionUiFixture.Appointment()] }));
        handler.Responses.Enqueue(ReceptionUiFixture.Envelope(new PagedResult<ReceptionAffectedAppointmentDTO>()));
        var state = ReceptionUiFixture.CreateState(handler);
        await state.LoadAppointmentsAsync(new());
        await state.LoadAppointmentsAsync(new() { DoctorId = 5 });
        Assert.Empty(state.Appointments.Items);
        Assert.Equal("Bác sĩ mẫu", Assert.Single(state.Doctors).FullName);
    }

    [Fact]
    public async Task NewDateFailure_ClearsPreviousSlotsSoOldDayCannotBeSubmitted()
    {
        var handler = new ReceptionUiHandler();
        handler.Responses.Enqueue(ReceptionUiFixture.Envelope(new List<SlotDTO> { new() { StartTime = ReceptionUiFixture.NewTime, IsAvailable = true } }));
        handler.Responses.Enqueue(new(HttpStatusCode.InternalServerError));
        var state = ReceptionUiFixture.CreateState(handler);
        state.OpenEditor(ReceptionUiFixture.Appointment());
        await state.LoadSlotsAsync(new(2026, 10, 20));
        await state.LoadSlotsAsync(new(2026, 10, 21));
        Assert.Empty(state.Slots);
        Assert.False(await state.RescheduleAsync(ReceptionUiFixture.NewTime, "Đổi lịch"));
        Assert.Equal(2, handler.Uris.Count);
        Assert.False(state.IsLoadingSlots);
    }

    [Fact]
    public async Task Notifications_UseCurrentUserEndpointAndSortNewestFirst()
    {
        var handler = new ReceptionUiHandler();
        handler.Responses.Enqueue(ReceptionUiFixture.Envelope(new List<NotificationDTO>
        { new() { Id = 1, CreatedAt = new(2026, 10, 19) }, new() { Id = 2, CreatedAt = new(2026, 10, 20) } }));
        var state = ReceptionUiFixture.CreateState(handler);
        await state.LoadNotificationsAsync();
        Assert.EndsWith("/api/notifications", Assert.Single(handler.Uris));
        Assert.Equal(new[] { 2, 1 }, state.Notifications.Select(n => n.Id));
    }

    [Fact]
    public async Task MalformedNotificationResponse_AllowsRetryWithoutFalseEmptySuccess()
    {
        var handler = new ReceptionUiHandler();
        handler.Responses.Enqueue(new(HttpStatusCode.OK) { Content = new StringContent("<html>gateway error</html>") });
        handler.Responses.Enqueue(ReceptionUiFixture.Envelope(new List<NotificationDTO> { new() { Id = 1 } }));
        var state = ReceptionUiFixture.CreateState(handler);
        await state.LoadNotificationsAsync();
        Assert.Contains("không hợp lệ", state.NotificationsError);
        Assert.False(state.IsLoadingNotifications);
        await state.LoadNotificationsAsync();
        Assert.Empty(state.NotificationsError);
        Assert.Single(state.Notifications);
    }
}

internal static class ReceptionUiFixture
{
    internal static DateTime OldTime => new(2026, 10, 19, 8, 0, 0);
    internal static DateTime NewTime => new(2026, 10, 20, 9, 0, 0);
    internal static ReceptionAffectedAppointmentDTO Appointment() => new()
    {
        Id = 7, AppointmentCode = "APT-TEST", DoctorId = 5, DoctorName = "Bác sĩ mẫu",
        PatientName = "Bệnh nhân mẫu", PatientPhone = "0909090909", StartTime = OldTime, EndTime = OldTime.AddMinutes(30),
        Status = 1, CanProcess = true, AffectedReasons = ["Bác sĩ đăng ký nghỉ."], Version = new('A', 64)
    };
    internal static HttpResponseMessage Envelope<T>(T? value, int status = 200, string message = "Thành công") => new((HttpStatusCode)status)
    { Content = JsonContent.Create(new HttpResponseData<T> { StatusCode = status, Message = message, Content = value }) };
    internal static ReceptionAffectedAppointmentStateService CreateState(HttpMessageHandler handler)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(handler, false) { BaseAddress = new("http://fixture.local") });
        var user = new UserStateService(null!, factory.Object, null!, new CustomAuthenticationStateProvider());
        typeof(UserStateService).GetProperty(nameof(UserStateService.AccessToken))!.SetValue(user, "fixture-token");
        return new(new AuthorizedApiService(factory.Object, user));
    }
}

internal sealed class ReceptionUiHandler : HttpMessageHandler
{
    internal Queue<HttpResponseMessage> Responses { get; } = new();
    internal List<string> Uris { get; } = new();
    internal List<HttpMethod> Methods { get; } = new();
    internal List<string> Bodies { get; } = new();
    internal bool ThrowNetwork { get; set; }
    internal TaskCompletionSource<bool>? Hold { get; set; }
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        Uris.Add(request.RequestUri!.ToString()); Methods.Add(request.Method);
        Bodies.Add(request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync(token));
        if (Hold != null) await Hold.Task;
        if (ThrowNetwork) throw new HttpRequestException("Fixture offline");
        return Responses.Dequeue();
    }
}
