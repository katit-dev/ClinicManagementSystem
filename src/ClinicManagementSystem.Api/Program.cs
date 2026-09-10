

using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Repositories;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// DI DbContext
builder.Services.AddDbContext<ClinicManagementDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("ClinicDb")));

// DI Repository
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

// DI UnitOfWork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// DI Service
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPatientService, PatientService>();

// DI Serilog
builder.Services.AddSerilog((services, loggerConfiguration) =>
    loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services));

var app = builder.Build();

// Log khoi dong
app.Logger.LogInformation("Clinic API started.");

app.Run();