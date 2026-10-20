using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Doctor;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace ClinicManagementSystem.IntegrationTests.MedicalRecord;

public class DoctorMedicalRecordAuthorizationTests : IClassFixture<DoctorMedicalRecordApiFactory>
{
    private readonly DoctorMedicalRecordApiFactory _factory;
    public DoctorMedicalRecordAuthorizationTests(DoctorMedicalRecordApiFactory factory) => _factory = factory;

    [Theory]
    [InlineData(null, 401)]
    [InlineData("Patient", 403)]
    [InlineData("Admin", 403)]
    [InlineData("Receptionist", 403)]
    [InlineData("Pharmacist", 403)]
    [InlineData("Doctor", 200)]
    public async Task Route_RequiresDoctor(string? role, int expected)
    {
        using var client = Client(role);
        using var response = await client.GetAsync("/api/doctors/me/medical-records");
        Assert.Equal(expected, (int)response.StatusCode);
    }

    [Fact]
    public async Task BindsFilters_AndUsesActorClaim_InsteadOfDoctorQueryParameter()
    {
        using var client = Client("Doctor");
        using var response = await client.GetAsync("/api/doctors/me/medical-records?doctorId=999&keyword=Lieu&status=0&from=2026-10-01&to=2026-10-19&page=2&pageSize=5");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<HttpResponseData<PagedResult<DoctorMedicalRecordDTO>>>();
        var row = Assert.Single(data!.Content!.Items);
        Assert.Equal(42, row.Id);
        Assert.Equal("Lieu", row.PatientName);
        Assert.Equal((byte)0, row.Status);
        Assert.Equal(2, data.Content.Page);
        Assert.Equal(5, data.Content.PageSize);
        Assert.True(response.Headers.CacheControl!.NoStore);
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?pageSize=101")]
    [InlineData("?status=2")]
    [InlineData("?status=Draft")]
    [InlineData("?from=2026-10-19")]
    [InlineData("?from=2026-10-19&to=2026-10-01")]
    [InlineData("?from=2025-01-01&to=2026-10-01")]
    public async Task InvalidFilters_Return400(string query)
    {
        using var client = Client("Doctor");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/doctors/me/medical-records" + query)).StatusCode);
    }

    [Fact]
    public void AuditMapping_TracksOnlyReturnedMedicalRecordIds()
    {
        var endpoint = ClinicManagementSystem.Api.Middleware.MedicalAuditEndpoint.Resolve(new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor
            { ControllerName = "DoctorMedicalRecords", ActionName = "GetMyMedicalRecords" });
        var ids = endpoint!.GetReturnedIds(new HttpResponseData<PagedResult<DoctorMedicalRecordDTO>>
        {
            Content = new() { Page = 2, TotalItems = 100, Items = [new() { Id = 7, PatientId = 999 }, new() { Id = 8, PatientId = 999 }] }
        });
        Assert.Equal(new[] { 7, 8 }, ids);
        Assert.Equal("medical_records", endpoint.Entity);
    }

    private HttpClient Client(string? role)
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        if (role != null)
        {
            var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(DoctorMedicalRecordApiFactory.Key)), SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken("DoctorRecordTests", "DoctorRecordTests",
                [new(ClaimTypes.NameIdentifier, "42"), new(ClaimTypes.Role, role)],
                expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: credentials);
            client.DefaultRequestHeaders.Authorization = new("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        }
        return client;
    }
}

public class DoctorMedicalRecordApiFactory : WebApplicationFactory<Program>
{
    public const string Key = "DoctorRecordTests_OnlyForTests_2026_32characters!";
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:ClinicDb", "Server=localhost;Database=UnusedDoctorRecordTests;Integrated Security=true;TrustServerCertificate=true");
        builder.UseSetting("Jwt:Key", Key);
        builder.UseSetting("Jwt:Issuer", "DoctorRecordTests");
        builder.UseSetting("Jwt:Audience", "DoctorRecordTests");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAccountAccessValidationService>();
            services.AddSingleton<IAccountAccessValidationService, TestAccountAccessValidationService>();
            services.RemoveAll<IMedicalAuditWriter>();
            services.AddSingleton<IMedicalAuditWriter, Audit.NoopMedicalAuditWriter>();
            services.RemoveAll<IDoctorMedicalRecordService>();
            services.AddSingleton<IDoctorMedicalRecordService, StubRecords>();
        });
    }
    private sealed class StubRecords : IDoctorMedicalRecordService
    {
        public Task<HttpResponseData<PagedResult<DoctorMedicalRecordDTO>>> GetMyRecordsAsync(DoctorMedicalRecordQueryDTO query, int userId)
            => Task.FromResult(new HttpResponseData<PagedResult<DoctorMedicalRecordDTO>>
            {
                StatusCode = 200, Content = new()
                {
                    Page = query.Page, PageSize = query.PageSize, TotalItems = 1, TotalPages = 1,
                    Items = [new() { Id = userId, PatientName = query.Keyword ?? "Test", Status = query.Status ?? 0 }]
                }
            });
    }
}
