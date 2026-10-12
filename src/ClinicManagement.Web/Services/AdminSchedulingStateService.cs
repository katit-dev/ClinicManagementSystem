using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Scheduling;

namespace ClinicManagementSystem.Web.Services;

public class AdminSchedulingStateService
{
    private readonly AuthorizedApiService _api;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private ScheduleRequestDTO? _pendingSchedule;
    private int? _pendingScheduleId;
    private TimeOffRequestDTO? _pendingTimeOff;

    public AdminSchedulingStateService(AuthorizedApiService api) => _api = api;

    public List<AdminDoctorOptionDTO> Doctors { get; private set; } = new();
    public PagedResult<AdminDoctorScheduleDTO> Schedules { get; private set; } = new();
    public PagedResult<AdminDoctorTimeOffDTO> TimeOffs { get; private set; } = new();
    public bool IsLoadingDoctors { get; private set; }
    public bool IsLoadingSchedules { get; private set; }
    public bool IsLoadingTimeOffs { get; private set; }
    public bool IsSubmitting { get; private set; }
    public bool IsBusy => IsLoadingDoctors || IsLoadingSchedules || IsLoadingTimeOffs || IsSubmitting;
    public string DoctorsError { get; private set; } = string.Empty;
    public string SchedulesError { get; private set; } = string.Empty;
    public string TimeOffsError { get; private set; } = string.Empty;
    public string ActionMessage { get; private set; } = string.Empty;
    public string ActionError { get; private set; } = string.Empty;
    public List<AffectedAppointmentDTO> AffectedAppointments { get; private set; } = new();
    public bool RequiresConfirmation => _pendingSchedule != null || _pendingTimeOff != null;
    public bool IsScheduleConfirmation => _pendingSchedule != null;

    public void ClearActions()
    {
        if (IsSubmitting) return;
        ActionMessage = ActionError = string.Empty;
        CancelConfirmation();
    }

    public void CancelConfirmation()
    {
        if (IsSubmitting) return;
        _pendingSchedule = null;
        _pendingScheduleId = null;
        _pendingTimeOff = null;
        AffectedAppointments = new();
    }

    public async Task LoadDoctorsAsync()
    {
        if (IsLoadingDoctors) return;
        IsLoadingDoctors = true;
        DoctorsError = string.Empty;
        try { Doctors = await GetAsync<List<AdminDoctorOptionDTO>>("/api/admin/scheduling/doctors") ?? new(); }
        catch (Exception ex) when (IsRequestError(ex)) { Doctors = new(); DoctorsError = Error(ex); }
        finally { IsLoadingDoctors = false; }
    }

    public async Task LoadSchedulesAsync(AdminScheduleQueryDTO query)
    {
        if (IsLoadingSchedules) return;
        IsLoadingSchedules = true;
        SchedulesError = string.Empty;
        var uri = $"/api/admin/doctor-schedules?page={query.Page}&pageSize={query.PageSize}";
        if (query.DoctorId.HasValue) uri += $"&doctorId={query.DoctorId}";
        if (query.Date.HasValue) uri += $"&date={query.Date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
        if (query.Active.HasValue) uri += $"&active={query.Active.Value.ToString().ToLowerInvariant()}";
        try { Schedules = await GetAsync<PagedResult<AdminDoctorScheduleDTO>>(uri) ?? new(); }
        catch (Exception ex) when (IsRequestError(ex)) { Schedules = new(); SchedulesError = Error(ex); }
        finally { IsLoadingSchedules = false; }
    }

    public async Task LoadTimeOffsAsync(AdminTimeOffQueryDTO query)
    {
        if (IsLoadingTimeOffs) return;
        IsLoadingTimeOffs = true;
        TimeOffsError = string.Empty;
        var uri = $"/api/admin/time-off?page={query.Page}&pageSize={query.PageSize}&clinicWideOnly={query.ClinicWideOnly.ToString().ToLowerInvariant()}";
        if (query.DoctorId.HasValue) uri += $"&doctorId={query.DoctorId}";
        if (query.From.HasValue) uri += $"&from={query.From.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
        if (query.To.HasValue) uri += $"&to={query.To.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
        try { TimeOffs = await GetAsync<PagedResult<AdminDoctorTimeOffDTO>>(uri) ?? new(); }
        catch (Exception ex) when (IsRequestError(ex)) { TimeOffs = new(); TimeOffsError = Error(ex); }
        finally { IsLoadingTimeOffs = false; }
    }

    public Task<bool> SaveScheduleAsync(int? id, ScheduleRequestDTO request)
        => SubmitScheduleAsync(id, Snapshot(request), false);

    public Task<bool> CreateTimeOffAsync(TimeOffRequestDTO request)
        => SubmitTimeOffAsync(Snapshot(request), false);

