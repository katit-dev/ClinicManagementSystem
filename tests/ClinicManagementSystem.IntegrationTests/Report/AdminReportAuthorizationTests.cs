using System.IdentityModel.Tokens.Jwt;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Report;
using ClinicManagementSystem.Application.Helpers;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace ClinicManagementSystem.IntegrationTests.Reports;

// Real JWT middleware, MVC binding/validation and binary response; no SQL connection.
public class AdminReportAuthorizationTests(AdminReportApiFactory factory) : IClassFixture<AdminReportApiFactory>
{
    private const string Dates = "?from=2026-10-01&to=2026-10-31";
    private static readonly string[] Routes = ["/api/admin/reports", "/api/admin/reports/export"];
    public static IEnumerable<object[]> AccessCases()
    {
        foreach (var path in Routes)
        foreach (var role in new string?[] { null, "Patient", "Doctor", "Receptionist", "Pharmacist", "Admin" })
            yield return [path, role!, role == null ? 401 : role == "Admin" ? 200 : 403];
    }
    [Theory]
    [MemberData(nameof(AccessCases))]
    public async Task BothRoutes_RequireAuthenticatedAdmin(string path, string? role, int status)
    {
        using var client = Client(role); using var response = await client.GetAsync(path + Dates);
        Assert.Equal(status, (int)response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("invalid")]
    public async Task InvalidActor_IsUnauthorized(string? actor)
    {
        using var client = Client("Admin", actor);
        foreach (var path in Routes) Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(path + Dates)).StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("?from=2026-10-01")]
    [InlineData("?to=2026-10-31")]
    [InlineData("?from=abc&to=2026-10-31")]
    [InlineData("?from=2026-10-31&to=2026-10-01")]
    [InlineData("?from=2026-01-01&to=2027-01-02")]
    [InlineData("?from=9999-12-31&to=9999-12-31")]
    [InlineData("?from=2026-10-01&to=2026-10-31&doctorId=0")]
    [InlineData("?from=2026-10-01&to=2026-10-31&doctorId=-1")]
    [InlineData("?from=2026-10-01&to=2026-10-31&doctorId=abc")]
    public async Task InvalidQuery_Is400ForReportAndExport(string query)
    {
        using var client = Client("Admin");
        foreach (var path in Routes)
        {
            using var response = await client.GetAsync(path + query);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("json", response.Content.Headers.ContentType!.MediaType!);
        }
    }

    [Fact]
    public async Task Report_PreservesDoctorAndDates_AndDisablesCaching()
    {
        using var client = Client("Admin"); using var response = await client.GetAsync(Routes[0] + Dates + "&doctorId=7");
        var result = await response.Content.ReadFromJsonAsync<HttpResponseData<ClinicReportDTO>>();
        Assert.Equal(200, result!.StatusCode); Assert.Equal(new DateOnly(2026, 10, 1), result.Content!.From);
        Assert.Equal(new DateOnly(2026, 10, 31), result.Content.To); Assert.Equal(7, result.Content.DoctorId);
        Assert.True(response.Headers.CacheControl!.NoStore);
    }

    [Fact]
    public async Task Export_IsRealWorkbook_WithSameDoctorFilterAndDownloadHeaders()
    {
        using var client = Client("Admin"); using var response = await client.GetAsync(Routes[1] + Dates + "&doctorId=7");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ClinicReportExportDTO.ContentType, response.Content.Headers.ContentType!.MediaType);
        Assert.Contains("doctor-7.xlsx", response.Content.Headers.ContentDisposition!.FileNameStar!);
        Assert.True(response.Headers.CacheControl!.NoStore);
        using var zip = new ZipArchive(new MemoryStream(await response.Content.ReadAsByteArrayAsync()));
        Assert.NotNull(zip.GetEntry("xl/workbook.xml"));
        using var reader = new StreamReader(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        Assert.Contains("#7", await reader.ReadToEndAsync());
    }

    [Theory]
    [InlineData(404)]
    [InlineData(500)]
    public async Task ServiceErrors_RemainJson_ForBothRoutes(int code)
    {
        using var client = Client("Admin");
        foreach (var path in Routes)
        {
            using var response = await client.GetAsync(path + Dates + $"&doctorId={code}");
            Assert.Equal(code, (int)response.StatusCode); Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
            Assert.Null(response.Content.Headers.ContentDisposition);
        }
    }

    private HttpClient Client(string? role, string? actor = "7")
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new("https://localhost") });
        if (role != null)
        {
            var claims = new List<Claim> { new(ClaimTypes.Role, role) };
            if (actor != null) claims.Add(new(ClaimTypes.NameIdentifier, actor));
            var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AdminReportApiFactory.Key)), SecurityAlgorithms.HmacSha256);
            var jwt = new JwtSecurityToken("AdminReportTests", "AdminReportTests", claims, expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: credentials);
            client.DefaultRequestHeaders.Authorization = new("Bearer", new JwtSecurityTokenHandler().WriteToken(jwt));
        }
        return client;
    }
}

public class AdminReportApiFactory : WebApplicationFactory<Program>
{
    public const string Key = "AdminReportTests_Key_AtLeast32Characters_2026!";
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:ClinicDb", "Server=localhost;Database=UnusedReportHttpTests;Integrated Security=true;TrustServerCertificate=true");
        builder.UseSetting("Jwt:Key", Key); builder.UseSetting("Jwt:Issuer", "AdminReportTests"); builder.UseSetting("Jwt:Audience", "AdminReportTests");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAccountAccessValidationService>();
            services.AddSingleton<IAccountAccessValidationService, TestAccountAccessValidationService>();
            services.RemoveAll<IAdminReportService>(); services.AddSingleton<IAdminReportService, StubReportService>();
        });
    }
    private sealed class StubReportService : IAdminReportService
    {
        private static ClinicReportDTO Report(ClinicReportQueryDTO q) => new() { From = q.From!.Value, To = q.To!.Value, DoctorId = q.DoctorId };
        private static int Status(ClinicReportQueryDTO q) => q.DoctorId is 404 or 500 ? q.DoctorId.Value : 200;
        public Task<HttpResponseData<ClinicReportDTO>> GetReportAsync(ClinicReportQueryDTO q)
            => Task.FromResult(new HttpResponseData<ClinicReportDTO> { StatusCode = Status(q), Content = Status(q) == 200 ? Report(q) : null });
        public Task<HttpResponseData<ClinicReportExportDTO>> ExportAsync(ClinicReportQueryDTO q)
            => Task.FromResult(new HttpResponseData<ClinicReportExportDTO> { StatusCode = Status(q), Content = Status(q) == 200
                ? new() { Bytes = ClinicReportExcelWriter.Create(Report(q)), FileName = $"report_doctor-{q.DoctorId}.xlsx" } : null });
    }
}
