using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Audit;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace ClinicManagementSystem.IntegrationTests.Audit;

// Exercises real JWT, MVC query validation and routing without a database connection.
public class AdminAuditLogAuthorizationTests(AdminAuditLogApiFactory factory) : IClassFixture<AdminAuditLogApiFactory>
{
    private static readonly string[] Routes = ["/api/admin/audit-logs", "/api/admin/audit-logs/7", "/api/admin/audit-logs/users", "/api/admin/audit-logs/accesses", "/api/admin/audit-logs/accesses/7"];
    public static IEnumerable<object[]> AccessCases()
    {
        foreach (var route in Routes)
        foreach (var role in new string?[] { null, "Patient", "Doctor", "Receptionist", "Pharmacist", "Admin" })
            yield return [route, role!, role == null ? 401 : role == "Admin" ? 200 : 403];
    }

    [Theory]
    [MemberData(nameof(AccessCases))]
    public async Task EveryReadRoute_RequiresAuthenticatedAdmin(string route, string? role, int status)
    {
        using var client = Client(role); using var result = await client.GetAsync(route);
        Assert.Equal(status, (int)result.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("invalid")]
    public async Task PositiveActorRequiredOnAllRoutes(string? actor)
    {
        using var client = Client("Admin", actor);
        foreach (var route in Routes)
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(route)).StatusCode);
    }

    [Theory]
    [InlineData("?from=2026-10-01")]
    [InlineData("?to=2026-10-13")]
    [InlineData("?from=invalid&to=2026-10-13")]
    [InlineData("?from=2026-10-13&to=2026-10-01")]
    [InlineData("?from=2026-01-01&to=2027-01-02")]
    [InlineData("?from=0001-01-01&to=0001-01-01")]
    [InlineData("?from=9999-12-31&to=9999-12-31")]
    [InlineData("?userId=0")]
    [InlineData("?entityId=-1")]
    [InlineData("?page=0")]
    [InlineData("?pageSize=101")]
    [InlineData("?pageSize=0")]
    [InlineData("?userId=abc")]
    public async Task InvalidListQuery_Returns400Json(string query)
    {
        using var client = Client("Admin"); using var response = await client.GetAsync(Routes[0] + query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("json", response.Content.Headers.ContentType!.MediaType!);
        using var grouped = await client.GetAsync("/api/admin/audit-logs/accesses" + query);
        Assert.Equal(HttpStatusCode.BadRequest, grouped.StatusCode);
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?pageSize=101")]
    [InlineData("?pageSize=0")]
    public async Task InvalidUserQuery_Returns400(string query)
    {
        using var client = Client("Admin");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(Routes[2] + query)).StatusCode);
    }

    [Fact]
    public async Task OversizedFilters_AreRejectedByMvc()
    {
        using var client = Client("Admin");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(Routes[0] + "?entity=" + new string('a', 129))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(Routes[2] + "?keyword=" + new string('a', 101))).StatusCode);
    }

    [Fact]
    public async Task GroupedRoutes_ReturnAccessCountsAndOriginalTargets_WithoutDetailsInList()
    {
        using var client = Client("Admin");
        using var list = await client.GetAsync("/api/admin/audit-logs/accesses");
        Assert.True(list.Headers.CacheControl!.NoStore);
        Assert.DoesNotContain("details", await list.Content.ReadAsStringAsync());
        var page = (await list.Content.ReadFromJsonAsync<HttpResponseData<PagedResult<AuditAccessDTO>>>())!.Content!;
        var access = Assert.Single(page.Items); Assert.Equal(4, access.EntityCount); Assert.Equal("request-1", access.RequestId);
        using var detail = await client.GetAsync("/api/admin/audit-logs/accesses/7");
        Assert.True(detail.Headers.CacheControl!.NoStore);
        var value = (await detail.Content.ReadFromJsonAsync<HttpResponseData<AuditAccessDetailDTO>>())!.Content!;
        Assert.Equal(4, value.Entries.Count); Assert.Equal(4, value.EntryCount);
    }

    [Fact]
    public async Task List_PreservesFilterAndPagination_AndDoesNotExposeDetails()
    {
        using var client = Client("Admin");
        using var response = await client.GetAsync(Routes[0] + "?entity=medical_records&userId=7&entityId=12&from=2026-10-01&to=2026-10-13&page=2&pageSize=10");
        Assert.True(response.Headers.CacheControl!.NoStore);
        var json = await response.Content.ReadAsStringAsync(); Assert.DoesNotContain("details", json);
        var page = (await response.Content.ReadFromJsonAsync<HttpResponseData<PagedResult<AuditLogDTO>>>())!.Content!;
        Assert.Equal(2, page.Page); Assert.Equal(10, page.PageSize); Assert.Equal(11, page.TotalItems);
        var row = Assert.Single(page.Items); Assert.Equal("medical_records", row.EntityName); Assert.Equal(7, row.UserId);
        Assert.Equal(12, row.EntityId); Assert.True(row.IsUtcTimestamp); Assert.Equal(DateTimeKind.Utc, row.OccurredAtUtc.Kind);
    }

    [Fact]
    public async Task DetailAndUserOptions_HaveCorrectEnvelope_AndNoCache()
    {
        using var client = Client("Admin"); using var detailResponse = await client.GetAsync(Routes[1]);
        var detail = (await detailResponse.Content.ReadFromJsonAsync<HttpResponseData<AuditLogDetailDTO>>())!.Content!;
        Assert.Equal(7, detail.Id); Assert.Equal("legacy metadata", detail.Details); Assert.True(detailResponse.Headers.CacheControl!.NoStore);
        using var usersResponse = await client.GetAsync(Routes[2] + "?keyword=doctor&page=2&pageSize=25");
        var users = (await usersResponse.Content.ReadFromJsonAsync<HttpResponseData<PagedResult<AuditUserOptionDTO>>>())!.Content!;
        Assert.Equal(2, users.Page); Assert.Equal(25, users.PageSize); Assert.Equal("doctor", Assert.Single(users.Items).Username);
        Assert.True(usersResponse.Headers.CacheControl!.NoStore);
    }

    [Theory]
    [InlineData("/api/admin/audit-logs/999", 404)]
    [InlineData("/api/admin/audit-logs/500", 500)]
    [InlineData("/api/admin/audit-logs?userId=500", 500)]
    [InlineData("/api/admin/audit-logs/users?keyword=500", 500)]
    public async Task ServiceErrors_KeepStatusAndJsonEnvelope(string route, int code)
    {
        using var client = Client("Admin"); using var response = await client.GetAsync(route);
        Assert.Equal(code, (int)response.StatusCode); Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
    }

    [Theory]
    [InlineData("POST", "/api/admin/audit-logs")]
    [InlineData("PUT", "/api/admin/audit-logs/7")]
    [InlineData("PATCH", "/api/admin/audit-logs/7")]
    [InlineData("DELETE", "/api/admin/audit-logs/7")]
    public async Task AuditApi_OffersNoWriteOrDeleteRoutes(string method, string route)
    {
        using var client = Client("Admin"); using var request = new HttpRequestMessage(new HttpMethod(method), route);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    private HttpClient Client(string? role, string? actor = "7")
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new("https://localhost") });
        if (role == null) return client;
        var claims = new List<Claim> { new(ClaimTypes.Role, role) };
        if (actor != null) claims.Add(new(ClaimTypes.NameIdentifier, actor));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AdminAuditLogApiFactory.Key));
        var jwt = new JwtSecurityToken("AdminAuditTests", "AdminAuditTests", claims, expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new("Bearer", new JwtSecurityTokenHandler().WriteToken(jwt));
        return client;
    }
}

