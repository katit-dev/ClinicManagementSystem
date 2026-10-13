using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using ClinicManagementSystem.Application.DTOs.Report;
using ClinicManagementSystem.Application.Helpers;

namespace ClinicManagementSystem.UnitTests.Application.Reports;

public class ClinicReportExcelWriterTests
{
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static ZipArchive Workbook(ClinicReportDTO r) => new(new MemoryStream(ClinicReportExcelWriter.Create(r)), ZipArchiveMode.Read);
    private static XDocument Xml(ZipArchive zip, string name) { using var stream = zip.GetEntry(name)!.Open(); return XDocument.Load(stream); }

    [Fact]
    public void Workbook_IsValidXmlPackage_WithThreeSheetsAndResolvedRelationships()
    {
        using var zip = Workbook(new());
        Assert.Equal(8, zip.Entries.Count);
        foreach (var entry in zip.Entries) { using var stream = entry.Open(); Assert.NotNull(XDocument.Load(stream).Root); }
        var wb = Xml(zip, "xl/workbook.xml");
        Assert.Equal(new[] { "Tổng quan", "Theo bác sĩ", "Dịch vụ & thuốc" }, wb.Descendants(Main + "sheet").Select(s => s.Attribute("name")!.Value));
        var links = Xml(zip, "xl/_rels/workbook.xml.rels");
        foreach (var link in links.Root!.Elements()) Assert.NotNull(zip.GetEntry("xl/" + link.Attribute("Target")!.Value));
        var styles = Xml(zip, "xl/styles.xml");
        Assert.Contains(styles.Descendants(Main + "numFmt"), f => f.Attribute("formatCode")!.Value == "0.00\"%\"");
    }

    [Fact]
    public void NumbersStayNumeric_AndWorkbookMatchesReportValues()
    {
        var r = new ClinicReportDTO { TotalVisits = 2, NoShowRate = 33.33m, TotalRevenue = 123456.78m,
            Doctors = [new() { DoctorId = 7, DoctorName = "Lê Thu Hà", TotalVisits = 2, NoShowRate = 33.33m, TotalRevenue = 123456.78m }],
            RevenueItems = [new() { Category = "Service", SourceId = 4, Description = "Xét nghiệm", Revenue = 123456.78m }] };
        using var zip = Workbook(r);
        var summary = Xml(zip, "xl/worksheets/sheet1.xml");
        AssertCell(summary, "B7", "2"); AssertCell(summary, "B10", "33.33"); AssertCell(summary, "B13", "123456.78");
        var doctors = Xml(zip, "xl/worksheets/sheet2.xml");
        AssertCell(doctors, "C2", "2"); AssertCell(doctors, "F2", "33.33"); AssertCell(doctors, "K2", "123456.78");
        AssertCell(Xml(zip, "xl/worksheets/sheet3.xml"), "D2", "123456.78");
        Assert.Equal("4", doctors.Descendants(Main + "c").Single(c => c.Attribute("r")!.Value == "F2").Attribute("s")!.Value);
    }

    [Theory]
    [InlineData("=HYPERLINK(\"https://example.invalid\")")]
    [InlineData("+SUM(A1:A9)")]
    [InlineData("-1+2")]
    [InlineData("@SUM(A1:A9)")]
    [InlineData("Lê Thu Hà <b> & Thuốc 💊")]
    public void UserText_IsAlwaysLiteral_NotAnExcelFormula(string text)
    {
        using var zip = Workbook(new() { RevenueItems = [new() { Category = "Medicine", Description = text, Revenue = 10 }] });
        var sheet = Xml(zip, "xl/worksheets/sheet3.xml");
        var cell = sheet.Descendants(Main + "c").Single(c => c.Attribute("r")!.Value == "C2");
        Assert.Equal("inlineStr", cell.Attribute("t")!.Value); Assert.Equal(text, cell.Descendants(Main + "t").Single().Value);
        Assert.Empty(sheet.Descendants(Main + "f"));
    }

    [Fact]
    public void ControlCharacters_AreSanitized_WithoutLosingVietnamese()
    {
        using var zip = Workbook(new() { Doctors = [new() { DoctorName = "Lê\u0001 Thu\nHà" }] });
        Assert.Contains("Lê Thu\nHà", Xml(zip, "xl/worksheets/sheet2.xml").Descendants(Main + "t").Select(t => t.Value));
    }

    [Fact]
    public void WorkbookNumbers_DoNotDependOnCurrentCulture()
    {
        var before = CultureInfo.CurrentCulture;
        try { CultureInfo.CurrentCulture = new("vi-VN"); using var zip = Workbook(new() { TotalRevenue = 12.34m }); AssertCell(Xml(zip, "xl/worksheets/sheet1.xml"), "B13", "12.34"); }
        finally { CultureInfo.CurrentCulture = before; }
    }

    private static void AssertCell(XDocument sheet, string address, string value)
    {
        var cell = sheet.Descendants(Main + "c").Single(c => c.Attribute("r")!.Value == address);
        Assert.Equal("n", cell.Attribute("t")!.Value); Assert.Equal(value, cell.Element(Main + "v")!.Value);
    }
}
