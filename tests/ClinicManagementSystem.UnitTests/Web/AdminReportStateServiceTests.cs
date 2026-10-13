using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Report;
using ClinicManagementSystem.Application.DTOs.Scheduling;
using ClinicManagementSystem.Application.Helpers;
using ClinicManagementSystem.Web.Services;
using Moq;

namespace ClinicManagementSystem.UnitTests.Web;

public class AdminReportStateServiceTests
{
    private static ClinicReportQueryDTO Query(int? doctor = 7) => new() { From = new(2026, 10, 1), To = new(2026, 10, 13), DoctorId = doctor };
    private static ClinicReportDTO Report(int? doctor = 7) => new() { From = new(2026, 10, 1), To = new(2026, 10, 13), DoctorId = doctor,
        TotalVisits = 2, NoShowCount = 1, ConcludedAppointments = 3, NoShowRate = 33.33m, TotalRevenue = -10,
        Doctors = [new() { DoctorId = 7, DoctorName = "Lê Thu Hà", NoShowRate = 33.33m, TotalRevenue = -10 }],
        RevenueItems = [new() { Category = "Service", SourceId = 4, Description = "Giá snapshot", Revenue = -10 }] };

    [Theory]
    [InlineData(null)]
    [InlineData(7)]
    public async Task Load_UsesIsoDatesAndOptionalDoctor_WithAuthorizedApi(int? doctor)
    {
        var h = new RecordingHandler(); h.Responses.Enqueue(Envelope(200, Report(doctor)));
        var state = State(h); var previous = CultureInfo.CurrentCulture;
        try { CultureInfo.CurrentCulture = new("vi-VN"); await state.LoadAsync(Query(doctor)); }
        finally { CultureInfo.CurrentCulture = previous; }
        Assert.Equal("http://fixture.local/api/admin/reports?from=2026-10-01&to=2026-10-13" + (doctor == null ? "" : "&doctorId=7"), Assert.Single(h.Uris));
        Assert.Equal("Bearer fixture-token", Assert.Single(h.Authorization));
        Assert.Equal(-10m, state.Report!.TotalRevenue); Assert.Equal(33.33m, state.Report.NoShowRate);
        Assert.True(state.MatchesAppliedFilter(Query(doctor))); Assert.False(state.IsBusy); Assert.Empty(state.ReportError);
    }

    [Fact]
    public async Task Doctors_IncludeInactiveHistoricalOptions_AndSortByName()
    {
        var h = new RecordingHandler(); h.Responses.Enqueue(Envelope(200, new List<AdminDoctorOptionDTO>
            { new() { Id = 7, FullName = "Z bác sĩ", IsActive = true }, new() { Id = 8, FullName = "A bác sĩ", IsActive = false } }));
        var state = State(h); await state.LoadDoctorsAsync();
        Assert.EndsWith("/api/admin/scheduling/doctors", Assert.Single(h.Uris));
        Assert.Equal(new[] { 8, 7 }, state.Doctors.Select(d => d.Id)); Assert.False(state.Doctors[0].IsActive);
    }

    [Theory]
    [InlineData(null, "2026-10-13", 7)]
    [InlineData("2026-10-01", null, 7)]
    [InlineData("2026-10-13", "2026-10-01", 7)]
    [InlineData("2026-01-01", "2027-01-02", 7)]
    [InlineData("2026-10-01", "2026-10-13", 0)]
    public async Task InvalidFilter_DoesNotSendAnApiRequest(string? from, string? to, int doctor)
    {
        var h = new RecordingHandler(); var state = State(h);
        await state.LoadAsync(new() { From = from == null ? null : DateOnly.Parse(from), To = to == null ? null : DateOnly.Parse(to), DoctorId = doctor });
        Assert.Empty(h.Uris); Assert.Null(state.Report); Assert.NotEmpty(state.ReportError); Assert.False(state.IsBusy);
    }

    [Fact]
    public async Task ChangedForm_DoesNotChangeLoadedReportOrExportQuery()
    {
        var h = new RecordingHandler(); h.Responses.Enqueue(Envelope(200, Report())); h.Responses.Enqueue(Workbook());
        var state = State(h); var query = Query(); await state.LoadAsync(query);
        query.From = new(2026, 9, 1); query.DoctorId = 8;
        Assert.False(state.MatchesAppliedFilter(query)); Assert.True(state.MatchesAppliedFilter(Query()));
        ClinicReportExportDTO? downloaded = null;
        Assert.True(await state.ExportAsync(file => { downloaded = file; return Task.CompletedTask; }));
        Assert.EndsWith("/api/admin/reports/export?from=2026-10-01&to=2026-10-13&doctorId=7", h.Uris.Last());
        Assert.NotEmpty(downloaded!.Bytes); Assert.EndsWith("_doctor-7.xlsx", downloaded.FileName);
        Assert.Contains("Tải xuống", state.ExportMessage); Assert.False(state.IsBusy);
    }

