using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Scheduling;
using ClinicManagementSystem.Web.Services;
using Moq;

namespace ClinicManagementSystem.UnitTests.Web;

public class AdminSchedulingStateServiceTests
{
    [Fact]
    public async Task SchedulePreview_ConfirmsExactSnapshot_AndKeepsOriginalFormUnconfirmed()
    {
        var handler = new RecordingHandler();
        handler.Responses.Enqueue(Envelope(409, new ScheduleChangeResultDTO { RequiresConfirmation = true, AffectedAppointments = [new() { Id = 7 }] }));
        handler.Responses.Enqueue(Envelope(201, new ScheduleChangeResultDTO { Schedule = new() { Id = 9 } }));
        var state = CreateState(handler);
        var request = Schedule();

        Assert.False(await state.SaveScheduleAsync(4, request));
        Assert.True(state.RequiresConfirmation);
        Assert.True(state.IsScheduleConfirmation);
        Assert.Equal(7, Assert.Single(state.AffectedAppointments).Id);
        request.SlotMinutes = 60;
        Assert.True(await state.ConfirmAsync());

        var sent = JsonSerializer.Deserialize<ScheduleRequestDTO>(handler.Bodies[1], new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Equal(30, sent.SlotMinutes);
        Assert.True(sent.ConfirmAffectedAppointments);
        Assert.False(request.ConfirmAffectedAppointments);
        Assert.Equal(HttpMethod.Put, handler.Methods[1]);
        Assert.EndsWith("/api/admin/doctor-schedules/4", handler.Uris[1]);
        Assert.False(state.RequiresConfirmation);
        Assert.Empty(state.AffectedAppointments);
        Assert.False(state.IsSubmitting);
    }

    [Fact]
    public async Task ClinicTimeOffPreview_ConfirmsNullDoctorAndWallTime_WithoutTimeZoneSuffix()
    {
        var handler = new RecordingHandler();
        handler.Responses.Enqueue(Envelope(409, new TimeOffChangeResultDTO { RequiresConfirmation = true, AffectedAppointments = [new() { Id = 8 }] }));
        handler.Responses.Enqueue(Envelope(201, new TimeOffChangeResultDTO { TimeOff = new() { Id = 2 } }));
        var state = CreateState(handler);
        var request = new TimeOffRequestDTO { DoctorId = null, StartAt = new(2026, 10, 26, 0, 0, 0), EndAt = new(2026, 10, 27, 0, 0, 0), Type = 1, Reason = "Nghỉ lễ", IsFullDay = true };

        Assert.False(await state.CreateTimeOffAsync(request));
        request.Reason = "Changed form";
        Assert.True(await state.ConfirmAsync());

        using var json = JsonDocument.Parse(handler.Bodies[1]);
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("doctorId").ValueKind);
        Assert.Equal("2026-10-26T00:00:00", json.RootElement.GetProperty("startAt").GetString());
        Assert.Equal("Nghỉ lễ", json.RootElement.GetProperty("reason").GetString());
        Assert.True(json.RootElement.GetProperty("confirmAffectedAppointments").GetBoolean());
        Assert.EndsWith("/api/admin/time-off", handler.Uris[1]);
    }

    [Fact]
    public async Task CancelPreview_DoesNotSendConfirmation()
    {
        var handler = new RecordingHandler();
        handler.Responses.Enqueue(Envelope(409, new ScheduleChangeResultDTO { RequiresConfirmation = true, AffectedAppointments = [new() { Id = 1 }] }));
        var state = CreateState(handler);
        await state.SaveScheduleAsync(null, Schedule());
        state.CancelConfirmation();
        Assert.False(await state.ConfirmAsync());
        Assert.Single(handler.Uris);
        Assert.Empty(state.AffectedAppointments);
    }

    [Fact]
    public async Task DuplicateConflict_DoesNotOfferConfirmation()
    {
        var handler = new RecordingHandler();
        handler.Responses.Enqueue(Envelope<ScheduleChangeResultDTO>(409, null, "Ca làm bị trùng."));
        var state = CreateState(handler);
        Assert.False(await state.SaveScheduleAsync(null, Schedule()));
        Assert.False(state.RequiresConfirmation);
        Assert.Equal("Ca làm bị trùng.", state.ActionError);
        Assert.Single(handler.Uris);
    }

    [Fact]
    public async Task ConfirmNetworkFailure_KeepsPreviewForRetry()
    {
        var handler = new RecordingHandler();
        handler.Responses.Enqueue(Envelope(409, new ScheduleChangeResultDTO { RequiresConfirmation = true, AffectedAppointments = [new() { Id = 1 }] }));
        var state = CreateState(handler);
        await state.SaveScheduleAsync(null, Schedule());
        handler.ThrowNetwork = true;
        Assert.False(await state.ConfirmAsync());
        Assert.True(state.RequiresConfirmation);
        Assert.Contains("kết nối", state.ActionError);
        Assert.False(state.IsSubmitting);
    }

    [Fact]
    public async Task ValidationProblem_ShowsFieldErrorsAndDoesNotReportSuccess()
    {
        var handler = new RecordingHandler();
        handler.Responses.Enqueue(new(HttpStatusCode.BadRequest) { Content = JsonContent.Create(new { errors = new { SlotMinutes = new[] { "Slot phải từ 5 đến 120." } } }) });
        var state = CreateState(handler);
        Assert.False(await state.SaveScheduleAsync(null, Schedule()));
        Assert.Equal("Slot phải từ 5 đến 120.", state.ActionError);
        Assert.Empty(state.ActionMessage);
    }