public class AdminAuditLogApiFactory : WebApplicationFactory<Program>
{
    public const string Key = "AdminAuditTests_Key_AtLeast32Characters_2026!";
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:ClinicDb", "Server=localhost;Database=UnusedAuditHttpTests;Integrated Security=true;TrustServerCertificate=true");
        builder.UseSetting("Jwt:Key", Key); builder.UseSetting("Jwt:Issuer", "AdminAuditTests"); builder.UseSetting("Jwt:Audience", "AdminAuditTests");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAccountAccessValidationService>();
            services.AddSingleton<IAccountAccessValidationService, TestAccountAccessValidationService>();
            services.RemoveAll<IAdminAuditLogService>(); services.AddSingleton<IAdminAuditLogService, StubAuditService>();
        });
    }

    private sealed class StubAuditService : IAdminAuditLogService
    {
        public Task<HttpResponseData<PagedResult<AuditAccessDTO>>> GetAccessesAsync(AuditLogQueryDTO q)
            => Task.FromResult(new HttpResponseData<PagedResult<AuditAccessDTO>> { StatusCode = 200,
                Content = new() { Page = q.Page, PageSize = q.PageSize, TotalItems = 1, TotalPages = 1,
                    Items = [new() { Id = 7, UserId = q.UserId, EntityName = "medical_records", Action = "VIEW_RECORD_HISTORY", RequestId = "request-1", EntryCount = 4, EntityCount = 4 }] } });
        public Task<HttpResponseData<AuditAccessDetailDTO>> GetAccessByIdAsync(int id)
            => Task.FromResult(new HttpResponseData<AuditAccessDetailDTO> { StatusCode = id == 999 ? 404 : 200,
                Content = id == 999 ? null : new() { Id = id, Action = "VIEW_RECORD_HISTORY", EntityName = "medical_records", RequestId = "request-1", EntryCount = 4, EntityCount = 4,
                    Entries = Enumerable.Range(1, 4).Select(x => new AuditLogDTO { Id = x, EntityId = x, EntityName = "medical_records", Action = "VIEW_RECORD_HISTORY" }).ToList() } });
        public Task<HttpResponseData<PagedResult<AuditLogDTO>>> GetListAsync(AuditLogQueryDTO q)
            => Task.FromResult(new HttpResponseData<PagedResult<AuditLogDTO>> { StatusCode = q.UserId == 500 ? 500 : 200,
                Content = q.UserId == 500 ? null : new() { Page = q.Page, PageSize = q.PageSize, TotalItems = 11, TotalPages = (11 + q.PageSize - 1) / q.PageSize,
                    Items = [new() { Id = 7, UserId = q.UserId, EntityId = q.EntityId, EntityName = q.Entity ?? "patients", Action = "VIEW_RECORD", OccurredAtUtc = new(2026, 10, 13, 1, 2, 3, DateTimeKind.Utc), IsUtcTimestamp = true, Succeeded = true }] } });
        public Task<HttpResponseData<AuditLogDetailDTO>> GetByIdAsync(int id)
            => Task.FromResult(new HttpResponseData<AuditLogDetailDTO> { StatusCode = id == 999 ? 404 : id == 500 ? 500 : 200,
                Content = id is 999 or 500 ? null : new() { Id = id, Action = "VIEW_PATIENT", EntityName = "patients", Details = "legacy metadata" } });
        public Task<HttpResponseData<PagedResult<AuditUserOptionDTO>>> GetUsersAsync(AuditUserQueryDTO q)
            => Task.FromResult(new HttpResponseData<PagedResult<AuditUserOptionDTO>> { StatusCode = q.Keyword == "500" ? 500 : 200,
                Content = q.Keyword == "500" ? null : new() { Page = q.Page, PageSize = q.PageSize, TotalItems = 26, TotalPages = (26 + q.PageSize - 1) / q.PageSize,
                    Items = [new() { Id = 7, Username = q.Keyword ?? "doctor", FullName = "Hà" }] } });
    }
}