    [Fact]
    public async Task ExportWithoutLoadedReport_DoesNotRequestOrDownload()
    {
        var h = new RecordingHandler(); var state = State(h); var called = false;
        Assert.False(await state.ExportAsync(_ => { called = true; return Task.CompletedTask; }));
        Assert.False(called); Assert.Empty(h.Uris); Assert.NotEmpty(state.ExportError);
    }

    [Fact]
    public async Task EmptyReport_IsSuccessful_AndCanExport()
    {
        var h = new RecordingHandler(); h.Responses.Enqueue(Envelope(200, new ClinicReportDTO { From = Query().From!.Value, To = Query().To!.Value, DoctorId = 7 })); h.Responses.Enqueue(Workbook());
        var state = State(h); await state.LoadAsync(Query());
        Assert.NotNull(state.Report); Assert.Equal(0, state.Report.TotalRevenue); Assert.Empty(state.Report.Doctors);
        Assert.True(await state.ExportAsync(_ => Task.CompletedTask));
    }

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    public async Task LostAccessDuringExport_ClearsSensitiveReportAndStopsDownload(int status)
    {
        var h = new RecordingHandler(); h.Responses.Enqueue(Envelope(200, Report()));
        h.Responses.Enqueue(new((HttpStatusCode)status) { Content = new StringContent("<html>no access</html>") });
        if (status == 401) h.Responses.Enqueue(new(HttpStatusCode.BadRequest)); // Existing AuthorizedApiService tries one refresh.
        var state = State(h); await state.LoadAsync(Query()); var called = false;
        Assert.False(await state.ExportAsync(_ => { called = true; return Task.CompletedTask; }));
        Assert.False(called); Assert.Null(state.Report); Assert.False(state.MatchesAppliedFilter(Query()));
        Assert.Equal(status == 401, state.RequiresLogin); Assert.Equal(status == 403, state.AccessDenied);
        Assert.DoesNotContain("<html>", state.ExportError); Assert.False(state.IsBusy);
    }

    [Fact]
    public async Task FailedReload_ClearsPreviouslyDisplayedFinancialData()
    {
        var h = new RecordingHandler(); h.Responses.Enqueue(Envelope(200, Report())); h.Responses.Enqueue(new(HttpStatusCode.InternalServerError));
        var state = State(h); await state.LoadAsync(Query()); await state.LoadAsync(Query(8));
        Assert.Null(state.Report); Assert.NotEmpty(state.ReportError); Assert.False(state.MatchesAppliedFilter(Query()));
    }

    [Theory]
    [InlineData("from")]
    [InlineData("doctor")]
    public async Task ReportForDifferentFilters_IsRejected(string mismatch)
    {
        var report = Report(); if (mismatch == "from") report.From = new(2026, 9, 1); else report.DoctorId = 8;
        var h = new RecordingHandler(); h.Responses.Enqueue(Envelope(200, report)); var state = State(h);
        await state.LoadAsync(Query()); Assert.Null(state.Report); Assert.Contains("không hợp lệ", state.ReportError);
    }

    [Theory]
    [InlineData("network", "kết nối")]
    [InlineData("timeout", "thời gian chờ")]
    [InlineData("html", "không hợp lệ")]
    [InlineData("array", "không hợp lệ")]
    public async Task Failures_AreReadableAndRetryable(string kind, string message)
    {
        var h = new RecordingHandler();
        if (kind == "network") h.Failure = new HttpRequestException("secret fixture detail");
        else if (kind == "timeout") h.Failure = new TaskCanceledException("secret fixture detail");
        else h.Responses.Enqueue(new(HttpStatusCode.OK) { Content = new StringContent(kind == "array" ? "[]" : "<html>Error</html>") });
        var state = State(h); await state.LoadAsync(Query());
        Assert.Contains(message, state.ReportError); Assert.DoesNotContain("secret", state.ReportError); Assert.Null(state.Report); Assert.False(state.IsBusy);
        h.Failure = null; h.Responses.Enqueue(Envelope(200, Report())); await state.LoadAsync(Query());
        Assert.NotNull(state.Report); Assert.Empty(state.ReportError);
    }

    [Fact]
    public async Task MvcValidationMessage_IsDisplayed()
    {
        var h = new RecordingHandler(); h.Responses.Enqueue(new(HttpStatusCode.BadRequest) { Content = new StringContent("{\"errors\":{\"To\":[\"Khoảng ngày không hợp lệ.\"]}}") });
        var state = State(h); await state.LoadAsync(Query()); Assert.Equal("Khoảng ngày không hợp lệ.", state.ReportError);
    }

