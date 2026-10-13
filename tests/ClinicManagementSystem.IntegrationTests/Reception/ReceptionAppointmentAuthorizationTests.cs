using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Appointment;
using ClinicManagementSystem.Application.DTOs.Notification;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace ClinicManagementSystem.IntegrationTests.Reception;

// Real JWT/MVC checks. Stubbed services deliberately never connect to a database.
public class ReceptionAppointmentAuthorizationTests : IClassFixture<ReceptionAppointmentApiFactory>
{
    private readonly ReceptionAppointmentApiFactory _factory;
    public ReceptionAppointmentAuthorizationTests(ReceptionAppointmentApiFactory factory) => _factory = factory;
    public static IEnumerable<object[]> Routes => new[]
    {
        new object[] { "GET", "/api/reception/appointments/affected" },
        new object[] { "PATCH", "/api/reception/appointments/1/reschedule" },
        new object[] { "PATCH", "/api/reception/appointments/1/cancel" }
    };
    public static IEnumerable<object[]> OtherRoles => Routes.SelectMany(r => new[] { "Patient", "Doctor", "Admin", "Pharmacist" }
        .Select(role => new object[] { r[0], r[1], role }));

    [Theory, MemberData(nameof(Routes))]
    public async Task Anonymous_IsUnauthorized(string method, string path)
    {
        using var client = Client(null); using var request = Request(method, path);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(request)).StatusCode);
    }
    [Theory, MemberData(nameof(OtherRoles))]
    public async Task OtherRoles_AreForbidden(string method, string path, string role)
    {
        using var client = Client(role); using var request = Request(method, path);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(request)).StatusCode);
    }
    [Theory, MemberData(nameof(Routes))]
    public async Task Receptionist_CanReachAllActions(string method, string path)
    {
        using var client = Client("Receptionist"); using var request = Request(method, path);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(request)).StatusCode);
    }
    [Theory, MemberData(nameof(Routes))]
    public async Task InvalidActorClaim_IsUnauthorized(string method, string path)
    {
        using var client = Client("Receptionist", "0"); using var request = Request(method, path);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(request)).StatusCode);
    }
    [Theory]
    [InlineData("GET", "/api/reception/appointments/affected?page=0", null)]
    [InlineData("GET", "/api/reception/appointments/affected?pageSize=101", null)]
    [InlineData("GET", "/api/reception/appointments/affected?from=2026-10-20&to=2026-10-19", null)]
    [InlineData("GET", "/api/reception/appointments/affected?from=2026-01-01&to=2026-10-19", null)]
    [InlineData("PATCH", "/api/reception/appointments/1/cancel", "{}")]
    [InlineData("PATCH", "/api/reception/appointments/1/reschedule", "{}")]
    [InlineData("PATCH", "/api/reception/appointments/1/reschedule", "{\"reason\":\"Test\",\"expectedVersion\":\"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA\",\"newStartTime\":\"2026-10-20T09:00:00Z\"}")]
    [InlineData("PATCH", "/api/reception/appointments/1/reschedule", "{\"reason\":\"Test\",\"expectedVersion\":\"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA\",\"newStartTime\":\"2026-10-20T09:00:00+07:00\"}")]
    public async Task InvalidBodyOrQuery_IsRejectedByMvc(string method, string path, string? body)
    {
        using var client = Client("Receptionist");
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (body != null) request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(request)).StatusCode);
    }
    [Theory]
    [InlineData("Doctor", 200)]
    [InlineData("Receptionist", 200)]
    [InlineData("Patient", 200)]
    [InlineData("Admin", 403)]
    [InlineData("Pharmacist", 403)]
    public async Task Notifications_UseCurrentActorAndAllowedRoles(string role, int status)
    {
        using var client = Client(role, "42");
        using var response = await client.GetAsync("/api/notifications?userId=999");
        Assert.Equal(status, (int)response.StatusCode);
        if (status == 200)
        {
            var result = await response.Content.ReadFromJsonAsync<HttpResponseData<List<NotificationDTO>>>();
            Assert.Equal("user:42", Assert.Single(result!.Content!).Content);
        }
    }
    [Theory]
    [InlineData("reschedule")]
    [InlineData("cancel")]
    public async Task PatientEndpoints_StillRejectReceptionist(string operation)
    {
        using var client = Client("Receptionist"); using var request = Request("PATCH", $"/api/appointments/1/{operation}");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(request)).StatusCode);
    }

    private HttpClient Client(string? role, string actorId = "7")
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new("https://localhost") });
        if (role != null)
        {
            var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ReceptionAppointmentApiFactory.Key)), SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken("ReceptionTests", "ReceptionTests", new[]
                { new Claim(ClaimTypes.NameIdentifier, actorId), new Claim(ClaimTypes.Role, role) }, expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: credentials);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        }
        return client;
    }
    private static HttpRequestMessage Request(string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method == "PATCH") request.Content = path.EndsWith("reschedule")
            ? JsonContent.Create(new ReceptionRescheduleAppointmentRequestDTO { ExpectedVersion = new('A', 64), NewStartTime = new(2026, 10, 20, 9, 0, 0), Reason = "Test" })
            : JsonContent.Create(new ReceptionCancelAppointmentRequestDTO { ExpectedVersion = new('A', 64), Reason = "Test" });
        return request;
    }
}

public class ReceptionAppointmentApiFactory : WebApplicationFactory<Program>
{
    public const string Key = "ReceptionTests_SecretKey_AtLeast32Characters_2026!";
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:ClinicDb", "Server=localhost;Database=UnusedReceptionTest;Integrated Security=true;TrustServerCertificate=true");
        builder.UseSetting("Jwt:Key", Key); builder.UseSetting("Jwt:Issuer", "ReceptionTests"); builder.UseSetting("Jwt:Audience", "ReceptionTests");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAccountAccessValidationService>();
            services.AddSingleton<IAccountAccessValidationService, ClinicManagementSystem.IntegrationTests.TestAccountAccessValidationService>();
            services.RemoveAll<IReceptionAffectedAppointmentService>();
            services.AddSingleton<IReceptionAffectedAppointmentService, StubReceptionService>();
            services.RemoveAll<INotificationService>(); services.AddSingleton<INotificationService, StubNotificationService>();
        });
    }
    private sealed class StubReceptionService : IReceptionAffectedAppointmentService
    {
        public Task<HttpResponseData<PagedResult<ReceptionAffectedAppointmentDTO>>> GetAffectedAsync(ReceptionAffectedQueryDTO query)
            => Result(new PagedResult<ReceptionAffectedAppointmentDTO>());
        public Task<HttpResponseData<ReceptionAffectedAppointmentDTO>> RescheduleAsync(int id, ReceptionRescheduleAppointmentRequestDTO request, int actorId)
            => Result(new ReceptionAffectedAppointmentDTO { Id = id });
        public Task<HttpResponseData<ReceptionAffectedAppointmentDTO>> CancelAsync(int id, ReceptionCancelAppointmentRequestDTO request, int actorId)
            => Result(new ReceptionAffectedAppointmentDTO { Id = id });
    }
    private sealed class StubNotificationService : INotificationService
    {
        public Task<HttpResponseData<List<NotificationDTO>>> GetMyNotificationsAsync(int currentUserId)
            => Result(new List<NotificationDTO> { new() { Id = 1, Content = $"user:{currentUserId}" } });
    }
    private static Task<HttpResponseData<T>> Result<T>(T content) => Task.FromResult(new HttpResponseData<T> { StatusCode = 200, Content = content });
}
