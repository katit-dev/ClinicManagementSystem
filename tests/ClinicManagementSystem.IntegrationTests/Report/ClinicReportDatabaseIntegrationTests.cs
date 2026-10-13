using ClinicManagementSystem.Application.DTOs.Report;
using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.Repositories;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MedicalRecordEntity = ClinicManagementSystem.Infrastructure.Models.MedicalRecord;
using InvoiceEntity = ClinicManagementSystem.Infrastructure.Models.Invoice;
using MedicalRecordServiceEntity = ClinicManagementSystem.Infrastructure.Models.MedicalRecordService;

namespace ClinicManagementSystem.IntegrationTests.Reports;

// Requires Docker. Each test owns a fresh database inside the test container only.
public class ClinicReportDatabaseIntegrationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly DateTime From = new(2026, 10, 1);
    private static readonly DateTime End = new(2026, 11, 1);

    [Fact]
    public async Task SqlQueries_CountVisitsOnce_AndApplyCashDateAndDoctorFilters()
    {
        await using var db = await Database();
        var (doctor, appointment, patient) = await SeedDoctor(db);
        var service = new Service { Code = "VC23-S", Name = "Tên mới", Price = 999, IsActive = true };
        var medicine = new Medicine { Code = "VC23-M", Name = "Tên thuốc mới", Price = 999, IsActive = true };
        var record = new MedicalRecordEntity { Appointment = appointment, Patient = patient, Doctor = doctor, CreatedAt = From };
        var ordered = new MedicalRecordServiceEntity { MedicalRecord = record, Service = service, Quantity = 1, UnitPriceSnapshot = 300, OrderedAt = From };
        var invoice = new InvoiceEntity { InvoiceNo = "VC23-I", Patient = patient, Appointment = appointment, MedicalRecord = record, CreatedAt = From,
            TotalAmount = 1000, DiscountAmount = 100, TaxAmount = 100, InsuranceAmount = 200, PaidAmount = 800, Status = 2,
            InvoiceItems = [new() { MedicalRecordService = ordered, Description = "Dịch vụ giá cũ", Amount = 300, Quantity = 1, UnitPrice = 300 },
                new() { Medicine = medicine, Description = "Thuốc giá cũ", Amount = 200, Quantity = 1, UnitPrice = 200 }],
            Payments = [new() { PaidAt = From.AddDays(-1), Amount = 100 }, new() { PaidAt = From, Amount = 300 },
                new() { PaidAt = End.AddSeconds(-1), Amount = 100 }, new() { PaidAt = End.AddDays(-2), Amount = -50, IsRefund = true },
                new() { PaidAt = End, Amount = 350 }] };
        var old = new InvoiceEntity { InvoiceNo = "VC23-OLD", Patient = patient, Appointment = appointment, CreatedAt = From.AddDays(-1), TotalAmount = 100,
            Payments = [new() { PaidAt = From.AddDays(1), Amount = 100 }] };
        db.Invoices.AddRange(invoice, old);
        db.Appointments.AddRange(
            Appointment(doctor, patient, "VC23-END", End.AddMinutes(-30), 3),
            Appointment(doctor, patient, "VC23-NOSHOW", From.AddDays(1), 5),
            Appointment(doctor, patient, "VC23-PENDING", From.AddDays(2), 0),
            Appointment(doctor, patient, "VC23-CANCEL", From.AddDays(3), 4),
            Appointment(doctor, patient, "VC23-NEXT", End, 3));
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();

        var repository = new ClinicReportRepository(db);
        var data = await repository.GetDataAsync(From, End, doctor.Id);
        var visit = Assert.Single(data.Visits);
        Assert.Equal(2, visit.CompletedCount); Assert.Equal(1, visit.NoShowCount);
        Assert.Equal(2, data.Invoices.Count);
        var row = data.Invoices.Single(i => i.Id == invoice.Id);
        Assert.Equal(400m, row.CollectedInPeriod); Assert.Equal(50m, row.RefundedInPeriod); Assert.Equal(450m, row.PaidThroughEnd);
        Assert.Equal(2, row.Items.Count); Assert.Equal(doctor.Id, row.DoctorId);
        Assert.Contains(row.Items, i => i.ServiceId == service.Id && i.Description == "Dịch vụ giá cũ" && i.Amount == 300);
        Assert.Contains(row.Items, i => i.MedicineId == medicine.Id && i.Amount == 200);
        Assert.Empty(db.ChangeTracker.Entries());

        await using var uow = new UnitOfWork(db);
        var report = (await new AdminReportService(uow, NullLogger<AdminReportService>.Instance).GetReportAsync(Query(doctor.Id))).Content!;
        Assert.Equal(2, report.TotalVisits); Assert.Equal(33.33m, report.NoShowRate);
        Assert.Equal(450m, report.TotalRevenue); Assert.Equal(350m, report.OutstandingDebt);
        Assert.Equal(report.TotalRevenue, report.Doctors.Sum(d => d.TotalRevenue));
        Assert.Equal(report.TotalRevenue, report.RevenueItems.Sum(i => i.Revenue));
    }

    [Fact]
    public async Task SqlQueries_UnlinkedAndCancelledInvoices_RemainVisibleInCashTotals()
    {
        await using var db = await Database();
        var (doctor, appointment, patient) = await SeedDoctor(db);
        db.Invoices.AddRange(
            new InvoiceEntity { InvoiceNo = "VC23-NODOC", Patient = patient, CreatedAt = From, TotalAmount = 100, Payments = [new() { Amount = 30, PaidAt = From }] },
            new InvoiceEntity { InvoiceNo = "VC23-CANCEL", Patient = patient, Appointment = appointment, CreatedAt = From, TotalAmount = 100,
                Status = 3, CancelledAt = End.AddDays(-1), Payments = [new() { Amount = 100, PaidAt = From }, new() { Amount = -100, IsRefund = true, PaidAt = From.AddDays(1) }] });
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        await using var uow = new UnitOfWork(db);
        var service = new AdminReportService(uow, NullLogger<AdminReportService>.Instance);
        var all = (await service.GetReportAsync(Query())).Content!;
        Assert.Equal(130m, all.CollectedAmount); Assert.Equal(100m, all.RefundedAmount);
        Assert.Equal(30m, all.TotalRevenue); Assert.Equal(70m, all.OutstandingDebt);
        Assert.Contains(all.Doctors, d => d.DoctorId == null && d.OtherRevenue == 30);
        var filtered = (await service.GetReportAsync(Query(doctor.Id))).Content!;
        Assert.Equal(0, filtered.TotalRevenue); Assert.Equal(0, filtered.OutstandingDebt);
        Assert.DoesNotContain(filtered.Doctors, d => d.DoctorId == null);
    }

    private async Task<ClinicManagementDbContext> Database()
    {
        var connection = new SqlConnectionStringBuilder(factory.ConnectionString) { InitialCatalog = "VC23_" + Guid.NewGuid().ToString("N") };
        var db = new ClinicManagementDbContext(new DbContextOptionsBuilder<ClinicManagementDbContext>().UseSqlServer(connection.ConnectionString).Options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }
    private static ClinicReportQueryDTO Query(int? id = null) => new() { From = new(2026, 10, 1), To = new(2026, 10, 31), DoctorId = id };
    private static Appointment Appointment(Doctor d, Patient p, string code, DateTime time, byte status)
        => new() { Doctor = d, Patient = p, AppointmentCode = code, StartTime = time, EndTime = time.AddMinutes(30), Status = status, CreatedAt = From };
    private static async Task<(Doctor, Appointment, Patient)> SeedDoctor(ClinicManagementDbContext db)
    {
        var doctor = new Doctor { FullName = "Lê Thu Hà", IsActive = true, User = new() { Username = "vc23-doctor", PasswordHash = "test-only", IsActive = true },
            Specialty = new() { Name = "Test VC23", Code = "VC23", IsActive = true } };
        var patient = new Patient { FullName = "Bệnh nhân test VC23", PatientCode = "VC23-P", IsActive = true };
        var appointment = Appointment(doctor, patient, "VC23-A", From, 3);
        db.Appointments.Add(appointment); await db.SaveChangesAsync();
        return (doctor, appointment, patient);
    }
}