    [Theory]
    [InlineData("success-json")]
    [InlineData("error-json")]
    [InlineData("empty-file")]
    [InlineData("html-file")]
    public async Task InvalidExport_NeverTriggersBrowserDownload(string kind)
    {
        var h = new RecordingHandler(); h.Responses.Enqueue(Envelope(200, Report()));
        h.Responses.Enqueue(kind switch
        {
            "success-json" => Envelope(200, Report()),
            "error-json" => Envelope<ClinicReportExportDTO>(400, null, "Khoảng ngày không hợp lệ."),
            "empty-file" => Workbook([]),
            _ => Workbook(System.Text.Encoding.UTF8.GetBytes("<html>Error</html>"))
        });
        var state = State(h); await state.LoadAsync(Query()); var called = false;
        Assert.False(await state.ExportAsync(_ => { called = true; return Task.CompletedTask; }));
        Assert.False(called); Assert.NotEmpty(state.ExportError); Assert.Empty(state.ExportMessage); Assert.NotNull(state.Report);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("../../evil.xlsx")]
    [InlineData("file.html")]
    public async Task MissingOrUnsafeFileName_UsesKnownXlsxName(string? name)
    {
        var h = new RecordingHandler(); h.Responses.Enqueue(Envelope(200, Report())); h.Responses.Enqueue(Workbook(name: name));
        var state = State(h); await state.LoadAsync(Query()); string? actual = null;
        await state.ExportAsync(file => { actual = file.FileName; return Task.CompletedTask; });
        Assert.Equal("clinic-report_2026-10-01_2026-10-13_doctor-7.xlsx", actual);
    }

    [Fact]
    public async Task InFlightReport_BlocksCompetingLoadsAndExports()
    {
        var h = new RecordingHandler { Hold = new(TaskCreationOptions.RunContinuationsAsynchronously) }; h.Responses.Enqueue(Envelope(200, Report()));
        var state = State(h); var pending = state.LoadAsync(Query()); Assert.True(state.IsBusy);
        await state.LoadAsync(Query(8)); await state.LoadDoctorsAsync(); Assert.False(await state.ExportAsync(_ => Task.CompletedTask));
        h.Hold.SetResult(true); await pending;
        Assert.Single(h.Uris); Assert.False(state.IsBusy);
    }

    [Fact]
    public async Task Export_StaysBusyUntilBrowserConsumesFile_AndSupportsRetryAfterBrowserFailure()
    {
        var h = new RecordingHandler(); h.Responses.Enqueue(Envelope(200, Report())); h.Responses.Enqueue(Workbook()); h.Responses.Enqueue(Workbook());
        var state = State(h); await state.LoadAsync(Query()); var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = state.ExportAsync(_ => gate.Task); Assert.True(state.IsExporting);
        Assert.False(await state.ExportAsync(_ => Task.CompletedTask)); await state.LoadAsync(Query(8)); Assert.Equal(2, h.Uris.Count);
        gate.SetException(new InvalidOperationException("secret browser details")); Assert.False(await pending);
        Assert.False(state.IsBusy); Assert.Contains("Trình duyệt", state.ExportError); Assert.DoesNotContain("secret", state.ExportError);
        Assert.True(await state.ExportAsync(_ => Task.CompletedTask)); Assert.Empty(state.ExportError);
    }

    private static HttpResponseMessage Envelope<T>(int status, T? content, string message = "Thành công") => new((HttpStatusCode)status)
        { Content = JsonContent.Create(new HttpResponseData<T> { StatusCode = status, Content = content, Message = message }) };
    private static HttpResponseMessage Workbook(byte[]? bytes = null, string? name = "clinic-report_2026-10-01_2026-10-13_doctor-7.xlsx")
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes ?? ClinicReportExcelWriter.Create(Report())) };
        response.Content.Headers.ContentType = new(ClinicReportExportDTO.ContentType);
        if (name != null) response.Content.Headers.ContentDisposition = new("attachment") { FileName = name };
        return response;
    }
    private static AdminReportStateService State(RecordingHandler handler)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(handler, false) { BaseAddress = new("http://fixture.local") });
        var user = new UserStateService(null!, factory.Object, null!, new CustomAuthenticationStateProvider());
        typeof(UserStateService).GetProperty(nameof(UserStateService.AccessToken))!.SetValue(user, "fixture-token");
        typeof(UserStateService).GetProperty(nameof(UserStateService.RefreshToken))!.SetValue(user, "fixture-refresh");
        return new(new AuthorizedApiService(factory.Object, user));
    }
    private sealed class RecordingHandler : HttpMessageHandler
    {
        public Queue<HttpResponseMessage> Responses { get; } = new();
        public List<string> Uris { get; } = []; public List<string?> Authorization { get; } = [];
        public Exception? Failure { get; set; }
        public TaskCompletionSource<bool>? Hold { get; set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Uris.Add(request.RequestUri!.AbsoluteUri); Authorization.Add(request.Headers.Authorization?.ToString());
            if (Hold != null) await Hold.Task;
            if (Failure != null) throw Failure;
            return Responses.Dequeue();
        }
    }
}