    [Fact]
    public async Task Filters_SendDatesAndKeepBothListsIndependent()
    {
        var handler = new RecordingHandler();
        handler.Responses.Enqueue(Envelope(200, new PagedResult<AdminDoctorScheduleDTO> { Items = [new() { Id = 3 }], Page = 2, PageSize = 10, TotalItems = 11, TotalPages = 2 }));
        handler.Responses.Enqueue(Envelope(200, new PagedResult<AdminDoctorTimeOffDTO> { Items = [new() { Id = 6 }], Page = 1 }));
        var state = CreateState(handler);
        await state.LoadSchedulesAsync(new() { DoctorId = 12, Date = new(2026, 10, 19), Active = true, Page = 2 });
        await state.LoadTimeOffsAsync(new() { ClinicWideOnly = true, From = new(2026, 10, 19), To = new(2026, 10, 26) });
        Assert.Contains("doctorId=12&date=2026-10-19&active=true", handler.Uris[0]);
        Assert.Contains("page=2", handler.Uris[0]);
        Assert.Contains("clinicWideOnly=true&from=2026-10-19&to=2026-10-26", handler.Uris[1]);
        Assert.DoesNotContain("doctorId", handler.Uris[1]);
        Assert.Equal(3, Assert.Single(state.Schedules.Items).Id);
        Assert.Equal(6, Assert.Single(state.TimeOffs.Items).Id);
    }

    [Fact]
    public async Task FailedReload_ClearsStaleRowsAndLoadingFlag()
    {
        var handler = new RecordingHandler();
        handler.Responses.Enqueue(Envelope(200, new PagedResult<AdminDoctorScheduleDTO> { Items = [new() { Id = 3 }] }));
        handler.Responses.Enqueue(new(HttpStatusCode.Forbidden));
        var state = CreateState(handler);
        await state.LoadSchedulesAsync(new());
        await state.LoadSchedulesAsync(new());
        Assert.Empty(state.Schedules.Items);
        Assert.Contains("không có quyền", state.SchedulesError);
        Assert.False(state.IsLoadingSchedules);
    }

    [Fact]
    public async Task MalformedResponse_ShowsSafeErrorAndAllowsRetry()
    {
        var handler = new RecordingHandler();
        handler.Responses.Enqueue(new(HttpStatusCode.OK) { Content = new StringContent("<html>Bad gateway</html>") });
        handler.Responses.Enqueue(Envelope(200, new List<AdminDoctorOptionDTO> { new() { Id = 2, IsActive = true } }));
        var state = CreateState(handler);
        await state.LoadDoctorsAsync();
        Assert.Contains("không hợp lệ", state.DoctorsError);
        Assert.False(state.IsLoadingDoctors);
        await state.LoadDoctorsAsync();
        Assert.Empty(state.DoctorsError);
        Assert.Equal(2, Assert.Single(state.Doctors).Id);
    }

    [Fact]
    public async Task Delete_UsesExactIdAndDoesNotMutateScheduleList()
    {
        var handler = new RecordingHandler();
        handler.Responses.Enqueue(Envelope(200, new AdminDoctorTimeOffDTO { Id = 23 }));
        var state = CreateState(handler);
        Assert.True(await state.DeleteTimeOffAsync(23));
        Assert.Equal(HttpMethod.Delete, Assert.Single(handler.Methods));
        Assert.EndsWith("/api/admin/time-off/23", Assert.Single(handler.Uris));
    }

    [Fact]
    public async Task DoubleSubmit_SendsOnlyOneMutation()
    {
        var handler = new RecordingHandler { Hold = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        handler.Responses.Enqueue(Envelope(201, new ScheduleChangeResultDTO { Schedule = new() { Id = 5 } }));
        var state = CreateState(handler);
        var first = state.SaveScheduleAsync(null, Schedule());
        Assert.True(state.IsSubmitting);
        Assert.False(await state.SaveScheduleAsync(null, Schedule()));
        handler.Hold.SetResult(true);
        Assert.True(await first);
        Assert.Single(handler.Uris);
        Assert.False(state.IsSubmitting);
    }

    private static ScheduleRequestDTO Schedule() => new() { DoctorId = 1, DayOfWeek = 1, StartTime = new(8, 0), EndTime = new(17, 0), SlotMinutes = 30, EffectiveFrom = new(2026, 10, 19) };
    private static HttpResponseMessage Envelope<T>(int status, T? content, string message = "Thành công") => new((HttpStatusCode)status)
    { Content = JsonContent.Create(new HttpResponseData<T> { StatusCode = status, Content = content, Message = message }) };

    private static AdminSchedulingStateService CreateState(RecordingHandler handler)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(handler, false) { BaseAddress = new("http://fixture.local") });
        // Auth is covered by existing HTTP tests; this fixture exercises frontend HTTP state only.
        var user = new UserStateService(null!, factory.Object, null!, new CustomAuthenticationStateProvider());
        typeof(UserStateService).GetProperty(nameof(UserStateService.AccessToken))!.SetValue(user, "fixture-token");
        return new(new AuthorizedApiService(factory.Object, user));
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public Queue<HttpResponseMessage> Responses { get; } = new();
        public List<string> Uris { get; } = new();
        public List<HttpMethod> Methods { get; } = new();
        public List<string> Bodies { get; } = new();
        public bool ThrowNetwork { get; set; }
        public TaskCompletionSource<bool>? Hold { get; set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Uris.Add(request.RequestUri!.ToString());
            Methods.Add(request.Method);
            Bodies.Add(request.Content != null ? await request.Content.ReadAsStringAsync(cancellationToken) : string.Empty);
            if (Hold != null) await Hold.Task;
            if (ThrowNetwork) throw new HttpRequestException("Offline fixture");
            return Responses.Dequeue();
        }
    }
}
