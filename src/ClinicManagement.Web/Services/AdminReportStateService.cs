using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;
using System.Text.Json;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Report;
using ClinicManagementSystem.Application.DTOs.Scheduling;

namespace ClinicManagementSystem.Web.Services;

public class AdminReportStateService(AuthorizedApiService api)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private ClinicReportQueryDTO? _appliedQuery;
    public ClinicReportDTO? Report { get; private set; }
    public List<AdminDoctorOptionDTO> Doctors { get; private set; } = [];
    public bool IsLoadingDoctors { get; private set; }
    public bool IsLoadingReport { get; private set; }
    public bool IsExporting { get; private set; }
    public bool IsBusy => IsLoadingDoctors || IsLoadingReport || IsExporting;
    public bool RequiresLogin { get; private set; }
    public bool AccessDenied { get; private set; }
    public string DoctorsError { get; private set; } = string.Empty;
    public string ReportError { get; private set; } = string.Empty;
    public string ExportError { get; private set; } = string.Empty;
    public string ExportMessage { get; private set; } = string.Empty;

    public void Reset()
    {
        if (IsBusy) return;
        Report = null; _appliedQuery = null; Doctors = [];
        RequiresLogin = AccessDenied = false;
        DoctorsError = ReportError = ExportError = ExportMessage = string.Empty;
    }

    public bool MatchesAppliedFilter(ClinicReportQueryDTO query) => _appliedQuery != null
        && query.From == _appliedQuery.From && query.To == _appliedQuery.To && query.DoctorId == _appliedQuery.DoctorId;

    public async Task LoadDoctorsAsync()
    {
        if (IsBusy) return;
        IsLoadingDoctors = true; DoctorsError = string.Empty; Doctors = [];
        try
        {
            using var response = await api.GetAsync("/api/admin/scheduling/doctors");
            var result = await ReadAsync<List<AdminDoctorOptionDTO>>(response);
            if (!Succeeded(response, result)) { DoctorsError = Error(response, result); return; }
            Doctors = result!.Content!.DistinctBy(d => d.Id).OrderBy(d => d.FullName).ThenBy(d => d.Id).ToList();
            MarkAuthorized();
        }
        catch (Exception ex) when (IsRequestFailure(ex)) { DoctorsError = FailureMessage(ex); }
        finally { IsLoadingDoctors = false; }
    }

    public async Task LoadAsync(ClinicReportQueryDTO query)
    {
        if (IsBusy) return;
        Report = null; _appliedQuery = null;
        ReportError = ExportError = ExportMessage = string.Empty;
        var validation = new List<ValidationResult>();
        if (query == null || !Validator.TryValidateObject(query, new ValidationContext(query), validation, true))
        {
            ReportError = query == null ? "Vui lòng chọn khoảng ngày báo cáo." : string.Join(" ", validation.Select(v => v.ErrorMessage));
            return;
        }
        // Keep request metadata independent of subsequent edits to the Razor form.
        var snapshot = new ClinicReportQueryDTO { From = query.From, To = query.To, DoctorId = query.DoctorId };
        IsLoadingReport = true;
        try
        {
            using var response = await api.GetAsync(QueryUri("/api/admin/reports", snapshot));
            var result = await ReadAsync<ClinicReportDTO>(response);
            if (!Succeeded(response, result)) { ReportError = Error(response, result); return; }
            var report = result!.Content!;
            if (report.From != snapshot.From || report.To != snapshot.To || report.DoctorId != snapshot.DoctorId
                || report.Doctors == null || report.RevenueItems == null)
                throw new JsonException("Unexpected report metadata.");
            Report = report; _appliedQuery = snapshot;
            MarkAuthorized();
        }
        catch (Exception ex) when (IsRequestFailure(ex)) { ReportError = FailureMessage(ex); }
        finally { IsLoadingReport = false; }
    }

    public async Task<bool> ExportAsync(Func<ClinicReportExportDTO, Task> download)
    {
        if (IsBusy || RequiresLogin || AccessDenied) return false;
        ExportError = ExportMessage = string.Empty;
        if (Report == null || _appliedQuery == null)
        {
            ExportError = "Vui lòng chạy báo cáo thành công trước khi xuất Excel.";
            return false;
        }
        ArgumentNullException.ThrowIfNull(download);
        var query = new ClinicReportQueryDTO { From = _appliedQuery.From, To = _appliedQuery.To, DoctorId = _appliedQuery.DoctorId };
        IsExporting = true;
        try
        {
            using var response = await api.GetAsync(QueryUri("/api/admin/reports/export", query));
            if (!response.IsSuccessStatusCode)
            {
                ExportError = Error(response, await ReadAsync<ClinicReportExportDTO>(response));
                return false;
            }
            if (!string.Equals(response.Content.Headers.ContentType?.MediaType, ClinicReportExportDTO.ContentType, StringComparison.OrdinalIgnoreCase))
            {
                var error = await ReadAsync<ClinicReportExportDTO>(response);
                if (error?.StatusCode >= 400) { ExportError = Error(response, error); return false; }
                throw new JsonException("Expected an Excel response.");
            }
            var bytes = await response.Content.ReadAsByteArrayAsync();
            if (bytes.Length < 4 || bytes[0] != 'P' || bytes[1] != 'K') throw new JsonException("Unexpected workbook content.");
            var headerName = response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName;
            var file = new ClinicReportExportDTO { Bytes = bytes, FileName = FileName(headerName, query) };
            // Stay busy until the browser has consumed the stream, preventing duplicate downloads.
            try { await download(file); }
            catch (Exception)
            {
                ExportError = "Trình duyệt chưa tải được file Excel. Vui lòng thử lại.";
                return false;
            }
            ExportMessage = "Đã gửi file Excel đến trình duyệt. Kiểm tra mục Tải xuống.";
            MarkAuthorized();
            return true;
        }
        catch (Exception ex) when (IsRequestFailure(ex)) { ExportError = FailureMessage(ex); return false; }
        finally { IsExporting = false; }
    }

    private static string QueryUri(string path, ClinicReportQueryDTO query)
    {
        var uri = $"{path}?from={query.From!.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}&to={query.To!.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
        if (query.DoctorId.HasValue) uri += "&doctorId=" + query.DoctorId.Value.ToString(CultureInfo.InvariantCulture);
        return uri;
    }

    private static string FileName(string? header, ClinicReportQueryDTO query)
    {
        var name = header?.Trim('"');
        if (!string.IsNullOrEmpty(name) && name.Length <= 180 && name.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.')
            && name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)) return name;
        return $"clinic-report_{query.From:yyyy-MM-dd}_{query.To:yyyy-MM-dd}"
            + (query.DoctorId.HasValue ? $"_doctor-{query.DoctorId.Value}" : "_all-doctors") + ".xlsx";
    }

    private void MarkAuthorized() => RequiresLogin = AccessDenied = false;
    private string Error<T>(HttpResponseMessage response, HttpResponseData<T>? result)
    {
        RequiresLogin = response.StatusCode == HttpStatusCode.Unauthorized || result?.StatusCode == 401;
        AccessDenied = response.StatusCode == HttpStatusCode.Forbidden || result?.StatusCode == 403;
        if (RequiresLogin || AccessDenied) { Report = null; _appliedQuery = null; Doctors = []; }
        if (RequiresLogin) return "Phiên đăng nhập không còn hợp lệ. Vui lòng đăng nhập lại.";
        if (AccessDenied) return "Bạn không có quyền xem báo cáo phòng khám.";
        return !string.IsNullOrWhiteSpace(result?.Message) ? result.Message : response.StatusCode switch
        {
            HttpStatusCode.NotFound => "Không tìm thấy bác sĩ. Vui lòng kiểm tra bộ lọc.",
            _ => "Không thể tải báo cáo hoặc file Excel. Vui lòng thử lại."
        };
    }

    private static bool Succeeded<T>(HttpResponseMessage response, HttpResponseData<T>? result)
        => response.IsSuccessStatusCode && result is { StatusCode: >= 200 and < 300, Content: not null };
    private static bool IsRequestFailure(Exception ex) => ex is HttpRequestException or JsonException or TaskCanceledException;
    private static string FailureMessage(Exception ex) => ex switch
    {
        HttpRequestException => "Không thể kết nối đến hệ thống. Vui lòng thử lại.",
        TaskCanceledException => "Yêu cầu quá thời gian chờ. Vui lòng thử lại.",
        _ => "Phản hồi hệ thống không hợp lệ. Vui lòng thử lại."
    };

    private static async Task<HttpResponseData<T>?> ReadAsync<T>(HttpResponseMessage response)
    {
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) return null;
        var text = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(text)) return null;
        using var json = JsonDocument.Parse(text);
        if (json.RootElement.ValueKind != JsonValueKind.Object) throw new JsonException("Expected response object.");
        if (json.RootElement.TryGetProperty("statusCode", out _)) return JsonSerializer.Deserialize<HttpResponseData<T>>(text, JsonOptions);
        if (json.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
        {
            var messages = errors.EnumerateObject().Where(p => p.Value.ValueKind == JsonValueKind.Array)
                .SelectMany(p => p.Value.EnumerateArray()).Where(v => v.ValueKind == JsonValueKind.String).Select(v => v.GetString());
            return new() { StatusCode = (int)response.StatusCode, Message = string.Join(" ", messages) };
        }
        return null;
    }
}
