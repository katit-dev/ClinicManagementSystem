using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Report;
using ClinicManagementSystem.Application.DTOs.Scheduling;
using ClinicManagementSystem.Web.Pages.Admin;
using ClinicManagementSystem.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;

namespace ClinicManagementSystem.UnitTests.Web;

public class AdminReportsRenderingTests
{
    [Theory]
    [InlineData("data")]
    [InlineData("empty")]
    [InlineData("error")]
    [InlineData("dirty")]
    public async Task ActualRazor_RendersFiltersResultsAndErrorsWithoutStaleExport(string scenario)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(new FixtureHandler(scenario)) { BaseAddress = new("http://fixture.local") });
        var user = new UserStateService(null!, factory.Object, null!, new CustomAuthenticationStateProvider());
        typeof(UserStateService).GetProperty(nameof(UserStateService.AccessToken))!.SetValue(user, "fixture-token");
        var state = new AdminReportStateService(new AuthorizedApiService(factory.Object, user)); var activator = new CapturingActivator();
        using var services = new ServiceCollection().AddLogging().AddSingleton(state).AddSingleton(user)
            .AddSingleton(Mock.Of<IJSRuntime>()).AddSingleton<NavigationManager>(new FixtureNavigation()).AddSingleton<IComponentActivator>(activator).BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = await renderer.RenderComponentAsync<Reports>();
            if (scenario == "dirty")
            {
                var query = (ClinicReportQueryDTO)typeof(Reports).GetField("_filter", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(activator.Page)!;
                query.DoctorId = 8;
                typeof(ComponentBase).GetMethod("StateHasChanged", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(activator.Page, null);
            }
            return root.ToHtmlString();
        });
        html = WebUtility.HtmlDecode(html);
        Assert.Contains("admin-reports-page", html); Assert.Contains("id=\"report-from\"", html); Assert.Contains("id=\"report-to\"", html);
        Assert.Contains("Tất cả bác sĩ", html); Assert.Contains("Bác sĩ lịch sử (đã ngừng hoạt động)", html);
        if (scenario == "error")
        {
            Assert.Contains("id=\"report-error\"", html); Assert.DoesNotContain("reports-metric-icon", html); Assert.DoesNotContain("id=\"export-report\"", html);
        }
        else
        {
            Assert.Contains("Công nợ trong kỳ", html); Assert.Contains("Tỷ lệ không đến", html); Assert.Contains("id=\"export-report\"", html);
            if (scenario == "empty") { Assert.Contains("Không có lượt khám", html); Assert.Contains("0,00 %", html); Assert.DoesNotContain("NaN", html); }
            else { Assert.Contains("Lê Thu Hà", html); Assert.Contains("-50.000 ₫", html); Assert.Contains("33,33 %", html); Assert.Contains("Giá snapshot", html); }
            if (scenario == "dirty")
            {
                Assert.Contains("Bộ lọc đã thay đổi", html);
                Assert.Matches("id=\"export-report\"[^>]*disabled", html);
            }
        }
    }

    [Fact]
    public void ReportsRoute_RequiresAdmin()
    {
        Assert.Equal("Admin", typeof(Reports).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("/admin/reports", typeof(Reports).GetCustomAttribute<RouteAttribute>()!.Template);
    }
    private sealed class FixtureNavigation : NavigationManager { public FixtureNavigation() => Initialize("http://fixture.local/", "http://fixture.local/admin/reports"); }
    private sealed class CapturingActivator : IComponentActivator
    {
        public Reports? Page { get; private set; }
        public IComponent CreateInstance(Type type) { var item = (IComponent)Activator.CreateInstance(type)!; if (item is Reports page) Page = page; return item; }
    }
    private sealed class FixtureHandler(string scenario) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            if (request.RequestUri!.AbsolutePath == "/api/admin/scheduling/doctors") return Task.FromResult(Response(new List<AdminDoctorOptionDTO>
                { new() { Id = 7, FullName = "Lê Thu Hà", IsActive = true }, new() { Id = 8, FullName = "Bác sĩ lịch sử", IsActive = false } }));
            if (scenario == "error") return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            var query = QueryHelpers.ParseQuery(request.RequestUri.Query);
            var report = new ClinicReportDTO { From = DateOnly.Parse(query["from"].ToString()), To = DateOnly.Parse(query["to"].ToString()), GeneratedAtUtc = new(2026, 10, 13, 1, 0, 0, DateTimeKind.Utc) };
            if (scenario != "empty")
            {
                report.TotalVisits = 2; report.NoShowCount = 1; report.ConcludedAppointments = 3; report.NoShowRate = 33.33m;
                report.TotalRevenue = report.ServiceRevenue = -50000; report.RefundedAmount = 50000;
                report.Doctors = [new() { DoctorId = 7, DoctorName = "Lê Thu Hà", TotalVisits = 2, NoShowCount = 1, NoShowRate = 33.33m, TotalRevenue = -50000, ServiceRevenue = -50000 }];
                report.RevenueItems = [new() { Category = "Service", SourceId = 4, Description = "Giá snapshot", Revenue = -50000 }];
            }
            return Task.FromResult(Response(report));
        }
        private static HttpResponseMessage Response<T>(T value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(new HttpResponseData<T> { StatusCode = 200, Content = value }) };
    }
}
