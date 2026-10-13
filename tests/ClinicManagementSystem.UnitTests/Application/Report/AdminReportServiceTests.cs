using ClinicManagementSystem.Application.DTOs.Report;
using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Models.Reports;
using ClinicManagementSystem.Infrastructure.Repositories;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ClinicManagementSystem.UnitTests.Application.Reports;

public class AdminReportServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IClinicReportRepository> _repository = new();
    private readonly AdminReportService _service;
    private ClinicReportData _data = new();
    private static readonly DateTime From = new(2026, 10, 1);
    private static readonly DateTime End = new(2026, 11, 1);
    private static readonly DateTime Now = new(2026, 10, 13, 1, 2, 3, DateTimeKind.Utc);

    public AdminReportServiceTests()
    {
        _uow.Setup(u => u.ClinicReportRepository).Returns(_repository.Object);
        _repository.Setup(r => r.DoctorExistsAsync(It.IsAny<int>())).ReturnsAsync(true);
        _repository.Setup(r => r.GetDataAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>())).ReturnsAsync(() => _data);
        _service = new(_uow.Object, NullLogger<AdminReportService>.Instance, new FixedClock());
    }
    private sealed class FixedClock : TimeProvider { public override DateTimeOffset GetUtcNow() => new(Now); }
    private static ClinicReportQueryDTO Query(int? doctor = null) => new() { From = new(2026, 10, 1), To = new(2026, 10, 31), DoctorId = doctor };
    private static ClinicReportInvoiceData Invoice(int id = 1) => new()
    {
        Id = id, CreatedAt = From, DoctorId = 7, DoctorName = "Lê Thu Hà", HasClinicalLink = true,
        TotalAmount = 1000, TaxAmount = 100, DiscountAmount = 100, InsuranceAmount = 200,
        CollectedInPeriod = 400, PaidThroughEnd = 400,
        Items = [new() { Id = 1, ServiceId = 4, Description = "Xét nghiệm (giá cũ)", Amount = 300 },
            new() { Id = 2, MedicineId = 9, Description = "Thuốc (giá cũ)", Amount = 200 }]
    };

    [Theory]
    [InlineData(null, "2026-10-31", null)]
    [InlineData("2026-10-01", null, null)]
    [InlineData("2026-10-02", "2026-10-01", null)]
    [InlineData("2026-01-01", "2027-01-02", null)]
    [InlineData("9999-12-31", "9999-12-31", null)]
    [InlineData("2026-10-01", "2026-10-31", 0)]
    [InlineData("2026-10-01", "2026-10-31", -1)]
    public async Task InvalidQuery_DoesNotQueryDatabase(string? from, string? to, int? doctor)
    {
        var query = new ClinicReportQueryDTO { From = from == null ? null : DateOnly.Parse(from), To = to == null ? null : DateOnly.Parse(to), DoctorId = doctor };
        var result = await _service.GetReportAsync(query);
        Assert.Equal(400, result.StatusCode); Assert.Null(result.Content);
        _repository.Verify(r => r.GetDataAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task NullQuery_IsRejected()
    {
        Assert.Equal(400, (await _service.GetReportAsync(null!)).StatusCode);
        Assert.Equal(400, (await _service.ExportAsync(null!)).StatusCode);
    }

    [Fact]
    public async Task UnknownDoctor_Is404ForReportAndExport()
    {
        _repository.Setup(r => r.DoctorExistsAsync(99)).ReturnsAsync(false);
        Assert.Equal(404, (await _service.GetReportAsync(Query(99))).StatusCode);
        Assert.Equal(404, (await _service.ExportAsync(Query(99))).StatusCode);
        _repository.Verify(r => r.GetDataAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>()), Times.Never);
    }

    [Theory]
    [InlineData("2026-10-31", "2026-10-31", "2026-11-01")]
    [InlineData("2026-01-01", "2027-01-01", "2027-01-02")]
    public async Task ValidRanges_UseInclusiveEndDayAndForwardDoctorFilter(string from, string to, string end)
    {
        var result = await _service.GetReportAsync(new() { From = DateOnly.Parse(from), To = DateOnly.Parse(to), DoctorId = 7 });
        Assert.Equal(200, result.StatusCode); Assert.Equal(7, result.Content!.DoctorId);
        _repository.Verify(r => r.GetDataAsync(DateTime.Parse(from), DateTime.Parse(end), 7), Times.Once);
    }

    [Fact]
    public async Task EmptyReport_HasZeroTotalsAndRate()
    {
        var result = await _service.GetReportAsync(Query());
        Assert.Equal(200, result.StatusCode); Assert.Equal(Now, result.Content!.GeneratedAtUtc);
        Assert.Equal(0, result.Content.TotalVisits); Assert.Equal(0, result.Content.NoShowRate);
        Assert.Equal(0, result.Content.TotalRevenue); Assert.Equal(0, result.Content.OutstandingDebt);
        Assert.Empty(result.Content.Doctors); Assert.Empty(result.Content.RevenueItems);
    }

    [Fact]
    public async Task VisitsAndNoShowRate_UseCompletedPlusNoShow_NotMeanOfDoctorRates()
    {
        _data.Visits = [new() { DoctorId = 7, DoctorName = "Hà", CompletedCount = 2, NoShowCount = 1 },
            new() { DoctorId = 8, DoctorName = "An", CompletedCount = 1, NoShowCount = 0 }];
        var r = (await _service.GetReportAsync(Query())).Content!;
        Assert.Equal(3, r.TotalVisits); Assert.Equal(1, r.NoShowCount); Assert.Equal(4, r.ConcludedAppointments);
        Assert.Equal(25m, r.NoShowRate);
        Assert.Equal(33.33m, r.Doctors.Single(d => d.DoctorId == 7).NoShowRate);
    }

    [Fact]
    public async Task PartialPayment_DiscountTaxInsurance_UsesCashAndInvoiceSnapshots()
    {
        _data.Invoices = [Invoice()];
        var r = (await _service.GetReportAsync(Query())).Content!;
        Assert.Equal(400m, r.CollectedAmount); Assert.Equal(0, r.RefundedAmount);
        Assert.Equal(400m, r.TotalRevenue); Assert.Equal(400m, r.OutstandingDebt);
        Assert.Equal(1, r.OutstandingInvoiceCount); Assert.Equal(200m, r.ConsultationRevenue);
        Assert.Equal(120m, r.ServiceRevenue); Assert.Equal(80m, r.MedicineRevenue);
        Assert.Contains(r.RevenueItems, i => i.Description == "Xét nghiệm (giá cũ)" && i.Revenue == 120);
        AssertReconciles(r);
    }

    [Fact]
    public async Task OldInvoicePaidThisPeriod_IncludesCashButNotCohortDebtOrVisits()
    {
        var old = Invoice(); old.CreatedAt = From.AddDays(-1);
        _data.Invoices = [old];
        var r = (await _service.GetReportAsync(Query())).Content!;
        Assert.Equal(400m, r.TotalRevenue); Assert.Equal(0, r.OutstandingDebt);
        Assert.Equal(0, r.TotalVisits); Assert.Equal(0, r.NoShowRate); AssertReconciles(r);
    }

    [Fact]
    public async Task RefundOnly_CanProduceNegativeRevenue_WithoutDoubleNegating()
    {
        var i = Invoice(); i.CollectedInPeriod = 0; i.RefundedInPeriod = 100; i.PaidThroughEnd = 300;
        _data.Invoices = [i];
        var r = (await _service.GetReportAsync(Query())).Content!;
        Assert.Equal(-100m, r.TotalRevenue); Assert.Equal(100m, r.RefundedAmount);
        Assert.Equal(-50m, r.ConsultationRevenue); Assert.Equal(-30m, r.ServiceRevenue); Assert.Equal(-20m, r.MedicineRevenue);
        Assert.Equal(500m, r.OutstandingDebt); AssertReconciles(r);
    }

    [Fact]
    public async Task CollectionAndFullRefund_KeepGrossAmountsButNetRevenueIsZero()
    {
        var i = Invoice(); i.RefundedInPeriod = 400; i.PaidThroughEnd = 0;
        _data.Invoices = [i];
        var r = (await _service.GetReportAsync(Query())).Content!;
        Assert.Equal(400m, r.CollectedAmount); Assert.Equal(400m, r.RefundedAmount);
        Assert.Equal(0, r.TotalRevenue); Assert.Equal(800m, r.OutstandingDebt); AssertReconciles(r);
    }

    [Theory]
    [InlineData("2026-10-31", 0)]
    [InlineData("2026-11-01", 400)]
    [InlineData("2026-11-02", 400)]
    public async Task CancelledInvoices_UseCancellationDateForHistoricalDebt_StillIncludeCash(string cancelledAt, int debt)
    {
        var i = Invoice(); i.Status = 3; i.CancelledAt = DateTime.Parse(cancelledAt);
        _data.Invoices = [i];
        var r = (await _service.GetReportAsync(Query())).Content!;
        Assert.Equal(debt, r.OutstandingDebt); Assert.Equal(400m, r.TotalRevenue); AssertReconciles(r);
    }

    [Fact]
    public async Task HistoricalDebt_UsesLedgerThroughEnd_EvenIfCurrentInvoiceIsPaid()
    {
        var i = Invoice(); i.Status = 2;
        _data.Invoices = [i];
        var r = (await _service.GetReportAsync(Query())).Content!;
        Assert.Equal(400m, r.OutstandingDebt);
        i.PaidThroughEnd = 900;
        Assert.Equal(0, (await _service.GetReportAsync(Query())).Content!.OutstandingDebt);
    }

    [Fact]
    public async Task UnlinkedInvoice_IsExplicitOtherBucket_AndStillReconciles()
    {
        _data.Invoices = [new() { Id = 8, DoctorName = "Chưa gắn bác sĩ", CreatedAt = From, TotalAmount = 100, CollectedInPeriod = 30, PaidThroughEnd = 30 }];
        var r = (await _service.GetReportAsync(Query())).Content!;
        var d = Assert.Single(r.Doctors); Assert.Null(d.DoctorId);
        Assert.Equal(30m, r.OtherRevenue); Assert.Equal(0, r.ConsultationRevenue); Assert.Equal(70m, r.OutstandingDebt); AssertReconciles(r);
    }

    [Fact]
    public async Task MultipleInvoicesAndRounding_KeepExactTotals_AndDifferentSnapshots()
    {
        var a = Invoice(); a.TotalAmount = 3; a.DiscountAmount = a.TaxAmount = a.InsuranceAmount = 0;
        a.CollectedInPeriod = a.PaidThroughEnd = 0.01m;
        a.Items = [new() { Id = 1, ServiceId = 4, Description = "Tên cũ", Amount = 1 }, new() { Id = 2, MedicineId = 9, Description = "Thuốc", Amount = 1 }];
        var b = Invoice(2); b.Items[0].Description = "Tên mới";
        _data.Invoices = [a, b];
        var r = (await _service.GetReportAsync(Query())).Content!;
        Assert.Equal(400.01m, r.TotalRevenue);
        Assert.Contains(r.RevenueItems, i => i.Description == "Tên cũ");
        Assert.Contains(r.RevenueItems, i => i.Description == "Tên mới"); AssertReconciles(r);
    }

    [Fact]
    public async Task ZeroTotalInvoice_WithCash_HasOtherRevenueFallback()
    {
        _data.Invoices = [new() { CreatedAt = From, CollectedInPeriod = 1, PaidThroughEnd = 1, DoctorName = "Chưa gắn bác sĩ" }];
        var r = (await _service.GetReportAsync(Query())).Content!;
        Assert.Equal(1m, r.OtherRevenue); Assert.Equal(0, r.OutstandingDebt); AssertReconciles(r);
    }

    [Theory]
    [InlineData(null, "_all-doctors.xlsx")]
    [InlineData(7, "_doctor-7.xlsx")]
    public async Task Export_UsesSameQuery_AndReturnsXlsxFile(int? doctor, string suffix)
    {
        var r = await _service.ExportAsync(Query(doctor));
        Assert.Equal(200, r.StatusCode); Assert.NotEmpty(r.Content!.Bytes);
        Assert.Equal("PK", System.Text.Encoding.ASCII.GetString(r.Content.Bytes[..2]));
        Assert.Equal("clinic-report_2026-10-01_2026-10-31" + suffix, r.Content.FileName);
        _repository.Verify(x => x.GetDataAsync(From, End, doctor), Times.Once);
    }

    [Fact]
    public async Task DatabaseFailure_IsGeneric500ForBothEndpoints()
    {
        _repository.Setup(x => x.GetDataAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>())).ThrowsAsync(new Exception("secret connection string"));
        var report = await _service.GetReportAsync(Query()); var export = await _service.ExportAsync(Query());
        Assert.Equal(500, report.StatusCode); Assert.Equal(500, export.StatusCode);
        Assert.Null(report.Content); Assert.Null(export.Content);
        Assert.DoesNotContain("secret", report.Message); Assert.DoesNotContain("secret", export.Message);
    }

    private static void AssertReconciles(ClinicReportDTO r)
    {
        Assert.Equal(r.TotalRevenue, r.CollectedAmount - r.RefundedAmount);
        Assert.Equal(r.TotalRevenue, r.ConsultationRevenue + r.ServiceRevenue + r.MedicineRevenue + r.OtherRevenue);
        Assert.Equal(r.TotalRevenue, r.Doctors.Sum(d => d.TotalRevenue));
        Assert.Equal(r.TotalRevenue, r.RevenueItems.Sum(i => i.Revenue));
        Assert.Equal(r.OutstandingDebt, r.Doctors.Sum(d => d.OutstandingDebt));
        foreach (var d in r.Doctors) Assert.Equal(d.TotalRevenue, d.ConsultationRevenue + d.ServiceRevenue + d.MedicineRevenue + d.OtherRevenue);
    }
}
