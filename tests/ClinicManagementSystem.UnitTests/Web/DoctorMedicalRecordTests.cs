using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Doctor;
using ClinicManagementSystem.Application.DTOs.MedicalRecord;
using ClinicManagementSystem.Application.DTOs.Prescription;
using ClinicManagementSystem.Web.Pages.Doctor;
using ClinicManagementSystem.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;
using PrescriptionPage = ClinicManagementSystem.Web.Pages.Doctor.Prescription;

namespace ClinicManagementSystem.UnitTests.Web;

public class DoctorMedicalRecordTests
{
    [Fact]
    public async Task Filters_AreEncodedAndPagingUsesTheSameCriteria()
    {
        var handler = new Handler(_ => Envelope(Page()));
        var state = new DoctorMedicalRecordStateService(Api(handler));
        state.Keyword = "An & Bình"; state.Status = 0; state.From = new(2026, 10, 1); state.To = new(2026, 10, 19);
        await state.LoadAsync(2);
        Assert.Contains("keyword=An%20%26%20B", handler.Paths[0]);
        Assert.Contains("status=0", handler.Paths[0]);
        Assert.Contains("from=2026-10-01&to=2026-10-19", handler.Paths[0]);
        Assert.Contains("page=2", handler.Paths[0]);
        Assert.False(state.HasUnappliedFilters);
        state.Keyword = "changed";
        Assert.True(state.HasUnappliedFilters);
    }

    [Fact]
    public async Task InvalidDateRange_ShowsErrorWithoutSendingRequest()
    {
        var handler = new Handler(_ => Envelope(Page()));
        var state = new DoctorMedicalRecordStateService(Api(handler)) { From = new(2026, 10, 19) };
        await state.LoadAsync();
        Assert.Empty(handler.Paths);
        Assert.Contains("nhập đủ", state.ErrorMessage);
        Assert.False(state.IsLoading);
    }

    [Theory]
    [InlineData(403)]
    [InlineData(500)]
    public async Task FailedRefresh_RemovesPreviouslyDisplayedPatientData(int code)
    {
        var call = 0;
        var handler = new Handler(_ => ++call == 1 ? Envelope(Page()) : new((HttpStatusCode)code) { Content = JsonContent.Create(new { status = code }) });
        var state = new DoctorMedicalRecordStateService(Api(handler));
        await state.LoadAsync(); Assert.NotEmpty(state.Records.Items);
        await state.LoadAsync(); Assert.Empty(state.Records.Items); Assert.NotEmpty(state.ErrorMessage);
    }

    [Fact]
    public async Task LeavingPage_DiscardsAResponseThatArrivesAfterReset()
    {
        var pending = new TaskCompletionSource<HttpResponseMessage>();
        var handler = new AsyncHandler(pending.Task);
        var state = new DoctorMedicalRecordStateService(Api(handler));
        var load = state.LoadAsync();
        state.Reset(); pending.SetResult(Envelope(Page())); await load;
        Assert.Empty(state.Records.Items); Assert.False(state.HasLoaded); Assert.False(state.IsLoading);
    }

