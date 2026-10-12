using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Scheduling;
using ClinicManagementSystem.Web.Pages.Admin;
using ClinicManagementSystem.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;

namespace ClinicManagementSystem.UnitTests.Web;

public class AdminSchedulesRenderingTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("OpenSchedule")]
    [InlineData("OpenTimeOff")]
    public async Task RazorPage_RendersListsAndEditors_WithActualBlazorInputComponents(string? openEditor)
    {
        var handler = new FixtureHandler();
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(handler, false) { BaseAddress = new("http://fixture.local") });
        var user = new UserStateService(null!, factory.Object, null!, new CustomAuthenticationStateProvider());
        typeof(UserStateService).GetProperty(nameof(UserStateService.AccessToken))!.SetValue(user, "fixture-token");
        var state = new AdminSchedulingStateService(new AuthorizedApiService(factory.Object, user));
        var activator = new CapturingActivator();
        using var services = new ServiceCollection().AddLogging()
            .AddSingleton(state)
            .AddSingleton(Mock.Of<IJSRuntime>())
            .AddSingleton<NavigationManager>(new FixtureNavigation())
            .AddSingleton<IComponentActivator>(activator)
            .BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = await renderer.RenderComponentAsync<Schedules>();
            if (openEditor != null)
            {
                typeof(Schedules).GetMethod(openEditor, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(activator.Page, null);
                typeof(ComponentBase).GetMethod("StateHasChanged", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(activator.Page, null);
            }
            return root.ToHtmlString();
        });
        html = WebUtility.HtmlDecode(html);
        Assert.Contains("Bác sĩ mẫu", html);
        Assert.Contains("Cả phòng khám", html);
        Assert.Contains("26/10/2026 00:00", html);
        Assert.Contains("Phân trang lịch làm việc", html);
        Assert.Contains("Phân trang nghỉ phép", html);
        Assert.Contains("admin-schedules-page", html);
        if (openEditor == "OpenSchedule")
        {
            Assert.Contains("id=\"schedule-start\"", html);
            Assert.Contains("value=\"08:00\"", html);
            Assert.Contains("id=\"schedule-from\"", html);
            Assert.Contains("Lưu lịch làm việc", html);
        }
        if (openEditor == "OpenTimeOff")
        {
            Assert.Contains("type=\"datetime-local\"", html);
            Assert.Contains("id=\"timeoff-doctor\"", html);
            Assert.Contains("maxlength=\"255\"", html);
            Assert.Contains("Lưu đăng ký nghỉ", html);
        }
    }

    private sealed class FixtureNavigation : NavigationManager
    { public FixtureNavigation() => Initialize("http://fixture.local/", "http://fixture.local/admin/schedules"); }
    private sealed class CapturingActivator : IComponentActivator
    {
        public Schedules? Page { get; private set; }
        public IComponent CreateInstance(Type type)
        {
            var instance = (IComponent)Activator.CreateInstance(type)!;
            if (instance is Schedules page) Page = page;
            return instance;
        }
    }
    private sealed class FixtureHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            object content = request.RequestUri!.AbsolutePath switch
            {
                "/api/admin/scheduling/doctors" => new List<AdminDoctorOptionDTO> { new() { Id = 1, FullName = "Bác sĩ mẫu", IsActive = true } },
                "/api/admin/doctor-schedules" => new PagedResult<AdminDoctorScheduleDTO> { Items = [new() { Id = 1, DoctorId = 1, DoctorName = "Bác sĩ mẫu", DayOfWeek = 1, StartTime = new(8, 0), EndTime = new(17, 0), EffectiveFrom = new(2026, 10, 19), SlotMinutes = 30, IsActive = true }], Page = 1, TotalItems = 1, TotalPages = 1 },
                "/api/admin/time-off" => new PagedResult<AdminDoctorTimeOffDTO> { Items = [new() { Id = 2, DoctorId = null, DoctorName = "Cả phòng khám", StartAt = new(2026, 10, 26, 0, 0, 0), EndAt = new(2026, 10, 27, 0, 0, 0), Type = 1, Reason = "Nghỉ lễ", IsFullDay = true }], Page = 1, TotalItems = 1, TotalPages = 1 },
                _ => throw new InvalidOperationException("Unexpected UI request")
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new HttpResponseData<object> { StatusCode = 200, Message = "Thành công", Content = content }) });
        }
    }
}
