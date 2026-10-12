using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Appointment;
using ClinicManagementSystem.Application.DTOs.Doctor;
using ClinicManagementSystem.Application.DTOs.Notification;
using ClinicManagementSystem.Application.DTOs.Specialty;
using ClinicManagementSystem.Web.Pages.Reception;
using ClinicManagementSystem.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;

namespace ClinicManagementSystem.UnitTests.Web;

public class ReceptionAffectedRenderingTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("reschedule")]
    [InlineData("cancel")]
    public async Task Razor_RendersQueueReadonlyRowsAndActualEditor(string? editor)
    {
        await using var fixture = new RenderFixture();
        var html = await fixture.RenderAffected(async page =>
        {
            if (editor != null) await Invoke(page, "OpenEditor", ReceptionUiFixture.Appointment(), editor == "cancel");
        });
        Assert.Contains("APT-TEST", html);
        Assert.Contains("Bệnh nhân mẫu", html);
        Assert.Contains("0909090909", html);
        Assert.Contains("Bác sĩ đăng ký nghỉ.", html);
        Assert.Contains("Cần xử lý riêng", html);
        Assert.DoesNotContain("id=\"affected-cancel-8\"", html);
        Assert.Contains("Phân trang lịch hẹn bị ảnh hưởng", html);
        if (editor != null)
        {
            Assert.Contains("id=\"affected-editor-heading\"", html);
            Assert.Contains("maxlength=\"500\"", html);
            Assert.Contains("id=\"affected-contact-confirmed\"", html);
            Assert.Equal("affected-editor-heading", Field(fixture.Activator.Page!, "_focusId"));
        }
        if (editor == "reschedule")
        {
            Assert.Contains("Giữ nguyên bác sĩ", html);
            Assert.Contains("Giờ khám còn trống", html);
            Assert.Contains("09:00 – 09:30", html);
        }
        if (editor == "cancel")
        {
            Assert.Contains("Xác nhận hủy lịch", html);
            Assert.Contains("thanh toán trước khi hủy", html);
            Assert.DoesNotContain("affected-new-date", html);
        }
    }

    [Fact]
    public async Task ChangingDay_ClearsSelectedTimeAndClosingRestoresTriggerFocus()
    {
        await using var fixture = new RenderFixture();
        var html = await fixture.RenderAffected(async page =>
        {
            await Invoke(page, "OpenEditor", ReceptionUiFixture.Appointment(), false);
            await Invoke(page, "SelectSlot", fixture.State.Slots[0].StartTime);
            var form = Field(page, "_form")!;
            Assert.NotNull(form.GetType().GetProperty("NewStartTime")!.GetValue(form));
            typeof(ReceptionAffectedAppointments).GetField("_newDate", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(page, new DateOnly(2026, 10, 20));
            await Invoke(page, "DateChanged");
            Assert.Null(form.GetType().GetProperty("NewStartTime")!.GetValue(form));
            Assert.Equal(ReceptionUiFixture.NewTime, fixture.State.Slots[0].StartTime);
            await Invoke(page, "CloseEditor");
            Assert.Null(fixture.State.Target);
            Assert.Equal("affected-reschedule-7", Field(page, "_focusId"));
        });
        Assert.DoesNotContain("id=\"affected-editor-heading\"", html);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Form_RequiresContactConfirmation_AndSlotOnlyForReschedule(bool cancel)
    {
        await using var fixture = new RenderFixture();
        await fixture.RenderAffected(async page =>
        {
            await Invoke(page, "OpenEditor", ReceptionUiFixture.Appointment(), cancel);
            var form = Field(page, "_form")!;
            form.GetType().GetProperty("Reason")!.SetValue(form, "Đã thống nhất với bệnh nhân");
            var results = new List<ValidationResult>();
            Assert.False(Validator.TryValidateObject(form, new(form), results, true));
            Assert.Contains(results, r => r.MemberNames.Contains("ContactConfirmed"));
            Assert.Equal(!cancel, results.Any(r => r.MemberNames.Contains("NewStartTime")));
            form.GetType().GetProperty("ContactConfirmed")!.SetValue(form, true);
            if (!cancel) await Invoke(page, "SelectSlot", fixture.State.Slots[0].StartTime);
            results.Clear();
            Assert.True(Validator.TryValidateObject(form, new(form), results, true));
        });
    }

    [Fact]
    public async Task SavedAction_ReloadsQueueAndFocusesSuccessMessage()
    {
        await using var fixture = new RenderFixture();
        var html = await fixture.RenderAffected(async page =>
        {
            await Invoke(page, "OpenEditor", ReceptionUiFixture.Appointment(), true);
            var form = Field(page, "_form")!;
            form.GetType().GetProperty("Reason")!.SetValue(form, "Đã thống nhất hủy");
            form.GetType().GetProperty("ContactConfirmed")!.SetValue(form, true);
            await Invoke(page, "Submit");
            Assert.Equal("affected-success", Field(page, "_focusId"));
        });
        Assert.Contains("Đã hủy lịch hẹn.", html);
        Assert.DoesNotContain("APT-TEST", html);
        Assert.Null(fixture.State.Target);
        Assert.Equal(2, fixture.Handler.ListLoads);
    }

    [Fact]
    public async Task Conflict_ClosesStaleEditorReloadsQueueAndFocusesError()
    {
        await using var fixture = new RenderFixture();
        fixture.Handler.Conflict = true;
        var html = await fixture.RenderAffected(async page =>
        {
            await Invoke(page, "OpenEditor", ReceptionUiFixture.Appointment(), true);
            var form = Field(page, "_form")!;
            form.GetType().GetProperty("Reason")!.SetValue(form, "Đã thống nhất hủy");
            await Invoke(page, "Submit");
            Assert.Equal("affected-action-error", Field(page, "_focusId"));
        });
        Assert.Contains("Lịch đã được cập nhật.", html);
        Assert.Contains("Danh sách đã được tải lại", html);
        Assert.DoesNotContain("id=\"affected-editor-heading\"", html);
        Assert.Null(fixture.State.Target);
        Assert.Equal(2, fixture.Handler.ListLoads);
    }

    [Fact]
    public async Task NotificationPage_RendersHistoricalNoticeAndConvertsUtcToClinicTime()
    {
        await using var fixture = new RenderFixture();
        var html = await fixture.RenderNotifications();
        Assert.Contains("Lịch bác sĩ thay đổi", html);
        Assert.Contains("19/10/2026 09:00", html);
        Assert.Contains("href=\"/reception/affected-appointments\"", html);
        Assert.Contains("Thông báo được giữ làm lịch sử", html);
        Assert.Contains("#7", html);
    }

    [Theory]
    [InlineData(typeof(ReceptionAffectedAppointments), "/reception/affected-appointments")]
    [InlineData(typeof(ReceptionNotifications), "/reception/notifications")]
    public void Pages_AreReceptionistOnly(Type page, string route)
    {
        Assert.Equal("Receptionist", page.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal(route, page.GetCustomAttribute<RouteAttribute>()!.Template);
    }

    private static object? Field(object page, string field) => page.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(page);
    private static async Task Invoke(object page, string method, params object[] args)
    {
        var result = page.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(page, args);
        if (result is Task task) await task;
    }

    private sealed class RenderFixture : IAsyncDisposable
    {
        public FixtureHandler Handler { get; } = new();
        public ReceptionAffectedAppointmentStateService State { get; }
        public CapturingActivator Activator { get; } = new();
        private readonly ServiceProvider _services;
        private readonly HtmlRenderer _renderer;
        public RenderFixture()
        {
            State = ReceptionUiFixture.CreateState(Handler);
            _services = new ServiceCollection().AddLogging().AddSingleton(State)
                .AddSingleton(Mock.Of<IJSRuntime>()).AddSingleton<NavigationManager>(new FixtureNavigation())
                .AddSingleton<IComponentActivator>(Activator).BuildServiceProvider();
            _renderer = new(_services, _services.GetRequiredService<ILoggerFactory>());
        }
        public Task<string> RenderAffected(Func<ReceptionAffectedAppointments, Task> act) => _renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = await _renderer.RenderComponentAsync<ReceptionAffectedAppointments>();
            await act(Activator.Page!);
            typeof(ComponentBase).GetMethod("StateHasChanged", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Activator.Page, null);
            return WebUtility.HtmlDecode(root.ToHtmlString());
        });
        public Task<string> RenderNotifications() => _renderer.Dispatcher.InvokeAsync(async () => WebUtility.HtmlDecode((await _renderer.RenderComponentAsync<ReceptionNotifications>()).ToHtmlString()));
        public async ValueTask DisposeAsync() { await _renderer.DisposeAsync(); await _services.DisposeAsync(); }
    }
    private sealed class FixtureNavigation : NavigationManager
    { public FixtureNavigation() => Initialize("http://fixture.local/", "http://fixture.local/reception/affected-appointments"); }
    private sealed class CapturingActivator : IComponentActivator
    {
        public ReceptionAffectedAppointments? Page { get; private set; }
        public IComponent CreateInstance(Type type)
        {
            var component = (IComponent)Activator.CreateInstance(type)!;
            if (component is ReceptionAffectedAppointments page) Page = page;
            return component;
        }
    }
    private sealed class FixtureHandler : HttpMessageHandler
    {
        public int ListLoads { get; private set; }
        public bool Conflict { get; set; }
        private bool _saved;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var path = request.RequestUri!.AbsolutePath;
            if (request.Method == HttpMethod.Patch)
            {
                if (Conflict) return Task.FromResult(ReceptionUiFixture.Envelope<ReceptionAffectedAppointmentDTO>(null, 409, "Lịch đã được cập nhật."));
                _saved = true;
                return Task.FromResult(ReceptionUiFixture.Envelope(ReceptionUiFixture.Appointment(), message: "Đã hủy lịch hẹn."));
            }
            var checkedIn = ReceptionUiFixture.Appointment(); checkedIn.Id = 8; checkedIn.AppointmentCode = "APT-READONLY";
            checkedIn.CanProcess = false; checkedIn.Status = 2; checkedIn.CannotProcessReason = "Bệnh nhân đã check-in; cần xử lý với bác sĩ.";
            object content;
            if (path == "/api/reception/appointments/affected")
            {
                ListLoads++;
                content = new PagedResult<ReceptionAffectedAppointmentDTO>
                { Items = _saved ? [checkedIn] : [ReceptionUiFixture.Appointment(), checkedIn], Page = 1, PageSize = 10, TotalPages = 1, TotalItems = _saved ? 1 : 2 };
            }
            else if (path.EndsWith("/available-slots"))
            {
                var day = DateOnly.Parse(request.RequestUri.Query.Split("date=")[1]);
                var time = day.ToDateTime(new TimeOnly(9, 0));
                content = new List<SlotDTO> { new() { StartTime = time, EndTime = time.AddMinutes(30), IsAvailable = true } };
            }
            else content = path switch
            {
                "/api/specialties" => new List<SpecialtyDTO> { new() { Id = 1, Name = "Chuyên khoa mẫu" } },
                "/api/doctors" => new List<DoctorDTO> { new() { Id = 5, FullName = "Bác sĩ mẫu" } },
                "/api/notifications" => new List<NotificationDTO> { new() { Id = 3, AppointmentId = 7, Title = "Lịch bác sĩ thay đổi", Content = "Liên hệ bệnh nhân mẫu", CreatedAt = new(2026, 10, 19, 2, 0, 0, DateTimeKind.Utc) } },
                _ => throw new InvalidOperationException("Unexpected UI request: " + path)
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new HttpResponseData<object> { StatusCode = 200, Message = "Thành công", Content = content }) });
        }
    }
}