    [Theory]
    [InlineData("list")]
    [InlineData("empty")]
    [InlineData("error")]
    public async Task ActualListRazor_ShowsCorrectActionsAndStates(string scenario)
    {
        var handler = new Handler(_ => scenario == "error" ? new(HttpStatusCode.InternalServerError) : Envelope(Page(scenario == "empty")));
        var state = new DoctorMedicalRecordStateService(Api(handler));
        using var services = Services().AddSingleton(state).BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        var raw = await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<MedicalRecords>()).ToHtmlString());
        var html = WebUtility.HtmlDecode(raw);
        Assert.Contains("Bệnh án của tôi", html); Assert.Contains("Bản nháp", html); Assert.Contains("Đã chốt", html);
        if (scenario == "list")
        {
            Assert.Contains("Tiếp tục khám", html); Assert.Contains("Xem chi tiết", html);
            Assert.Contains("/doctor/exam/2", raw); Assert.Contains("Lịch hẹn đã hủy", html);
            Assert.Contains("19/10/2026", html); Assert.DoesNotContain("<script>", raw);
            var preview = Environment.GetEnvironmentVariable("DOCTOR_RECORDS_PREVIEW_DIR");
            if (!string.IsNullOrWhiteSpace(preview)) { Directory.CreateDirectory(preview); await File.WriteAllTextAsync(Path.Combine(preview, "list.html"), raw); }
        }
        else if (scenario == "empty") Assert.Contains("Không có bệnh án phù hợp", html);
        else { Assert.Contains("role=\"alert\"", raw); Assert.Contains("Thử lại", html); Assert.DoesNotContain("/doctor/exam/", raw); }
        Assert.Equal("Doctor", typeof(MedicalRecords).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
    }

    [Theory]
    [InlineData("exam", false)]
    [InlineData("exam", true)]
    [InlineData("services", false)]
    [InlineData("prescription", false)]
    public async Task ExistingDoctorScreens_RespectReadOnlyModeAndLoadSavedPrescription(string screen, bool editable)
    {
        var handler = new Handler(request => request.RequestUri!.AbsolutePath.StartsWith("/api/medical-records/")
            ? Envelope(new MedicalRecordExamDTO
            {
                Id = 2, PatientName = "Lieu Nguyen", PatientCode = "BN005", Status = 0, CanEdit = editable,
                ReadOnlyReason = editable ? null : "Lịch hẹn đã hủy, bệnh án chỉ được xem thông tin.",
                Prescription = new PrescriptionResponseDTO { Id = 3, Items = [new() { MedicineName = "Thuốc đã lưu", Quantity = 2, Dosage = "sáng 1" }] }
            })
            : Envelope(new List<ClinicManagementSystem.Application.DTOs.Service.ServiceDTO>()));
        var state = new DoctorExamStateService(Api(handler));
        using var services = Services().AddSingleton(state).BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        var raw = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var values = ParameterView.FromDictionary(new Dictionary<string, object?> { ["MedicalRecordId"] = 2 });
            return screen switch
            {
                "exam" => (await renderer.RenderComponentAsync<DoctorExam>(values)).ToHtmlString(),
                "services" => (await renderer.RenderComponentAsync<DoctorExamServices>(values)).ToHtmlString(),
                _ => (await renderer.RenderComponentAsync<PrescriptionPage>(values)).ToHtmlString()
            };
        });
        var html = WebUtility.HtmlDecode(raw);
        Assert.Contains("Bệnh án của tôi", html);
        if (!editable) Assert.Contains("Lịch hẹn đã hủy", html);
        else Assert.DoesNotContain("fa-lock", raw);
        if (screen == "prescription") Assert.Contains("Thuốc đã lưu", html);
        if (screen == "exam" && !editable)
        {
            Assert.Matches("(?s)<button\\b(?:(?!</button>).)*disabled(?:(?!</button>).)*Lưu nháp", html);
            Assert.Matches("(?s)<button\\b(?:(?!</button>).)*disabled(?:(?!</button>).)*Hoàn tất", html);
        }
        if (screen == "services") Assert.Matches("(?s)<button\\b(?:(?!</button>).)*disabled(?:(?!</button>).)*Thêm chỉ định", raw);
    }

    private static IServiceCollection Services() => new ServiceCollection().AddLogging()
        .AddSingleton(Mock.Of<IJSRuntime>()).AddSingleton<NavigationManager>(new FixtureNavigation());
    private sealed class FixtureNavigation : NavigationManager { public FixtureNavigation() => Initialize("http://fixture.local/", "http://fixture.local/doctor/medical-records"); }
    private static AuthorizedApiService Api(HttpMessageHandler handler)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(handler, false) { BaseAddress = new("http://fixture.local") });
        var user = new UserStateService(null!, factory.Object, null!, new CustomAuthenticationStateProvider());
        typeof(UserStateService).GetProperty(nameof(UserStateService.AccessToken))!.SetValue(user, "test-token");
        return new(factory.Object, user);
    }
    private static HttpResponseMessage Envelope<T>(T data) => new(HttpStatusCode.OK) { Content = JsonContent.Create(new HttpResponseData<T> { StatusCode = 200, Content = data }) };
    private static PagedResult<DoctorMedicalRecordDTO> Page(bool empty = false) => new()
    {
        Page = 1, PageSize = 20, TotalItems = empty ? 0 : 3, TotalPages = empty ? 0 : 1,
        Items = empty ? [] : [
            new() { Id = 7, PatientName = "Nguyễn An", PatientCode = "BN005", Status = 0, AppointmentStatus = 2, CanContinue = true, AppointmentStartTime = new(2026, 10, 19, 9, 0, 0) },
            new() { Id = 2, PatientName = "Lieu Nguyen", Status = 0, AppointmentStatus = 4, ReadOnlyReason = "Lịch hẹn đã hủy", AppointmentStartTime = new(2026, 9, 23, 14, 30, 0) },
            new() { Id = 8, PatientName = "<script>alert(1)</script>", Status = 1, AppointmentStatus = 3, AppointmentStartTime = new(2026, 10, 2, 9, 0, 0) }
        ]
    };
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        public List<string> Paths { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) { Paths.Add(request.RequestUri!.PathAndQuery); return Task.FromResult(response(request)); }
    }
    private sealed class AsyncHandler(Task<HttpResponseMessage> pending) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => pending;
    }
}
