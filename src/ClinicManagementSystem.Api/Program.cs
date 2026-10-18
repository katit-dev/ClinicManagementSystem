using ClinicManagementSystem.Api.Configuration;
using Microsoft.OpenApi;
using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Repositories;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using ClinicManagementSystem.Infrastructure.Services;
using ClinicManagementSystem.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// QUESTPDF
// =====================================================

QuestPDF.Settings.License =
    QuestPDF.Infrastructure.LicenseType.Evaluation;

// ============================================================
// CONTROLLERS
// ============================================================

builder.Services.AddControllers(options => options.Filters.Add<MedicalAuditResultFilter>());

// ============================================================
// DI DbContext
// ============================================================

string connectionString =
    builder.Configuration.GetConnectionString("ClinicDb")
    ?? throw new InvalidOperationException(
        "Connection string 'ClinicDb' not found.");

builder.Services.AddDbContext<ClinicManagementDbContext>(options =>
    options.UseSqlServer(connectionString));

// ============================================================
// DI Repository
// ============================================================

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<IAppointmentStatusHistoryRepository, AppointmentStatusHistoryRepository>();
builder.Services.AddScoped<IAttachmentRepository, AttachmentRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
builder.Services.AddScoped<IDoctorScheduleRepository, DoctorScheduleRepository>();
builder.Services.AddScoped<IDoctorTimeOffRepository, DoctorTimeOffRepository>();
builder.Services.AddScoped<IInsurancePolicyRepository, InsurancePolicyRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IInvoiceItemRepository, InvoiceItemRepository>();
builder.Services.AddScoped<IClinicReportRepository, ClinicReportRepository>();
builder.Services.AddScoped<ILabResultRepository, LabResultRepository>();
builder.Services.AddScoped<IMedicalRecordRepository, MedicalRecordRepository>();
builder.Services.AddScoped<IMedicalRecordServiceRepository, MedicalRecordServiceRepository>();
builder.Services.AddScoped<IMedicineRepository, MedicineRepository>();
builder.Services.AddScoped<IMedicineBatchRepository, MedicineBatchRepository>();
builder.Services.AddScoped<IMedicineStockTransactionRepository, MedicineStockTransactionRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IPatientAllergyRepository, PatientAllergyRepository>();
builder.Services.AddScoped<IPatientVitalRepository, PatientVitalRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
builder.Services.AddScoped<IPrescriptionItemRepository, PrescriptionItemRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IServiceRepository, ServiceRepository>();
builder.Services.AddScoped<ISpecialtyRepository, SpecialtyRepository>();
builder.Services.AddScoped<IUserRoleRepository, UserRoleRepository>();
builder.Services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();


// ============================================================
// DI UnitOfWork
// ============================================================

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ============================================================
// DI Service
// ============================================================

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IJwtAuthService, JwtAuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAdminUserManagementService, AdminUserManagementService>();
builder.Services.AddScoped<IAdminAuditLogService, AdminAuditLogService>();
builder.Services.AddScoped<IMedicalAuditWriter, MedicalAuditWriter>();
builder.Services.AddScoped<IAccountAccessValidationService, AccountAccessValidationService>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<ISpecialtyService, SpecialtyService>();
builder.Services.AddScoped<IAdminSpecialtyService, AdminSpecialtyService>();
builder.Services.AddScoped<IAdminServiceCatalogService, AdminServiceCatalogService>();
builder.Services.AddScoped<IAdminReportService, AdminReportService>();
builder.Services.AddScoped<IAdminSchedulingService, AdminSchedulingService>();
builder.Services.AddScoped<IReceptionAffectedAppointmentService, ReceptionAffectedAppointmentService>();
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IMedicalRecordService, MedicalRecordService>();
builder.Services.AddScoped<IPatientRecordService, PatientRecordService>();
builder.Services.AddScoped<IAttachmentService, AttachmentService>();
builder.Services.AddScoped<IPatientInvoiceService, PatientInvoiceService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IMedicalRecordService, MedicalRecordService>();
builder.Services.AddScoped< IMedicineService, MedicineService>();
builder.Services.AddScoped<IServiceCatalogService, ServiceCatalogService>();
builder.Services.AddScoped<IPharmacyService, PharmacyService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IMedicineInventoryService, MedicineInventoryService>();


// file storage
builder.Services.AddAttachmentFileStorage(builder.Configuration, builder.Environment);


// ============================================================
// DI Serilog
// ============================================================

builder.Services.AddSerilog((services, loggerConfiguration) =>
    loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services));

// ============================================================
// Authorization
// ============================================================

builder.Services.AddAuthorization();

// ============================================================
// Swagger
// ============================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Clinic Management System API",
        Version = "v1",
        Description = "API documentation for Clinic Management System"
    });
    options.AddSecurityDefinition(
    "bearer",
    new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập JWT Access Token"
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [
                new OpenApiSecuritySchemeReference(
                    "bearer",
                    document)
            ] = new List<string>()
        });
});

// ============================================================
// JWT AUTHENTICATION
// ============================================================

var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "JWT Key is not configured.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,

                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey)
                ),

                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateLifetime = true,

                ClockSkew = TimeSpan.Zero,

                RoleClaimType = ClaimTypes.Role,

                NameClaimType = "UserName"
            };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var validator = context.HttpContext.RequestServices.GetRequiredService<IAccountAccessValidationService>();
                if (context.Principal == null || !await validator.IsValidAsync(context.Principal))
                    context.Fail("Account session is no longer valid.");
            }
        };
    });


// =====================================================
// CORS
// Cho phép Frontend gọi Backend API
// =====================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5050")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


var app = builder.Build();

// ============================================================
// Swagger Middleware
// ============================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "Clinic Management System API V1"
        );

        options.RoutePrefix = string.Empty;
    });
}

// ============================================================
// Middleware
// ============================================================

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("AllowFrontend");

app.UseAuthentication();

app.UseMiddleware<MedicalAuditMiddleware>();

app.UseAuthorization();

// MedicalAuditMiddleware blocks private uploads before static-file serving.
app.UseStaticFiles();

app.MapControllers();
// Log khởi động
app.Logger.LogInformation("Clinic API started.");

app.Run();

public partial class Program
{
}