    // Keep the exact reviewed request even if the original form model subsequently changes.
    public Task<bool> ConfirmAsync()
    {
        if (_pendingSchedule != null) return SubmitScheduleAsync(_pendingScheduleId, Snapshot(_pendingSchedule), true);
        if (_pendingTimeOff != null) return SubmitTimeOffAsync(Snapshot(_pendingTimeOff), true);
        return Task.FromResult(false);
    }

    private Task<bool> SubmitScheduleAsync(int? id, ScheduleRequestDTO request, bool confirmed)
    {
        request.ConfirmAffectedAppointments = confirmed;
        return SubmitAsync<ScheduleChangeResultDTO>(async () =>
        {
            using var content = JsonContent.Create(request);
            return id.HasValue ? await _api.PutAsync($"/api/admin/doctor-schedules/{id}", content)
                : await _api.PostAsync("/api/admin/doctor-schedules", content);
        }, result =>
        {
            if (!result.RequiresConfirmation) return false;
            _pendingSchedule = request;
            _pendingScheduleId = id;
            _pendingTimeOff = null;
            AffectedAppointments = result.AffectedAppointments;
            return true;
        }, result => result.Schedule != null);
    }

    private Task<bool> SubmitTimeOffAsync(TimeOffRequestDTO request, bool confirmed)
    {
        request.ConfirmAffectedAppointments = confirmed;
        return SubmitAsync<TimeOffChangeResultDTO>(async () =>
        {
            using var content = JsonContent.Create(request);
            return await _api.PostAsync("/api/admin/time-off", content);
        }, result =>
        {
            if (!result.RequiresConfirmation) return false;
            _pendingTimeOff = request;
            _pendingSchedule = null;
            _pendingScheduleId = null;
            AffectedAppointments = result.AffectedAppointments;
            return true;
        }, result => result.TimeOff != null);
    }

    public Task<bool> DeleteTimeOffAsync(int id)
        => SubmitAsync<AdminDoctorTimeOffDTO>(() => _api.DeleteAsync($"/api/admin/time-off/{id}"), _ => false, _ => true);

    private async Task<bool> SubmitAsync<T>(Func<Task<HttpResponseMessage>> send, Func<T, bool> preview, Func<T, bool> valid) where T : class
    {
        if (IsBusy) return false;
        IsSubmitting = true;
        ActionMessage = ActionError = string.Empty;
        try
        {
            using var response = await send();
            var result = await ReadAsync<T>(response);
            if (response.StatusCode == HttpStatusCode.Conflict && result?.Content != null && preview(result.Content))
                return false;
            if (!response.IsSuccessStatusCode || result?.Content == null || !valid(result.Content))
            {
                ActionError = result?.Message ?? HttpError(response.StatusCode);
                return false;
            }
            _pendingSchedule = null;
            _pendingScheduleId = null;
            _pendingTimeOff = null;
            AffectedAppointments = new();
            ActionMessage = result.Message;
            return true;
        }
        catch (Exception ex) when (IsRequestError(ex)) { ActionError = Error(ex); return false; }
        finally { IsSubmitting = false; }
    }

    private async Task<T?> GetAsync<T>(string uri) where T : class
    {
        using var response = await _api.GetAsync(uri);
        var result = await ReadAsync<T>(response);
        if (!response.IsSuccessStatusCode || result?.Content == null)
            throw new ApiResponseException(result?.Message ?? HttpError(response.StatusCode));
        return result.Content;
    }

    private static async Task<HttpResponseData<T>?> ReadAsync<T>(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(text)) return null;
        using var json = JsonDocument.Parse(text);
        if (json.RootElement.TryGetProperty("statusCode", out _))
            return JsonSerializer.Deserialize<HttpResponseData<T>>(text, JsonOptions);
        if (json.RootElement.TryGetProperty("errors", out var errors))
            return new HttpResponseData<T>
            {
                StatusCode = (int)response.StatusCode,
                Message = string.Join(" ", errors.EnumerateObject().SelectMany(p => p.Value.EnumerateArray()).Select(v => v.GetString()))
            };
        return null;
    }

    private static T Snapshot<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, JsonOptions), JsonOptions)!;
    private sealed class ApiResponseException(string message) : Exception(message);
    private static bool IsRequestError(Exception ex) => ex is HttpRequestException or JsonException or TaskCanceledException or ApiResponseException;
    private static string Error(Exception ex) => ex switch
    {
        ApiResponseException => ex.Message,
        JsonException => "Phản hồi hệ thống không hợp lệ. Vui lòng thử lại.",
        TaskCanceledException => "Yêu cầu quá thời gian chờ. Vui lòng thử lại.",
        _ => "Không thể kết nối đến hệ thống. Vui lòng thử lại."
    };
    private static string HttpError(HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized => "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.",
        HttpStatusCode.Forbidden => "Bạn không có quyền quản lý lịch làm việc và nghỉ phép.",
        _ => "Không thể thực hiện yêu cầu. Vui lòng thử lại."
    };
}
