using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Audit;
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

public class AdminAuditLogsRenderingTests
{
    [Theory]
    [InlineData("data")]
    [InlineData("empty")]
    [InlineData("error")]
    [InlineData("dirty")]
    [InlineData("detail")]
    [InlineData("lookup")]
    [InlineData("focus")]
    [InlineData("group")]
    [InlineData("file")]
    public async Task ActualRazor_RendersReadOnlyFiltersPagingDetailsAndSafeErrors(string scenario)
    {
        var factory = new Mock<IHttpClientFactory>(); var handler = new FixtureHandler(scenario);
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(handler, false) { BaseAddress = new("http://fixture.local") });
        var user = new UserStateService(null!, factory.Object, null!, new CustomAuthenticationStateProvider());
        typeof(UserStateService).GetProperty(nameof(UserStateService.AccessToken))!.SetValue(user, "fixture-token");
        var state = new AdminAuditLogStateService(new AuthorizedApiService(factory.Object, user)); var activator = new CapturingActivator();
        using var services = new ServiceCollection().AddLogging().AddSingleton(state).AddSingleton(user)
            .AddSingleton(Mock.Of<IJSRuntime>()).AddSingleton<NavigationManager>(new FixtureNavigation()).AddSingleton<IComponentActivator>(activator).BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        var raw = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = await renderer.RenderComponentAsync<AuditLogs>();
            var filter = (AuditLogQueryDTO)typeof(AuditLogs).GetField("_filter", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(activator.Page)!;
            if (scenario == "dirty") filter.EntityId = 999;
            if (scenario == "detail") await state.LoadDetailAsync(9);
            if (scenario is "group" or "file") await state.LoadDetailAsync(9);
            if (scenario == "lookup")
            {
                filter.UserId = 7; typeof(AuditLogs).GetMethod("RememberUser", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(activator.Page, null);
                await state.LoadUsersAsync("other", 2);
            }
            if (scenario == "focus")
            {
                var pendingFocus = typeof(AuditLogs).GetField("_pendingFocus", BindingFlags.Instance | BindingFlags.NonPublic)!;
                await (Task)typeof(AuditLogs).GetMethod("OpenDetail", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(activator.Page, [9])!;
                Assert.Equal("audit-detail-heading", pendingFocus.GetValue(activator.Page));
                typeof(AuditLogs).GetMethod("CloseDetail", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(activator.Page, null);
                Assert.Equal("audit-detail-9", pendingFocus.GetValue(activator.Page)); Assert.Null(state.Detail);
            }
            if (scenario is "dirty" or "detail" or "lookup" or "focus" or "group" or "file") typeof(ComponentBase).GetMethod("StateHasChanged", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(activator.Page, null);
            return root.ToHtmlString();
        });
        var html = WebUtility.HtmlDecode(raw);
        Assert.DoesNotMatch(@">\s*\}\s*<", raw);
        Assert.Contains("admin-audit-logs-page", html); Assert.Contains("id=\"audit-from\"", html); Assert.Contains("id=\"audit-to\"", html);
        Assert.Contains("Tất cả đối tượng", html); Assert.Contains("Tất cả người dùng", html); Assert.Contains("không có chức năng sửa hoặc xóa nhật ký", html);
        if (scenario == "error")
        {
            Assert.Contains("id=\"audit-error\"", html); Assert.DoesNotContain("id=\"audit-detail-9\"", html); Assert.DoesNotContain("id=\"audit-results-heading\"", html);
        }
        else if (scenario == "empty") { Assert.Contains("Không có nhật ký phù hợp", html); Assert.Contains("0 lượt truy cập", html); }
        else if (scenario is "group" or "file")
        {
            Assert.Contains("2 lượt truy cập", html); Assert.DoesNotContain("Thông tin kỹ thuật", html);
            Assert.DoesNotContain("audit-technical-details", raw); Assert.DoesNotContain("returnedCount", html);
            Assert.DoesNotContain("Phương thức", html); Assert.DoesNotContain("Mã yêu cầu", html); Assert.DoesNotContain("GET", html); Assert.DoesNotContain("HTTP 200", html); Assert.DoesNotContain("req-one", html); Assert.Contains("Thành công", html);
            if (scenario == "group")
            {
                Assert.Contains("Xem lịch sử khám", html); Assert.Contains("4 bệnh án", html);
                foreach (var id in new[] { 1, 8, 9, 10 }) Assert.Contains($"Bệnh án #{id}", html);
                Assert.Contains("4 log gốc", html);
            }
            else { Assert.Contains("Tải tệp đính kèm", html); Assert.Contains("Tệp đính kèm #65", html); Assert.Contains("1 log gốc", html); }
        }
        else
        {
            Assert.Contains("Xem bệnh án", html); Assert.Contains("Bệnh án #1204", html); Assert.Contains("10.0.0.15", html);
            Assert.Contains("13/10/2026 09:12:00", html); Assert.Contains("13/10/2026 03:00:00", html); Assert.Contains("Giờ lưu cũ — chưa xác định múi giờ", html);
            Assert.Contains("Tài khoản #8 (không còn tồn tại)", html); Assert.Contains("Không thành công", html);
            if (scenario == "dirty") { Assert.Contains("Bộ lọc đã thay đổi", html); Assert.Matches("id=\"audit-page-next\"[^>]*disabled", html); }
            if (scenario == "detail") { Assert.Contains("id=\"audit-detail-heading\"", html); Assert.Contains("id=\"audit-close-detail\"", html); Assert.DoesNotContain("<script>alert", raw); Assert.DoesNotContain("&lt;script&gt;", raw); Assert.DoesNotContain("/api/records/1204", html); }
            if (scenario == "lookup") { Assert.Contains("bacsi.a — Nguyễn Văn A (#7)", html); Assert.Contains("user.other", html); Assert.Contains("60 người dùng", html); }
        }
    }

    [Fact]
    public void AuditLogsRoute_RequiresAdmin()
    {
        Assert.Equal("Admin", typeof(AuditLogs).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("/admin/audit-logs", typeof(AuditLogs).GetCustomAttribute<RouteAttribute>()!.Template);
    }
    private sealed class FixtureNavigation : NavigationManager { public FixtureNavigation() => Initialize("http://fixture.local/", "http://fixture.local/admin/audit-logs"); }
    private sealed class CapturingActivator : IComponentActivator
    {
        public AuditLogs? Page { get; private set; }
        public IComponent CreateInstance(Type type) { var item = (IComponent)Activator.CreateInstance(type)!; if (item is AuditLogs page) Page = page; return item; }
    }
    private sealed class FixtureHandler(string scenario) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/users"))
            {
                var page = int.Parse(QueryHelpers.ParseQuery(request.RequestUri.Query)["page"].ToString());
                return Task.FromResult(Response(new PagedResult<AuditUserOptionDTO> { Page = page, PageSize = 50, TotalItems = 60, TotalPages = 2,
                    Items = page == 2 ? [new() { Id = 25, Username = "user.other" }] : [new() { Id = 7, Username = "bacsi.a", FullName = "Nguyễn Văn A" }, new() { Id = 8 }] }));
            }
            if (scenario is "group" or "file")
            {
                var file = scenario == "file"; var ids = file ? new[] { 65 } : new[] { 1, 8, 9, 10 };
                var detail = new AuditAccessDetailDTO { Id = 9, UserId = 7, Username = "bacsi.a", FullName = "Nguyễn Văn A",
                    Action = file ? "DOWNLOAD_ATTACHMENT" : "VIEW_RECORD_HISTORY", EntityName = file ? "attachments" : "medical_records",
                    EntityId = file ? 65 : null, EntityCount = ids.Length, EntryCount = ids.Length, RequestId = "req-one",
                    Succeeded = true, IsUtcTimestamp = true, OccurredAtUtc = new(2026, 10, 13, 2, 12, 0),
                    Metadata = new() { Method = "GET", StatusCode = 200 }, Details = "{\"requestId\":\"req-one\",\"returnedCount\":0}",
                    Entries = ids.Select((id, index) => new AuditLogDTO { Id = 9 + index, EntityId = id, EntityName = file ? "attachments" : "medical_records" }).ToList() };
                if (request.RequestUri.AbsolutePath.EndsWith("/9")) return Task.FromResult(Response(detail));
                return Task.FromResult(Response(new PagedResult<AuditAccessDTO> { Page = 1, PageSize = 20, TotalItems = 2, TotalPages = 1, Items = [detail,
                    new() { Id = 99, UserId = 7, Action = detail.Action, EntityName = detail.EntityName, EntryCount = ids.Length, EntityCount = ids.Length, RequestId = "req-two" }] }));
            }
            if (request.RequestUri.AbsolutePath.EndsWith("/9")) return Task.FromResult(Response(new AuditAccessDetailDTO { Id = 9, UserId = 7, Username = "bacsi.a", Action = "VIEW_RECORD", EntityName = "medical_records", EntityId = 1204, Succeeded = true, IsUtcTimestamp = true,
                OccurredAtUtc = new(2026, 10, 13, 2, 12, 0), Details = "<script>alert('bad')</script>\n{\"path\":\"/api/records/1204\"}" }));
            if (scenario == "error") return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            var logs = new PagedResult<AuditAccessDTO> { Page = 1, PageSize = 20, TotalItems = scenario == "empty" ? 0 : 21, TotalPages = scenario == "empty" ? 0 : 2 };
            if (scenario != "empty") logs.Items = [new() { Id = 9, UserId = 7, Username = "bacsi.a", FullName = "Nguyễn Văn A", Action = "VIEW_RECORD", EntityName = "medical_records", EntityId = 1204, IpAddress = "10.0.0.15", Succeeded = true, IsUtcTimestamp = true, OccurredAtUtc = new(2026, 10, 13, 2, 12, 0) },
                new() { Id = 10, UserId = 8, Action = "VIEW_RECORD", EntityName = "medical_records", EntityId = 1205, Succeeded = false, OccurredAtUtc = new(2026, 10, 13, 3, 0, 0) }];
            return Task.FromResult(Response(logs));
        }
        private static HttpResponseMessage Response<T>(T value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(new HttpResponseData<T> { StatusCode = 200, Content = value }) };
    }
}
