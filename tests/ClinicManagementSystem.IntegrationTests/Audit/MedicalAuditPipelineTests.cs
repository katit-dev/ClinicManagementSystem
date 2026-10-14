using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using ClinicManagementSystem.Api.Middleware;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.MedicalRecord;
using ClinicManagementSystem.Application.DTOs.Patient;
using ClinicManagementSystem.Application.DTOs.Scheduling;
using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;

namespace ClinicManagementSystem.IntegrationTests.Audit;

// Runs the real API routing, JWT, authorization, controllers, result filter and
// middleware. Only the clinical service and audit persistence are test doubles.
public class MedicalAuditPipelineTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("Patient")]
    [InlineData("Doctor")]
    public async Task DirectMedicalFileUrl_IsBlockedAndAudited_InsteadOfBypassingAttachmentApi(string? role)
    {
        using var factory = new MedicalAuditApiFactory(); using var client = factory.Client(role);
        using var response = await client.GetAsync("/uploads/medical-records/1204/private.txt");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("secret-medical-file", await response.Content.ReadAsStringAsync());
        var row = Assert.Single(factory.Capture.Entries);
        Assert.Equal("DIRECT_ATTACHMENT_ACCESS_DENIED", row.Action); Assert.False(row.Succeeded);
        Assert.Null(row.EntityId); Assert.Contains("\"medicalRecordId\":1204", row.Details!);
        Assert.DoesNotContain("private.txt", row.Details!);
    }

    [Fact]
    public async Task PublicStaticAssets_StillWorkWithoutPatientAudit()
    {
        using var factory = new MedicalAuditApiFactory(); using var client = factory.Client(null);
        using var response = await client.GetAsync("/audit-public.txt");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("public asset", await response.Content.ReadAsStringAsync());
        Assert.Empty(factory.Capture.Entries);
    }

    [Theory]
    [InlineData("/api/medical-records/1204", "Doctor", "medical_records", 1204)]
    [InlineData("/api/medical-records?appointmentId=77", "Patient", "medical_records", 1204)]
    [InlineData("/api/prescriptions/81/pdf", "Doctor", "prescriptions", 81)]
    [InlineData("/api/attachments/65/download", "Patient", "attachments", 65)]
    public async Task SuccessfulAccess_PersistsCorrectTargetBeforeReturningData(string path, string role, string entity, int id)
    {
        using var factory = new MedicalAuditApiFactory(); using var client = factory.Client(role);
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var row = Assert.Single(factory.Capture.Entries);
        Assert.Equal(entity, row.EntityName); Assert.Equal(id, row.EntityId); Assert.Equal(7, row.UserId);
        Assert.True(row.Succeeded); Assert.Equal(DateTimeKind.Utc, row.OccurredAt.Kind);
        Assert.InRange(row.OccurredAt, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddSeconds(1));
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
        Assert.NotEqual(factory.Capture.ClinicalScope, factory.Capture.WriterScope);
        Assert.NotNull(factory.Capture.ClinicalScope); Assert.NotNull(factory.Capture.WriterScope);
        if (path.EndsWith("pdf")) Assert.Equal("application/pdf", response.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task Details_ExcludeSensitiveBodyQueryHeadersAndSpoofedForwardedIp()
    {
        using var factory = new MedicalAuditApiFactory(); using var client = factory.Client("Doctor");
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "198.51.100.77");
        using var response = await client.PutAsJsonAsync("/api/medical-records/1204?phone=0909090909&keyword=secret-search",
            new MedicalRecordDraftRequestDTO { Diagnosis = "secret-diagnosis", Note = "secret-note", Symptoms = "secret-symptoms" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var row = Assert.Single(factory.Capture.Entries);
        Assert.Equal("UPDATE_RECORD", row.Action); Assert.Equal(1204, row.EntityId);
        Assert.Equal("203.0.113.9", row.IpAddress);
        using var details = JsonDocument.Parse(row.Details!);
        Assert.Equal("VC24_MEDICAL_ACCESS", details.RootElement.GetProperty("source").GetString());
        Assert.Equal("api/medical-records/{id:int}", details.RootElement.GetProperty("endpoint").GetString());
        Assert.Equal("PUT", details.RootElement.GetProperty("method").GetString());
        Assert.Equal(200, details.RootElement.GetProperty("statusCode").GetInt32());
        foreach (var secret in new[] { "0909090909", "secret-search", "secret-diagnosis", "secret-note", "secret-symptoms", "198.51.100.77", "Bearer" })
            Assert.DoesNotContain(secret, row.Details!);
    }

    [Theory]
    [InlineData(null, 401, null)]
    [InlineData("Patient", 403, 7)]
    [InlineData("Admin", 403, 7)]
    public async Task DeniedAccess_IsRecordedWithVerifiedActorOnly(string? role, int status, int? actor)
    {
        using var factory = new MedicalAuditApiFactory(); using var client = factory.Client(role);
        using var response = await client.GetAsync("/api/medical-records/1204");
        Assert.Equal(status, (int)response.StatusCode);
        var row = Assert.Single(factory.Capture.Entries);
        Assert.False(row.Succeeded); Assert.Equal(actor, row.UserId); Assert.Equal(1204, row.EntityId);
        Assert.Null(factory.Capture.ClinicalScope);
    }

    [Fact]
    public async Task InvalidJwt_DoesNotTrustItsActorClaim()
    {
        using var factory = new MedicalAuditApiFactory(); using var client = factory.Client(null);
        client.DefaultRequestHeaders.Authorization = new("Bearer", "invalid.jwt.token");
        using var response = await client.GetAsync("/api/medical-records/1204");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(Assert.Single(factory.Capture.Entries).UserId);
    }

    [Theory]
    [InlineData("/api/medical-records/999", 404)]
    [InlineData("/api/medical-records/500", 500)]
    [InlineData("/api/medical-records?appointmentId=oops", 400)]
    public async Task ServiceAndMvcFailures_AreRecorded(string path, int expected)
    {
        using var factory = new MedicalAuditApiFactory(); using var client = factory.Client(path.Contains('?') ? "Patient" : "Doctor");
        using var response = await client.GetAsync(path);
        Assert.Equal(expected, (int)response.StatusCode);
        var row = Assert.Single(factory.Capture.Entries); Assert.False(row.Succeeded);
        Assert.Contains($"\"statusCode\":{expected}", row.Details!);
    }

    [Fact]
    public async Task History_WritesOneEventPerReturnedMedicalRecord_NotPatientId()
    {
        using var factory = new MedicalAuditApiFactory(); using var client = factory.Client("Doctor");
        using var response = await client.GetAsync("/api/medical-records/patient/125");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new int?[] { 1204, 1205 }, factory.Capture.Entries.Select(x => x.EntityId));
        Assert.All(factory.Capture.Entries, x =>
        { Assert.Equal("medical_records", x.EntityName); Assert.Contains("\"patientId\":125", x.Details!); Assert.DoesNotContain("secret-diagnosis", x.Details!); });
    }

    [Fact]
    public async Task EmptyHistory_StillRecordsCollectionAccess()
    {
        using var factory = new MedicalAuditApiFactory(); using var client = factory.Client("Doctor");
        using var response = await client.GetAsync("/api/medical-records/patient/999");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var row = Assert.Single(factory.Capture.Entries); Assert.Null(row.EntityId); Assert.True(row.Succeeded);
    }

    [Fact]
    public async Task LabResult_UsesNestedResultId_NotServiceId()
    {
        using var factory = new MedicalAuditApiFactory(); using var client = factory.Client("Doctor");
        using var response = await client.PostAsJsonAsync("/api/medical-record-services/36/result", new LabResultRequestDTO { ResultValue = "secret-lab-value" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var row = Assert.Single(factory.Capture.Entries);
        Assert.Equal("lab_results", row.EntityName); Assert.Equal(65, row.EntityId);
        Assert.Contains("\"medicalRecordServiceId\":36", row.Details!); Assert.DoesNotContain("secret-lab-value", row.Details!);
    }

    [Fact]
    public async Task PublicPatientLookup_IsAuditedWithoutAddingAuthorization()
    {
        using var factory = new MedicalAuditApiFactory(); using var client = factory.Client(null);
        using var response = await client.GetAsync("/api/patients/lookup?phone=0909090909");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var row = Assert.Single(factory.Capture.Entries); Assert.Equal("VIEW_PATIENT", row.Action);
        Assert.Equal(125, row.EntityId); Assert.Null(row.UserId); Assert.DoesNotContain("0909090909", row.Details!);
    }

    [Theory]
    [InlineData("GET", "/api/medical-records/1204")]
    [InlineData("GET", "/api/prescriptions/81/pdf")]
    [InlineData("GET", "/api/attachments/65/download")]
    [InlineData("PUT", "/api/medical-records/1204")]
    public async Task AuditPersistenceFailure_Returns503WithoutReleasingPatientData(string method, string path)
    {
        using var factory = new MedicalAuditApiFactory(); factory.Capture.ThrowOnWrite = true;
        using var client = factory.Client(path.Contains("attachments") ? "Patient" : "Doctor"); using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method == "PUT") request.Content = JsonContent.Create(new MedicalRecordDraftRequestDTO { Note = "secret-note" });
        using var response = await client.SendAsync(request); var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
        Assert.DoesNotContain("secret-note", body); Assert.DoesNotContain("secret-diagnosis", body);
        Assert.DoesNotContain("secret-db-error", body); Assert.DoesNotContain("%PDF", body);
        Assert.DoesNotContain("secret-medical-file", body);
        Assert.NotNull(factory.Capture.ClinicalScope); Assert.Empty(factory.Capture.Entries);
        // A mutation may already have committed. The API does not claim rollback.
        var error = JsonSerializer.Deserialize<HttpResponseData<object>>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Contains("kiểm tra trạng thái", error!.Message);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
    }

    [Fact]
    public void SchedulingConflictPreview_UsesAffectedAppointmentIds()
    {
        var endpoint = MedicalAuditEndpoint.Resolve(new() { ControllerName = "AdminScheduling", ActionName = "CreateTimeOff" })!;
        var envelope = new HttpResponseData<TimeOffChangeResultDTO> { StatusCode = 409, Content = new()
        { AffectedAppointments = [new() { Id = 77, PatientName = "secret-patient" }] } };
        Assert.Equal(new[] { 77 }, endpoint.GetReturnedIds(envelope));
        Assert.Equal("appointments", endpoint.Entity);
    }

    [Fact]
    public void AllExistingPatientDataActions_HaveAuditMapping_AndCatalogsAreExcluded()
    {
        using var factory = new MedicalAuditApiFactory();
        var descriptors = factory.Services.GetRequiredService<IActionDescriptorCollectionProvider>().ActionDescriptors.Items.OfType<ControllerActionDescriptor>().ToList();
        var controllers = new[] { "MedicalRecord", "Patient", "PatientRecord", "Attachment", "Appointment", "ReceptionAppointment", "Queue", "PatientInvoice", "Invoice", "Notification" };
        var actions = descriptors.Where(x => controllers.Contains(x.ControllerName)).ToList();
        Assert.True(actions.Count >= 35);
        Assert.All(actions, action => Assert.NotNull(MedicalAuditEndpoint.Resolve(action)));
        foreach (var action in descriptors.Where(x => x.ControllerName is "AdminAuditLog" or "Service" or "Specialty" or "Medicines"))
            Assert.Null(MedicalAuditEndpoint.Resolve(action));
    }
}

public sealed class MedicalAuditApiFactory : WebApplicationFactory<Program>
{
    private const string Key = "VC24Pipeline_TestKey_WithAtLeast32Characters!";
    public MedicalAuditCapture Capture { get; } = new();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:ClinicDb", "Server=localhost;Database=UnusedMedicalAudit;Integrated Security=true;TrustServerCertificate=true");
        builder.UseSetting("Jwt:Key", Key); builder.UseSetting("Jwt:Issuer", "VC24"); builder.UseSetting("Jwt:Audience", "VC24");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAccountAccessValidationService>(); services.AddSingleton<IAccountAccessValidationService, TestAccountAccessValidationService>();
            services.AddSingleton(Capture); services.AddScoped<AuditScopeMarker>();
            services.RemoveAll<IMedicalAuditWriter>(); services.AddScoped<IMedicalAuditWriter, CaptureMedicalAuditWriter>();
            services.RemoveAll<IMedicalRecordService>();
            services.AddScoped<IMedicalRecordService>(sp =>
            {
                var proxy = DispatchProxy.Create<IMedicalRecordService, MedicalFixtureProxy>();
                ((MedicalFixtureProxy)(object)proxy).Capture = Capture;
                ((MedicalFixtureProxy)(object)proxy).Scope = sp.GetRequiredService<AuditScopeMarker>().Id;
                return proxy;
            });
            services.RemoveAll<IPatientService>();
            services.AddScoped<IPatientService>(_ => DispatchProxy.Create<IPatientService, MedicalFixtureProxy>());
            services.RemoveAll<IAttachmentService>();
            services.AddScoped<IAttachmentService>(sp =>
            {
                var proxy = DispatchProxy.Create<IAttachmentService, MedicalFixtureProxy>();
                ((MedicalFixtureProxy)(object)proxy).Capture = Capture;
                ((MedicalFixtureProxy)(object)proxy).Scope = sp.GetRequiredService<AuditScopeMarker>().Id;
                return proxy;
            });
            services.AddSingleton<IStartupFilter, AuditIpStartupFilter>();
            services.Configure<StaticFileOptions>(options => options.FileProvider = new AuditStaticFiles());
        });
    }
    public HttpClient Client(string? role)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new("https://localhost") });
        if (role == null) return client;
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "7"), new Claim(ClaimTypes.Role, role) };
        var jwt = new JwtSecurityToken("VC24", "VC24", claims, expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)), SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new("Bearer", new JwtSecurityTokenHandler().WriteToken(jwt));
        return client;
    }
    private sealed class AuditIpStartupFilter : IStartupFilter
    {
        public Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> Configure(Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> next)
            => app => { app.Use((context, continuation) => { context.Connection.RemoteIpAddress = IPAddress.Parse("::ffff:203.0.113.9"); return continuation(); }); next(app); };
    }
    private sealed class AuditStaticFiles : IFileProvider
    {
        public IFileInfo GetFileInfo(string subpath) => subpath switch
        {
            "/uploads/medical-records/1204/private.txt" => new AuditFile("private.txt", "secret-medical-file"),
            "/audit-public.txt" => new AuditFile("audit-public.txt", "public asset"),
            _ => new NotFoundFileInfo(subpath)
        };
        public IDirectoryContents GetDirectoryContents(string subpath) => NotFoundDirectoryContents.Singleton;
        public IChangeToken Watch(string filter) => NullChangeToken.Singleton;
    }
    private sealed class AuditFile(string name, string text) : IFileInfo
    {
        public bool Exists => true;
        public long Length => Encoding.UTF8.GetByteCount(text);
        public string? PhysicalPath => null;
        public string Name => name;
        public DateTimeOffset LastModified => DateTimeOffset.UtcNow;
        public bool IsDirectory => false;
        public Stream CreateReadStream() => new MemoryStream(Encoding.UTF8.GetBytes(text));
    }
}

