using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Specialty;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace ClinicManagementSystem.IntegrationTests.AdminSpecialties;

// Real HTTP routing, JWT validation and MVC validation; no external database needed.
public class AdminSpecialtyAuthorizationTests : IClassFixture<AdminSpecialtyApiFactory>
{
    private readonly AdminSpecialtyApiFactory _factory;
    public AdminSpecialtyAuthorizationTests(AdminSpecialtyApiFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/api/specialties")]
    [InlineData("/api/specialties?active=true")]
    [InlineData("/api/specialties?active=false")]
    public async Task SpecialtyList_Anonymous_CanRead(string path)
    {
        using var client = Client(null);
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<HttpResponseData<List<SpecialtyDTO>>>();
        Assert.NotNull(result);
        Assert.Equal("Tai mũi họng", Assert.Single(result.Content!).Name);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Patient")]
    [InlineData("Receptionist")]
    [InlineData("Doctor")]
    [InlineData("Pharmacist")]
    public async Task SpecialtyList_AuthenticatedUser_CanRead(string role)
    {
        using var client = Client(role);
        using var response = await client.GetAsync("/api/specialties?active=true");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<HttpResponseData<List<SpecialtyDTO>>>();
        Assert.NotNull(result);
        Assert.Equal("Tai mũi họng", Assert.Single(result.Content!).Name);
    }

    [Fact]
    public async Task SpecialtyList_InvalidToken_CanReadPublicList()
    {
        using var client = Client(null);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        using var response = await client.GetAsync("/api/specialties");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/admin/specialties")]
    [InlineData("POST", "/api/admin/specialties")]
    [InlineData("PUT", "/api/admin/specialties/1")]
    [InlineData("PATCH", "/api/admin/specialties/1/status")]
    public async Task Anonymous_IsUnauthorized(string method, string path)
    {
        using var client = Client(null);
        using var request = Request(method, path);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Patient", "GET", "/api/admin/specialties")]
    [InlineData("Doctor", "POST", "/api/admin/specialties")]
    [InlineData("Receptionist", "PUT", "/api/admin/specialties/1")]
    [InlineData("Pharmacist", "PATCH", "/api/admin/specialties/1/status")]
    public async Task OtherRoles_AreForbidden(string role, string method, string path)
    {
        using var client = Client(role);
        using var request = Request(method, path);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/admin/specialties", 200)]
    [InlineData("POST", "/api/admin/specialties", 201)]
    [InlineData("PUT", "/api/admin/specialties/1", 200)]
    [InlineData("PATCH", "/api/admin/specialties/1/status", 200)]
    public async Task Admin_CanReachActions(string method, string path, int status)
    {
        using var client = Client("Admin");
        using var request = Request(method, path);
        using var response = await client.SendAsync(request);
        Assert.Equal(status, (int)response.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/api/admin/specialties", "{}")]
    [InlineData("PATCH", "/api/admin/specialties/1/status", "{}")]
    [InlineData("GET", "/api/admin/specialties?page=0", null)]
    [InlineData("GET", "/api/admin/specialties?pageSize=101", null)]
    public async Task InvalidInput_IsRejectedByMvc(string method, string path, string? json)
    {
        using var client = Client("Admin");
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (json != null) request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private HttpClient Client(string? role)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        if (role != null)
        {
            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AdminSpecialtyApiFactory.Key)), SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken("AdminSpecialtyTests", "AdminSpecialtyTests",
                new[] { new Claim(ClaimTypes.NameIdentifier, "7"), new Claim(ClaimTypes.Role, role) },
                expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: credentials);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        }
        return client;
    }

    private static HttpRequestMessage Request(string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method == "POST" || method == "PUT")
            request.Content = JsonContent.Create(new SaveSpecialtyRequestDTO { Code = "ENT", Name = "Tai mũi họng" });
        else if (method == "PATCH")
            request.Content = JsonContent.Create(new SetSpecialtyStatusRequestDTO { IsActive = false });
        return request;
    }
}

public class AdminSpecialtyApiFactory : WebApplicationFactory<Program>
{
    public const string Key = "AdminSpecialtyTests_SecretKey_AtLeast32Characters_2026!";
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:ClinicDb", "Server=localhost;Database=UnusedAdminTest;Integrated Security=true;TrustServerCertificate=true");
        builder.UseSetting("Jwt:Key", Key);
        builder.UseSetting("Jwt:Issuer", "AdminSpecialtyTests");
        builder.UseSetting("Jwt:Audience", "AdminSpecialtyTests");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAccountAccessValidationService>();
            services.AddSingleton<IAccountAccessValidationService, ClinicManagementSystem.IntegrationTests.TestAccountAccessValidationService>();
            services.RemoveAll<IAdminSpecialtyService>();
            services.AddSingleton<IAdminSpecialtyService, StubSpecialtyService>();
            services.RemoveAll<ISpecialtyService>();
            services.AddSingleton<ISpecialtyService, StubSpecialtyListService>();
        });
    }

    private sealed class StubSpecialtyListService : ISpecialtyService
    {
        public Task<HttpResponseData<List<SpecialtyDTO>>> GetSpecialtiesAsync(bool? active)
            => Task.FromResult(new HttpResponseData<List<SpecialtyDTO>>
            {
                StatusCode = 200,
                Content = new List<SpecialtyDTO> { new() { Id = 1, Name = "Tai mũi họng" } }
            });
    }

    private sealed class StubSpecialtyService : IAdminSpecialtyService
    {
        public Task<HttpResponseData<PagedResult<AdminSpecialtyDTO>>> GetListAsync(AdminSpecialtyQueryDTO query)
            => Task.FromResult(new HttpResponseData<PagedResult<AdminSpecialtyDTO>> { StatusCode = 200, Content = new() });
        public Task<HttpResponseData<AdminSpecialtyDTO>> CreateAsync(SaveSpecialtyRequestDTO request, int actorId) => Result(201);
        public Task<HttpResponseData<AdminSpecialtyDTO>> UpdateAsync(int id, SaveSpecialtyRequestDTO request, int actorId) => Result(200);
        public Task<HttpResponseData<AdminSpecialtyDTO>> SetStatusAsync(int id, bool isActive, int actorId) => Result(200);
        private static Task<HttpResponseData<AdminSpecialtyDTO>> Result(int status)
            => Task.FromResult(new HttpResponseData<AdminSpecialtyDTO> { StatusCode = status, Content = new() { Id = 1 } });
    }
}
