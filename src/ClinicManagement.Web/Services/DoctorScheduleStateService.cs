using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Doctor;

namespace ClinicManagementSystem.Web.Services;

public sealed class DoctorScheduleStateService(AuthorizedApiService api)
{
    public DoctorScheduleDTO? Schedule { get; private set; }
    public bool IsLoadingWeek { get; private set; }
    public string WeekError { get; private set; } = string.Empty;
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public byte? Scope { get; set; }
    public byte? Type { get; set; }
    public int PageSize { get; set; } = 10;
    public PagedResult<DoctorTimeOffDTO> TimeOff { get; private set; } = new() { Page = 1, PageSize = 10 };
    public bool IsLoadingTimeOff { get; private set; }
    public bool HasLoadedTimeOff { get; private set; }
    public string TimeOffError { get; private set; } = string.Empty;
    public bool HasUnappliedFilters => HasLoadedTimeOff && _appliedFilter != FilterKey;
    public event Action? OnChange;
    private int _weekVersion, _leaveVersion;
    private string? _appliedFilter;
    private string FilterKey => $"{From}|{To}|{Scope}|{Type}|{PageSize}";

    public async Task LoadWeekAsync(DateOnly? anchor = null)
    {
        var version = ++_weekVersion;
        WeekError = string.Empty;
        Schedule = null;
        IsLoadingWeek = true;
        Notify();
        try
        {
            var url = "/api/doctors/me/schedule";
            if (anchor.HasValue)
            {
                var start = anchor.Value.AddDays(-((int)anchor.Value.DayOfWeek + 6) % 7);
                if (start.DayNumber > DateOnly.MaxValue.DayNumber - 7)
                {
                    WeekError = "Tuần được chọn nằm ngoài khoảng ngày hỗ trợ.";
                    return;
                }
                url += $"?from={Date(start)}&to={Date(start.AddDays(6))}";
            }
            var (content, error) = await ReadAsync<DoctorScheduleDTO>(url);
            if (version != _weekVersion) return;
            Schedule = content; WeekError = error;
        }
        catch
        {
            if (version == _weekVersion) WeekError = "Không thể kết nối để tải lịch làm việc. Vui lòng thử lại.";
        }
        finally { if (version == _weekVersion) { IsLoadingWeek = false; Notify(); } }
    }

    public async Task LoadTimeOffAsync(int page = 1)
    {
        var version = ++_leaveVersion;
        TimeOffError = string.Empty;
        var filter = FilterKey;
        var query = new DoctorTimeOffQueryDTO { From = From, To = To, Scope = Scope, Type = Type, Page = page, PageSize = PageSize };
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(query, new ValidationContext(query), errors, true))
        {
            TimeOffError = errors[0].ErrorMessage ?? "Bộ lọc không hợp lệ.";
            IsLoadingTimeOff = false; Notify(); return;
        }
        TimeOff = new() { Page = 1, PageSize = query.PageSize };
        IsLoadingTimeOff = true; Notify();
        try
        {
            var url = $"/api/doctors/me/time-off?page={query.Page}&pageSize={query.PageSize}";
            if (query.From.HasValue) url += $"&from={Date(query.From.Value)}&to={Date(query.To!.Value)}";
            if (query.Scope.HasValue) url += "&scope=" + query.Scope.Value;
            if (query.Type.HasValue) url += "&type=" + query.Type.Value;
            var (content, error) = await ReadAsync<PagedResult<DoctorTimeOffDTO>>(url);
            if (version != _leaveVersion) return;
            TimeOffError = error;
            if (content != null) { TimeOff = content; _appliedFilter = filter; HasLoadedTimeOff = true; }
        }
        catch
        {
            if (version == _leaveVersion) TimeOffError = "Không thể kết nối để tải lịch nghỉ. Vui lòng thử lại.";
        }
        finally { if (version == _leaveVersion) { IsLoadingTimeOff = false; Notify(); } }
    }

    public void Reset()
    {
        ++_weekVersion; ++_leaveVersion;
        Schedule = null; WeekError = TimeOffError = string.Empty;
        IsLoadingWeek = IsLoadingTimeOff = HasLoadedTimeOff = false;
        From = To = null; Scope = Type = null; PageSize = 10; _appliedFilter = null;
        TimeOff = new() { Page = 1, PageSize = 10 }; Notify();
    }

    private async Task<(T? Content, string Error)> ReadAsync<T>(string url)
    {
        using var response = await api.GetAsync(url);
        if (response.StatusCode == HttpStatusCode.Unauthorized) return (default, "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
        if (response.StatusCode == HttpStatusCode.Forbidden) return (default, "Bạn không có quyền xem lịch bác sĩ hoặc hồ sơ bác sĩ chưa hoạt động.");
        var data = await response.Content.ReadFromJsonAsync<HttpResponseData<T>>();
        if (!response.IsSuccessStatusCode || data == null || data.StatusCode != 200 || data.Content == null)
            return (default, string.IsNullOrWhiteSpace(data?.Message) ? "Không thể tải dữ liệu. Vui lòng thử lại." : data.Message);
        return (data.Content, string.Empty);
    }
    private static string Date(DateOnly value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private void Notify() => OnChange?.Invoke();
}