public class MedicalFixtureProxy : DispatchProxy
{
    public MedicalAuditCapture? Capture { get; set; }
    public Guid Scope { get; set; }
    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        if (Capture != null) Capture.ClinicalScope = Scope;
        return method!.Name switch
        {
            "GetMedicalRecordByIdForDoctorAsync" => Result(new MedicalRecordExamDTO { Id = (int)args![0]!, Diagnosis = "secret-diagnosis", Note = "secret-note" },
                (int)args![0]! == 999 ? 404 : (int)args[0]! == 500 ? 500 : 200),
            "GetMedicalRecordByAppointmentAsync" => Result(new MedicalRecordDTO { Id = 1204, AppointmentId = 77, Diagnosis = "secret-diagnosis" }),
            "GetPatientMedicalRecordsAsync" => Result((int)args![0]! == 999 ? new List<MedicalRecordDTO>()
                : [new MedicalRecordDTO { Id = 1204, Diagnosis = "secret-diagnosis" }, new() { Id = 1205 }, new() { Id = 1204 }]),
            "UpdateMedicalRecordDraftAsync" => Result(new MedicalRecordDraftResponseDTO { Id = 1204, Note = "secret-note" }),
            "AddMedicalRecordServiceResultAsync" => Result(new MedicalRecordServiceDTO { Id = 36, Result = new() { Id = 65, ResultValue = "secret-lab-value" } }),
            "GetPrescriptionPdfAsync" => Task.FromResult<byte[]?>(Encoding.ASCII.GetBytes("%PDF-1.4 secret-pdf")),
            "DownloadAttachmentAsync" => Task.FromResult<(byte[] FileBytes, string ContentType, string FileName)?>((Encoding.UTF8.GetBytes("secret-medical-file"), "text/plain", "private.txt")),
            "LookupByPhoneAsync" => Result<PatientLookupDTO?>(new() { Id = 125 }),
            _ => throw new NotSupportedException("Unexpected clinical fixture call: " + method.Name)
        };
    }
    private static Task<HttpResponseData<T>> Result<T>(T content, int status = 200)
        => Task.FromResult(new HttpResponseData<T> { StatusCode = status, Content = status == 200 ? content : default });
}

public sealed class MedicalAuditCapture
{
    public List<AuditLog> Entries { get; } = [];
    public bool ThrowOnWrite { get; set; }
    public Guid? ClinicalScope { get; set; }
    public Guid? WriterScope { get; set; }
}
public sealed class AuditScopeMarker { public Guid Id { get; } = Guid.NewGuid(); }
public sealed class CaptureMedicalAuditWriter(MedicalAuditCapture capture, AuditScopeMarker marker) : IMedicalAuditWriter
{
    public Task WriteAsync(IReadOnlyCollection<AuditLog> entries)
    {
        capture.WriterScope = marker.Id;
        if (capture.ThrowOnWrite) throw new InvalidOperationException("secret-db-error");
        capture.Entries.AddRange(entries);
        return Task.CompletedTask;
    }
}
