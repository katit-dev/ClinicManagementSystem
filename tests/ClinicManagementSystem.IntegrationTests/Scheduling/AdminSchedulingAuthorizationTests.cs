using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Scheduling;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace ClinicManagementSystem.IntegrationTests.AdminScheduling;

// Real JWT and MVC pipeline, with only the scheduling service stubbed; never connects to a database.
public class AdminSchedulingAuthorizationTests : IClassFixture<AdminSchedulingApiFactory>
{
    private readonly AdminSchedulingApiFactory _factory;
    public AdminSchedulingAuthorizationTests(AdminSchedulingApiFactory factory) => _factory = factory;
    public static IEnumerable<object[]> Routes => new[]
    {
        new object[] { "GET", "/api/admin/scheduling/doctors", 200 },
        new object[] { "GET", "/api/admin/doctor-schedules", 200 },
        new object[] { "POST", "/api/admin/doctor-schedules", 201 },
        new object[] { "PUT", "/api/admin/doctor-schedules/1", 200 },
        new object[] { "GET", "/api/admin/time-off", 200 },
        new object[] { "POST", "/api/admin/time-off", 201 },
        new object[] { "DELETE", "/api/admin/time-off/1", 200 }
    };
    public static IEnumerable<object[]> OtherRoles => Routes.SelectMany(r => new[] { "Patient", "Doctor", "Receptionist", "Pharmacist" }
        .Select(role => new object[] { r[0], r[1], role }));
    public static IEnumerable<object[]> AnonymousRoutes => Routes.Select(r => new object[] { r[0], r[1] });

    [Theory, MemberData(nameof(AnonymousRoutes))]
    public async Task Anonymous_IsUnauthorized(string method, string path)
    {
        using var client = Client(null);
        using var request = Request(method, path);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory, MemberData(nameof(OtherRoles))]
    public async Task OtherRoles_AreForbidden(string method, string path, string role)
    {
        using var client = Client(role);
        using var request = Request(method, path);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory, MemberData(nameof(Routes))]
    public async Task Admin_CanReachActions(string method, string path, int status)
    {
        using var client = Client("Admin");
        using var request = Request(method, path);
        using var response = await client.SendAsync(request);
        Assert.Equal(status, (int)response.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/api/admin/doctor-schedules", "{}")]
    [InlineData("PUT", "/api/admin/doctor-schedules/1", "{}")]
    [InlineData("POST", "/api/admin/time-off", "{}")]
    [InlineData("GET", "/api/admin/doctor-schedules?page=0", null)]
    [InlineData("GET", "/api/admin/time-off?pageSize=101", null)]
    [InlineData("GET", "/api/admin/time-off?from=2026-10-20&to=2026-10-19", null)]
    [InlineData("GET", "/api/admin/time-off?doctorId=1&clinicWideOnly=true", null)]
    [InlineData("POST", "/api/admin/time-off", "{\"startAt\":\"2026-10-19T08:00:00Z\",\"endAt\":\"2026-10-19T10:00:00Z\",\"type\":0,\"reason\":\"test\"}")]
    [InlineData("POST", "/api/admin/time-off", "{\"startAt\":\"2026-10-19T08:00:00+07:00\",\"endAt\":\"2026-10-19T10:00:00+07:00\",\"type\":0,\"reason\":\"test\"}")]
    public async Task InvalidInput_IsRejectedByMvc(string method, string path, string? body)
    {
        using var client = Client("Admin");
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (body != null) request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/api/admin/doctor-schedules")]
    [InlineData("PUT", "/api/admin/doctor-schedules/1")]
    [InlineData("POST", "/api/admin/time-off")]
    [InlineData("DELETE", "/api/admin/time-off/1")]
    public async Task Mutation_RequiresValidActorClaim(string method, string path)
    {
        using var client = Client("Admin", actorId: "0");
        using var request = Request(method, path);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private HttpClient Client(string? role, string actorId = "7")
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        if (role != null)
        {
            var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AdminSchedulingApiFactory.Key)), SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken("SchedulingTests", "SchedulingTests",
                new[] { new Claim(ClaimTypes.NameIdentifier, actorId), new Claim(ClaimTypes.Role, role) },
                expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: credentials);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        }
        return client;
    }

    private static HttpRequestMessage Request(string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method == "POST" || method == "PUT")
            request.Content = path.Contains("doctor-schedules")
                ? JsonContent.Create(new ScheduleRequestDTO
                {
                    DoctorId = 1, DayOfWeek = 1, StartTime = new(8, 0), EndTime = new(17, 0),
                    SlotMinutes = 30, EffectiveFrom = new(2026, 10, 19)
                })
                : JsonContent.Create(new TimeOffRequestDTO
                {
                    StartAt = new DateTime(2026, 10, 19, 8, 0, 0), EndAt = new DateTime(2026, 10, 19, 10, 0, 0),
                    Type = 0, Reason = "test"
                });
        return request;
    }
}

public class AdminSchedulingApiFactory : WebApplicationFactory<Program>
{
    public const string Key = "SchedulingTests_SecretKey_AtLeast32Characters_2026!";
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:ClinicDb", "Server=localhost;Database=UnusedSchedulingTest;Integrated Security=true;TrustServerCertificate=true");
        builder.UseSetting("Jwt:Key", Key);
        builder.UseSetting("Jwt:Issuer", "SchedulingTests");
        builder.UseSetting("Jwt:Audience", "SchedulingTests");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAdminSchedulingService>();
            services.AddSingleton<IAdminSchedulingService, StubSchedulingService>();
        });
    }

    private sealed class StubSchedulingService : IAdminSchedulingService
    {
        public Task<HttpResponseData<List<AdminDoctorOptionDTO>>> GetDoctorsAsync() => Result(200, new List<AdminDoctorOptionDTO>());
        public Task<HttpResponseData<PagedResult<AdminDoctorScheduleDTO>>> GetSchedulesAsync(AdminScheduleQueryDTO query) => Result(200, new PagedResult<AdminDoctorScheduleDTO>());
        public Task<HttpResponseData<ScheduleChangeResultDTO>> SaveScheduleAsync(int? id, ScheduleRequestDTO request, int actorId) => Result(id.HasValue ? 200 : 201, new ScheduleChangeResultDTO());
        public Task<HttpResponseData<PagedResult<AdminDoctorTimeOffDTO>>> GetTimeOffsAsync(AdminTimeOffQueryDTO query) => Result(200, new PagedResult<AdminDoctorTimeOffDTO>());
        public Task<HttpResponseData<TimeOffChangeResultDTO>> CreateTimeOffAsync(TimeOffRequestDTO request, int actorId) => Result(201, new TimeOffChangeResultDTO());
        public Task<HttpResponseData<AdminDoctorTimeOffDTO>> DeleteTimeOffAsync(int id, int actorId) => Result(200, new AdminDoctorTimeOffDTO());
        private static Task<HttpResponseData<T>> Result<T>(int status, T content) => Task.FromResult(new HttpResponseData<T> { StatusCode = status, Content = content });
    }
}
