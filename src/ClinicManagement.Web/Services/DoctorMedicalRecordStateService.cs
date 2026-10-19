using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Doctor;

namespace ClinicManagementSystem.Web.Services;

public sealed class DoctorMedicalRecordStateService(AuthorizedApiService api)
{
    public string Keyword { get; set; } = string.Empty;
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public byte? Status { get; set; }
    public int PageSize { get; set; } = 20;
    public PagedResult<DoctorMedicalRecordDTO> Records { get; private set; } = new() { Page = 1, PageSize = 20 };
    public bool IsLoading { get; private set; }
    public bool HasLoaded { get; private set; }
    public string ErrorMessage { get; private set; } = string.Empty;
    public event Action? OnChange;
    private int _version;
    private string? _appliedFilter;
    private string FilterKey => $"{Keyword.Trim()}|{Status}|{From}|{To}|{PageSize}";
    public bool HasUnappliedFilters => HasLoaded && _appliedFilter != FilterKey;

    public async Task LoadAsync(int page = 1)
    {
        var version = ++_version;
        var filter = FilterKey;
        var query = new DoctorMedicalRecordQueryDTO { Keyword = Keyword.Trim(), Status = Status, From = From, To = To, Page = page, PageSize = PageSize };
        ErrorMessage = string.Empty;
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(query, new ValidationContext(query), errors, true))
        {
            ErrorMessage = errors[0].ErrorMessage ?? "Bộ lọc không hợp lệ.";
            IsLoading = false; Notify(); return;
        }
        IsLoading = true; Notify();
        try
        {
            var url = $"/api/doctors/me/medical-records?page={page}&pageSize={PageSize}";
            if (query.Keyword.Length > 0) url += "&keyword=" + Uri.EscapeDataString(query.Keyword);
            if (Status.HasValue) url += "&status=" + Status.Value;
            if (From.HasValue) url += "&from=" + From.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (To.HasValue) url += "&to=" + To.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            using var response = await api.GetAsync(url);
            if (version != _version) return;
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                Records = new() { Page = 1, PageSize = PageSize };
                ErrorMessage = response.StatusCode == HttpStatusCode.Unauthorized ? "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại." : "Bạn không có quyền truy cập bệnh án của bác sĩ.";
                return;
            }
            var data = await response.Content.ReadFromJsonAsync<HttpResponseData<PagedResult<DoctorMedicalRecordDTO>>>();
            if (version != _version) return;
            if (!response.IsSuccessStatusCode || data?.Content == null || data.StatusCode != 200)
            {
                Records = new() { Page = 1, PageSize = PageSize };
                ErrorMessage = string.IsNullOrWhiteSpace(data?.Message)
                    ? "Không thể tải danh sách bệnh án. Vui lòng thử lại." : data.Message;
                return;
            }
            Records = data.Content; _appliedFilter = filter; HasLoaded = true;
        }
        catch
        {
            if (version != _version) return;
            Records = new() { Page = 1, PageSize = PageSize };
            ErrorMessage = "Không thể kết nối để tải danh sách bệnh án. Vui lòng thử lại.";
        }
        finally { if (version == _version) { IsLoading = false; Notify(); } }
    }

    public void Reset()
    {
        ++_version; Keyword = string.Empty; From = null; To = null; Status = null; PageSize = 20;
        Records = new() { Page = 1, PageSize = 20 };
        IsLoading = false; HasLoaded = false; ErrorMessage = string.Empty; _appliedFilter = null; Notify();
    }
    private void Notify() => OnChange?.Invoke();
}
