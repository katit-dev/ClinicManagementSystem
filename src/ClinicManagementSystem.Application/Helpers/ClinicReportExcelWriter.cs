using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using ClinicManagementSystem.Application.DTOs.Report;

namespace ClinicManagementSystem.Application.Helpers;

// Small OOXML workbook: no formulas, macros, external links or temporary server files.
public static class ClinicReportExcelWriter
{
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace ContentTypes = "http://schemas.openxmlformats.org/package/2006/content-types";
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private record Cell(object? Value, int Style = 0);

    public static byte[] Create(ClinicReportDTO report)
    {
        ArgumentNullException.ThrowIfNull(report);
        List<Cell[]> summary =
        [
            [new("BÁO CÁO DOANH THU & LƯỢT KHÁM", 5)],
            [new("Từ ngày"), new(report.From.ToString("yyyy-MM-dd", Invariant))],
            [new("Đến ngày (bao gồm)"), new(report.To.ToString("yyyy-MM-dd", Invariant))],
            [new("Bác sĩ"), new(report.DoctorId.HasValue ? $"#{report.DoctorId}" : "Tất cả bác sĩ")],
            [new("Tạo lúc (UTC)"), new(report.GeneratedAtUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", Invariant))],
            [new("Chỉ số", 1), new("Giá trị", 1)],
            [new("Lượt khám (Completed)"), new(report.TotalVisits, 3)],
            [new("Không đến (NoShow)"), new(report.NoShowCount, 3)],
            [new("Completed + NoShow"), new(report.ConcludedAppointments, 3)],
            [new("Tỷ lệ không đến"), new(report.NoShowRate, 4)],
            [new("Tiền thu trong kỳ (VND)"), new(report.CollectedAmount, 2)],
            [new("Tiền hoàn trong kỳ (VND)"), new(report.RefundedAmount, 2)],
            [new("Doanh thu thực thu (VND)"), new(report.TotalRevenue, 2)],
            [new("Công nợ hóa đơn phát hành trong kỳ (VND)"), new(report.OutstandingDebt, 2)],
            [new("Số hóa đơn còn nợ"), new(report.OutstandingInvoiceCount, 3)],
            [new("Doanh thu khám (VND)"), new(report.ConsultationRevenue, 2)],
            [new("Doanh thu CLS (VND)"), new(report.ServiceRevenue, 2)],
            [new("Doanh thu thuốc (VND)"), new(report.MedicineRevenue, 2)],
            [new("Doanh thu khác (VND)"), new(report.OtherRevenue, 2)],
            [new("Cách tính", 1), new("Quy tắc", 1)],
            [new("Lượt khám", 6), new("Theo ngày hẹn StartTime; chỉ tính Completed. Tỷ lệ = NoShow / (Completed + NoShow); mẫu số bằng 0 trả 0%.", 6)],
            [new("Doanh thu", 6), new("Theo PaidAt: tiền thu trừ tiền hoàn. Phân bổ theo giá snapshot trên hóa đơn; không dùng giá danh mục hiện tại.", 6)],
            [new("Công nợ", 6), new("Hóa đơn phát hành trong khoảng ngày chọn, trừ thanh toán đến hết ngày cuối; loại hóa đơn đã hủy trước khi kỳ kết thúc. Không bao gồm nợ hóa đơn phát hành trước kỳ.", 6)],
            [new("Ngày giờ", 6), new("Lọc theo timestamps đang lưu trong database; không tự chuyển múi giờ. GeneratedAtUtc là UTC.", 6)]
        ];
        var doctorRows = new List<Cell[]> { Headers("Mã bác sĩ", "Bác sĩ", "Lượt khám", "Không đến", "Completed + NoShow", "Tỷ lệ không đến", "Doanh thu khám (VND)", "Doanh thu CLS (VND)", "Doanh thu thuốc (VND)", "Doanh thu khác (VND)", "Tổng doanh thu (VND)", "Công nợ (VND)") };
        doctorRows.AddRange(report.Doctors.Select(d => new Cell[]
        {
            new(d.DoctorId, 3), new(d.DoctorName), new(d.TotalVisits, 3), new(d.NoShowCount, 3),
            new(d.ConcludedAppointments, 3), new(d.NoShowRate, 4), new(d.ConsultationRevenue, 2),
            new(d.ServiceRevenue, 2), new(d.MedicineRevenue, 2), new(d.OtherRevenue, 2), new(d.TotalRevenue, 2), new(d.OutstandingDebt, 2)
        }));
        var itemRows = new List<Cell[]> { Headers("Nhóm", "Mã nguồn", "Mô tả snapshot", "Doanh thu thực thu (VND)") };
        itemRows.AddRange(report.RevenueItems.Select(i => new Cell[] { new(CategoryName(i.Category)), new(i.SourceId, 3), new(i.Description), new(i.Revenue, 2) }));
        string[] names = ["Tổng quan", "Theo bác sĩ", "Dịch vụ & thuốc"];
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            Write(archive, "[Content_Types].xml", new(ContentTypes + "Types",
                new XElement(ContentTypes + "Default", new XAttribute("Extension", "rels"), new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
                new XElement(ContentTypes + "Default", new XAttribute("Extension", "xml"), new XAttribute("ContentType", "application/xml")),
                Override("/xl/workbook.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"),
                Override("/xl/styles.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"),
                Enumerable.Range(1, 3).Select(n => Override($"/xl/worksheets/sheet{n}.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"))));
            Write(archive, "_rels/.rels", new(PackageRelationships + "Relationships", Relationship("rId1", "officeDocument", "xl/workbook.xml")));
            Write(archive, "xl/workbook.xml", new(Main + "workbook", new XAttribute(XNamespace.Xmlns + "r", Relationships),
                new XElement(Main + "sheets", names.Select((name, index) => new XElement(Main + "sheet", new XAttribute("name", name), new XAttribute("sheetId", index + 1), new XAttribute(Relationships + "id", $"rId{index + 1}"))))));
            Write(archive, "xl/_rels/workbook.xml.rels", new(PackageRelationships + "Relationships",
                Enumerable.Range(1, 3).Select(n => Relationship($"rId{n}", "worksheet", $"worksheets/sheet{n}.xml")), Relationship("rId4", "styles", "styles.xml")));
            Write(archive, "xl/styles.xml", Styles());
            Write(archive, "xl/worksheets/sheet1.xml", Sheet(summary, [48, 100], false));
            Write(archive, "xl/worksheets/sheet2.xml", Sheet(doctorRows, [16, 30, 15, 15, 24, 22, 26, 26, 26, 26, 26, 26], true));
            Write(archive, "xl/worksheets/sheet3.xml", Sheet(itemRows, [22, 16, 60, 30], true));
        }
        return output.ToArray();
    }

    private static Cell[] Headers(params string[] values) => values.Select(v => new Cell(v, 1)).ToArray();
    private static string CategoryName(string category) => category switch { "Consultation" => "Khám", "Service" => "Dịch vụ CLS", "Medicine" => "Thuốc", _ => "Khác" };
    private static XElement Override(string part, string type) => new(ContentTypes + "Override", new XAttribute("PartName", part), new XAttribute("ContentType", type));
    private static XElement Relationship(string id, string type, string target) => new(PackageRelationships + "Relationship",
        new XAttribute("Id", id), new XAttribute("Type", Relationships.NamespaceName + "/" + type), new XAttribute("Target", target));

    private static XElement Sheet(List<Cell[]> rows, int[] widths, bool filter)
    {
        var sheet = new XElement(Main + "worksheet",
            new XElement(Main + "sheetViews", new XElement(Main + "sheetView", new XAttribute("workbookViewId", 0),
                filter ? new XElement(Main + "pane", new XAttribute("ySplit", 1), new XAttribute("topLeftCell", "A2"), new XAttribute("activePane", "bottomLeft"), new XAttribute("state", "frozen")) : null)),
            new XElement(Main + "cols", widths.Select((w, i) => new XElement(Main + "col", new XAttribute("min", i + 1), new XAttribute("max", i + 1), new XAttribute("width", w), new XAttribute("customWidth", 1)))),
            new XElement(Main + "sheetData", rows.Select((cells, r) => new XElement(Main + "row", new XAttribute("r", r + 1),
                cells.Any(c => c.Style == 6) ? new XAttribute("ht", 60) : new XAttribute("ht", r == 0 ? 32 : 24), new XAttribute("customHeight", 1),
                cells.Select((cell, col) => CellElement(cell, $"{Column(col + 1)}{r + 1}"))))));
        if (filter) sheet.Add(new XElement(Main + "autoFilter", new XAttribute("ref", $"A1:{Column(widths.Length)}{rows.Count}")));
        else sheet.Add(new XElement(Main + "mergeCells", new XAttribute("count", 1), new XElement(Main + "mergeCell", new XAttribute("ref", "A1:B1"))));
        return sheet;
    }

    private static XElement CellElement(Cell cell, string address)
    {
        var element = new XElement(Main + "c", new XAttribute("r", address), new XAttribute("s", cell.Style));
        if (cell.Value is null) return element;
        if (cell.Value is decimal or int or long)
            element.Add(new XAttribute("t", "n"), new XElement(Main + "v", Convert.ToString(cell.Value, Invariant)));
        else
            element.Add(new XAttribute("t", "inlineStr"), new XElement(Main + "is", new XElement(Main + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), CleanText(Convert.ToString(cell.Value, Invariant) ?? ""))));
        return element;
    }

    private static string CleanText(string value)
    {
        var result = new StringBuilder();
        foreach (var rune in value.EnumerateRunes())
        {
            var cp = rune.Value;
            if (cp is 9 or 10 or 13 || cp >= 0x20 && cp <= 0xD7FF || cp >= 0xE000 && cp <= 0xFFFD || cp >= 0x10000 && cp <= 0x10FFFF)
            {
                if (result.Length + rune.Utf16SequenceLength > 32767) break;
                result.Append(rune.ToString());
            }
            if (result.Length >= 32767) break;
        }
        return result.ToString();
    }

    private static string Column(int number)
    {
        var label = "";
        while (number > 0) { number--; label = (char)('A' + number % 26) + label; number /= 26; }
        return label;
    }

    private static XElement Styles()
    {
        XElement Font(bool bold, string color, int size) => new(Main + "font", bold ? new XElement(Main + "b") : null,
            new XElement(Main + "sz", new XAttribute("val", size)), new XElement(Main + "color", new XAttribute("rgb", color)), new XElement(Main + "name", new XAttribute("val", "Calibri")));
        XElement Format(int font, int fill, int number, bool wrap = false) => new(Main + "xf", new XAttribute("numFmtId", number), new XAttribute("fontId", font),
            new XAttribute("fillId", fill), new XAttribute("borderId", 0), new XAttribute("xfId", 0), new XAttribute("applyNumberFormat", 1), new XAttribute("applyFont", 1), new XAttribute("applyFill", 1),
            new XAttribute("applyAlignment", 1), new XElement(Main + "alignment", new XAttribute("vertical", "center"), new XAttribute("wrapText", wrap ? 1 : 0)));
        return new(Main + "styleSheet",
            new XElement(Main + "numFmts", new XAttribute("count", 1), new XElement(Main + "numFmt", new XAttribute("numFmtId", 164), new XAttribute("formatCode", "0.00\"%\""))),
            new XElement(Main + "fonts", new XAttribute("count", 3), Font(false, "FF183B56", 11), Font(true, "FFFFFFFF", 11), Font(true, "FF183B56", 18)),
            new XElement(Main + "fills", new XAttribute("count", 3), new XElement(Main + "fill", new XElement(Main + "patternFill", new XAttribute("patternType", "none"))),
                new XElement(Main + "fill", new XElement(Main + "patternFill", new XAttribute("patternType", "gray125"))),
                new XElement(Main + "fill", new XElement(Main + "patternFill", new XAttribute("patternType", "solid"), new XElement(Main + "fgColor", new XAttribute("rgb", "FF1674D1")), new XElement(Main + "bgColor", new XAttribute("indexed", 64))))),
            new XElement(Main + "borders", new XAttribute("count", 1), new XElement(Main + "border", new XElement(Main + "left"), new XElement(Main + "right"), new XElement(Main + "top"), new XElement(Main + "bottom"), new XElement(Main + "diagonal"))),
            new XElement(Main + "cellStyleXfs", new XAttribute("count", 1), new XElement(Main + "xf", new XAttribute("numFmtId", 0), new XAttribute("fontId", 0), new XAttribute("fillId", 0), new XAttribute("borderId", 0))),
            new XElement(Main + "cellXfs", new XAttribute("count", 7), Format(0, 0, 0), Format(1, 2, 0, true), Format(0, 0, 4), Format(0, 0, 1), Format(0, 0, 164), Format(2, 0, 0), Format(0, 0, 0, true)),
            new XElement(Main + "cellStyles", new XAttribute("count", 1), new XElement(Main + "cellStyle", new XAttribute("name", "Normal"), new XAttribute("xfId", 0), new XAttribute("builtinId", 0))));
    }

    private static void Write(ZipArchive archive, string path, XElement root)
    {
        using var stream = archive.CreateEntry(path, CompressionLevel.Optimal).Open();
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new UTF8Encoding(false) });
        new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root).Save(writer);
    }
}
