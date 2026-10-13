using System.Reflection;
using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.Models.Reports;
using ClinicManagementSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using MedicalRecordEntity = ClinicManagementSystem.Infrastructure.Models.MedicalRecord;

namespace ClinicManagementSystem.UnitTests.Infrastructure.Reports;

public class ClinicReportRepositoryTests
{
    private static readonly DateTime From = new(2026, 10, 1);
    private static readonly DateTime End = new(2026, 11, 1);
    private static ClinicManagementDbContext Context() => new(new DbContextOptionsBuilder<ClinicManagementDbContext>()
        .UseSqlServer("Server=localhost;Database=UnusedReportTranslation;Integrated Security=true;TrustServerCertificate=true").Options);

    [Theory]
    [InlineData(null)]
    [InlineData(7)]
    public void BothQueries_TranslateToSqlServer_WithoutConnectingToDatabase(int? doctorId)
    {
        using var db = Context(); var repository = new ClinicReportRepository(db);
        // Compile the actual private production queries; no alternative LINQ is used here.
        var visitQuery = (IQueryable<ClinicReportVisitData>)typeof(ClinicReportRepository).GetMethod("VisitQuery", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(repository, [From, End, doctorId])!;
        var invoiceQuery = (IQueryable<ClinicReportInvoiceData>)typeof(ClinicReportRepository).GetMethod("InvoiceQuery", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(repository, [From, End, doctorId])!;
        var visits = visitQuery.ToQueryString(); var invoices = invoiceQuery.ToQueryString();
        Assert.Contains("GROUP BY", visits); Assert.Contains("[scheduling].[appointments]", visits);
        Assert.Contains("[billing].[payments]", invoices); Assert.Contains("[billing].[invoice_items]", invoices);
        Assert.Contains("SUM(", invoices); Assert.Contains("ABS(", invoices);
    }

    [Fact]
    public async Task Visits_IncludeEndDay_ExcludeNextDayAndUnconcludedStatuses()
    {
        var doctor = new Doctor { Id = 7, FullName = "Hà" };
        var other = new Doctor { Id = 8, FullName = "An" };
        var appointments = new List<Appointment>();
        void Add(DateTime time, byte status, Doctor d) => appointments.Add(new() { Doctor = d, DoctorId = d.Id, StartTime = time, Status = status });
        Add(From, 3, doctor); Add(End.AddSeconds(-1), 3, doctor); Add(From.AddDays(1), 5, doctor);
        Add(From.AddTicks(-1), 3, doctor); Add(End, 3, doctor);
        foreach (byte status in new byte[] { 0, 1, 2, 4 }) Add(From.AddDays(2), status, doctor);
        Add(From, 3, other);
        using var db = MockContext(appointments, []);
        var data = await new ClinicReportRepository(db).GetDataAsync(From, End, 7);
        var visit = Assert.Single(data.Visits);
        Assert.Equal(2, visit.CompletedCount); Assert.Equal(1, visit.NoShowCount);
    }

    [Fact]
    public async Task Payments_AreSummedOnce_RefundSignNormalized_AndHistoricalCutoffRespected()
    {
        var invoice = new Invoice { Id = 1, CreatedAt = From, TotalAmount = 1000, PaidAmount = 999 };
        invoice.Payments = [new() { Amount = 100, PaidAt = From.AddDays(-1) }, new() { Amount = 200, PaidAt = From },
            new() { Amount = -50, IsRefund = true, PaidAt = End.AddSeconds(-1) },
            new() { Amount = 20, IsRefund = true, PaidAt = From.AddDays(2) },
            new() { Amount = 10, IsRefund = false, PaidAt = End }, new() { Amount = -5, PaidAt = From.AddDays(3) }];
        invoice.InvoiceItems = [new() { Id = 1, MedicineId = 9, Description = "Giá snapshot", Amount = 100 },
            new() { Id = 2, MedicineId = 9, Description = "Giá snapshot", Amount = 200 }];
        using var db = MockContext([], [invoice]);
        var row = Assert.Single((await new ClinicReportRepository(db).GetDataAsync(From, End, null)).Invoices);
        Assert.Equal(200m, row.CollectedInPeriod); Assert.Equal(75m, row.RefundedInPeriod);
        Assert.Equal(225m, row.PaidThroughEnd); Assert.Equal(2, row.Items.Count);
        Assert.Equal("Giá snapshot", row.Items[0].Description); Assert.Null(row.DoctorId);
    }

    [Fact]
    public async Task OldInvoice_WithPaymentInPeriod_IsIncluded_ButOtherOldInvoicesAreNot()
    {
        var paidNow = new Invoice { Id = 1, CreatedAt = From.AddYears(-1), Payments = [new() { PaidAt = From, Amount = 10 }] };
        var paidAfter = new Invoice { Id = 2, CreatedAt = From.AddDays(-1), Payments = [new() { PaidAt = End, Amount = 20 }] };
        var issuedNow = new Invoice { Id = 3, CreatedAt = End.AddTicks(-1) };
        var issuedAfter = new Invoice { Id = 4, CreatedAt = End };
        using var db = MockContext([], [paidNow, paidAfter, issuedNow, issuedAfter]);
        var rows = (await new ClinicReportRepository(db).GetDataAsync(From, End, null)).Invoices;
        Assert.Equal(new[] { 1, 3 }, rows.Select(r => r.Id));
    }

    [Fact]
    public async Task DoctorFilter_PrefersMedicalRecord_ThenAppointment_AndKeepsInactiveDoctors()
    {
        var doc = new Doctor { Id = 7, FullName = "Hà", IsActive = false };
        var other = new Doctor { Id = 8, FullName = "An" };
        var appointment = new Appointment { DoctorId = other.Id, Doctor = other };
        var invoice = new Invoice { Id = 1, CreatedAt = From, AppointmentId = 1, Appointment = appointment,
            MedicalRecordId = 1, MedicalRecord = new MedicalRecordEntity { DoctorId = doc.Id, Doctor = doc } };
        var fallback = new Invoice { Id = 2, CreatedAt = From, AppointmentId = 2, Appointment = new() { DoctorId = 7, Doctor = doc } };
        var unlinked = new Invoice { Id = 3, CreatedAt = From };
        using var db = MockContext([], [invoice, fallback, unlinked]);
        db.Doctors = new[] { doc, other }.BuildMockDbSet().Object;
        var repository = new ClinicReportRepository(db);
        Assert.True(await repository.DoctorExistsAsync(7)); Assert.False(await repository.DoctorExistsAsync(99));
        var rows = (await repository.GetDataAsync(From, End, 7)).Invoices;
        Assert.Equal(new[] { 1, 2 }, rows.Select(r => r.Id));
        Assert.All(rows, r => { Assert.Equal(7, r.DoctorId); Assert.Equal("Hà", r.DoctorName); Assert.True(r.HasClinicalLink); });
        Assert.Empty((await repository.GetDataAsync(From, End, 8)).Invoices);
    }

    private static ClinicManagementDbContext MockContext(List<Appointment> appointments, List<Invoice> invoices)
    {
        var context = Context();
        context.Appointments = appointments.BuildMockDbSet().Object;
        context.Invoices = invoices.BuildMockDbSet().Object;
        return context;
    }
}
