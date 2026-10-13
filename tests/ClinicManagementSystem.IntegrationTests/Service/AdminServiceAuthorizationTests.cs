using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Service;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace ClinicManagementSystem.IntegrationTests.AdminServices;

// Real JWT, routes, model binding and validation. Application stubs avoid an external DB.
public class AdminServiceAuthorizationTests(AdminServiceApiFactory factory) : IClassFixture<AdminServiceApiFactory>
{
    private static readonly (string Method, string Path, int Status)[] Routes =
    {
        ("GET", "/api/admin/services", 200), ("GET", "/api/admin/services/5", 200),
        ("POST", "/api/admin/services", 201), ("PUT", "/api/admin/services/5", 200),
        ("PATCH", "/api/admin/services/5/deactivate", 200)
    };
    public static IEnumerable<object[]> AccessCases()
    {
        foreach (var route in Routes)
        foreach (var role in new string?[] { null, "Patient", "Doctor", "Receptionist", "Pharmacist", "Admin" })
            yield return new object[] { route.Method, route.Path, role!, role == null ? 401 : role == "Admin" ? route.Status : 403 };
    }

    [Theory]
    [MemberData(nameof(AccessCases))]
    public async Task EveryAdminEndpoint_EnforcesJwtAndAdminRole(string method, string path, string? role, int status)
    {
        using var client = Client(role);
        using var request = Request(method, path);
        using var response = await client.SendAsync(request);
        Assert.Equal(status, (int)response.StatusCode);
        if (role == "Admin" && method != "GET")
        {
            var body = await response.Content.ReadFromJsonAsync<HttpResponseData<AdminServiceDTO>>();
            Assert.Equal(5, body!.Content!.Id);
            Assert.Equal(status, body.StatusCode);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("not-a-number")]
    public async Task Admin_InvalidUserClaimCannotReachAnyAction(string? actor)
    {
        using var client = Client("Admin", actor);
        foreach (var route in Routes)
        {
            using var request = Request(route.Method, route.Path);
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [Theory]
    [InlineData("POST", "/api/admin/services", "{}")]
    [InlineData("POST", "/api/admin/services", "{\"code\":\"DV\",\"name\":\"Test\",\"durationMinutes\":15}")]
    [InlineData("POST", "/api/admin/services", "{\"code\":\"DV\",\"name\":\"Test\",\"price\":0}")]
    [InlineData("PUT", "/api/admin/services/5", "{\"code\":\"DV\",\"name\":\"Test\",\"price\":1.001,\"durationMinutes\":15}")]
    [InlineData("POST", "/api/admin/services", "{\"code\":\"DV\",\"name\":\"Test\",\"price\":10000000000,\"durationMinutes\":15}")]
    [InlineData("POST", "/api/admin/services", "{\"code\":\"DV\",\"name\":\"Test\",\"price\":-1,\"durationMinutes\":15}")]
    [InlineData("POST", "/api/admin/services", "{\"code\":\"DV\",\"name\":\"Test\",\"price\":10,\"durationMinutes\":0}")]
    [InlineData("POST", "/api/admin/services", "{\"code\":\"DV\",\"name\":\"Test\",\"price\":10,\"durationMinutes\":1.5}")]
    [InlineData("POST", "/api/admin/services", "{\"code\":\"DV\",\"name\":\"Test\",\"price\":10,\"durationMinutes\":15,\"specialtyId\":0}")]
    [InlineData("GET", "/api/admin/services?page=0", null)]
    [InlineData("GET", "/api/admin/services?pageSize=101", null)]
    [InlineData("GET", "/api/admin/services?specialtyId=0", null)]
    [InlineData("GET", "/api/admin/services?active=invalid", null)]
    public async Task InvalidInput_IsRejectedByMvc(string method, string path, string? json)
    {
        using var client = Client("Admin");
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (json != null) request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(null, 401)]
    [InlineData("Doctor", 200)]
    [InlineData("Admin", 403)]
    [InlineData("Patient", 403)]
    public async Task ExistingDoctorCatalog_PreservesItsRoleScope(string? role, int status)
    {
        using var client = Client(role);
        using var response = await client.GetAsync("/api/services");
        Assert.Equal(status, (int)response.StatusCode);
    }

    [Fact]
    public async Task Deactivate_RequiresNoBodyAndReturnsInactiveStatus()
    {
        using var client = Client("Admin");
        using var response = await client.PatchAsync("/api/admin/services/5/deactivate", null);
        var body = await response.Content.ReadFromJsonAsync<HttpResponseData<AdminServiceDTO>>();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(body!.Content!.IsActive);
    }

    private HttpClient Client(string? role, string? actor = "7")
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        if (role != null)
        {
            var claims = new List<Claim> { new(ClaimTypes.Role, role) };
            if (actor != null) claims.Add(new(ClaimTypes.NameIdentifier, actor));
            var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AdminServiceApiFactory.Key)), SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken("AdminServiceTests", "AdminServiceTests", claims,
                expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: credentials);
            client.DefaultRequestHeaders.Authorization = new("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        }
        return client;
    }
    private static HttpRequestMessage Request(string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is "POST" or "PUT") request.Content = JsonContent.Create(new SaveServiceRequestDTO
        { Code = "DV001", Name = "Test", Price = 250000, DurationMinutes = 15 });
        return request;
    }
}

public class AdminServiceApiFactory : WebApplicationFactory<Program>
{
    public const string Key = "AdminServiceTests_Key_AtLeast32Characters_2026!";
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:ClinicDb", "Server=localhost;Database=UnusedServiceTest;Integrated Security=true;TrustServerCertificate=true");
        builder.UseSetting("Jwt:Key", Key); builder.UseSetting("Jwt:Issuer", "AdminServiceTests"); builder.UseSetting("Jwt:Audience", "AdminServiceTests");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAdminServiceCatalogService>(); services.AddSingleton<IAdminServiceCatalogService, StubAdminServices>();
            services.RemoveAll<IServiceCatalogService>(); services.AddSingleton<IServiceCatalogService, StubDoctorServices>();
        });
    }
    private sealed class StubAdminServices : IAdminServiceCatalogService
    {
        public Task<HttpResponseData<PagedResult<AdminServiceDTO>>> GetListAsync(AdminServiceQueryDTO query)
            => Task.FromResult(new HttpResponseData<PagedResult<AdminServiceDTO>> { StatusCode = 200, Content = new() });
        public Task<HttpResponseData<AdminServiceDTO>> GetByIdAsync(int id) => Result(200);
        public Task<HttpResponseData<AdminServiceDTO>> CreateAsync(SaveServiceRequestDTO request, int actorId) => Result(201);
        public Task<HttpResponseData<AdminServiceDTO>> UpdateAsync(int id, SaveServiceRequestDTO request, int actorId) => Result(200);
        public Task<HttpResponseData<AdminServiceDTO>> DeactivateAsync(int id, int actorId) => Result(200, false);
        private static Task<HttpResponseData<AdminServiceDTO>> Result(int status, bool active = true)
            => Task.FromResult(new HttpResponseData<AdminServiceDTO> { StatusCode = status, Content = new() { Id = 5, IsActive = active } });
    }
    private sealed class StubDoctorServices : IServiceCatalogService
    {
        public Task<HttpResponseData<List<ServiceDTO>>> GetActiveServicesAsync()
            => Task.FromResult(new HttpResponseData<List<ServiceDTO>> { StatusCode = 200, Content = new() });
    }
}
