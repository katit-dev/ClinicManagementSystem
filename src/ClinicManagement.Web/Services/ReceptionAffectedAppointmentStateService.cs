using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Appointment;
using ClinicManagementSystem.Application.DTOs.Doctor;
using ClinicManagementSystem.Application.DTOs.Notification;
using ClinicManagementSystem.Application.DTOs.Specialty;

namespace ClinicManagementSystem.Web.Services;

public class ReceptionAffectedAppointmentStateService(AuthorizedApiService api)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private ReceptionAffectedAppointmentDTO? _target;
    public ReceptionAffectedAppointmentDTO? Target => _target;
    public PagedResult<ReceptionAffectedAppointmentDTO> Appointments { get; private set; } = new();
    public List<DoctorDTO> Doctors { get; private set; } = new();
    public List<SlotDTO> Slots { get; private set; } = new();
    public List<NotificationDTO> Notifications { get; private set; } = new();
    public bool IsLoading { get; private set; }
    public bool IsLoadingDoctors { get; private set; }
    public bool IsLoadingSlots { get; private set; }
    public bool IsLoadingNotifications { get; private set; }
    public bool IsSubmitting { get; private set; }
    public bool IsBusy => IsLoading || IsLoadingDoctors || IsLoadingSlots || IsSubmitting;
    public bool NeedsReload { get; private set; }
    public string ListError { get; private set; } = string.Empty;
    public string DoctorsError { get; private set; } = string.Empty;
    public string SlotsError { get; private set; } = string.Empty;
    public string NotificationsError { get; private set; } = string.Empty;
    public string ActionError { get; private set; } = string.Empty;
    public string ActionMessage { get; private set; } = string.Empty;

    public void ClearActions()
    {
        if (IsSubmitting) return;
        ActionError = ActionMessage = string.Empty;
        NeedsReload = false;
    }

    public bool OpenEditor(ReceptionAffectedAppointmentDTO appointment)
    {
        if (IsBusy || !appointment.CanProcess) return false;
        ClearActions();
        // Capture the version the receptionist actually reviewed, independently of list refreshes.
        _target = JsonSerializer.Deserialize<ReceptionAffectedAppointmentDTO>(JsonSerializer.Serialize(appointment, JsonOptions), JsonOptions)!;
        Slots = new();
        SlotsError = string.Empty;
        return true;
    }

    public void CloseEditor()
    {
        if (IsSubmitting || IsLoadingSlots) return;
        _target = null;
        Slots = new();
        SlotsError = string.Empty;
    }

    public async Task LoadAppointmentsAsync(ReceptionAffectedQueryDTO query)
    {
        if (IsBusy) return;
        IsLoading = true;
        ListError = string.Empty;
        var uri = $"/api/reception/appointments/affected?page={query.Page}&pageSize={query.PageSize}";
        if (query.From.HasValue) uri += $"&from={Date(query.From.Value)}";
        if (query.To.HasValue) uri += $"&to={Date(query.To.Value)}";
        if (query.DoctorId.HasValue) uri += $"&doctorId={query.DoctorId.Value}";
        try
        {
            Appointments = await GetAsync<PagedResult<ReceptionAffectedAppointmentDTO>>(uri);
            // Include inactive doctors encountered in the queue and retain their filter label across pages.
            Doctors = Appointments.Items.Select(a => new DoctorDTO { Id = a.DoctorId, FullName = a.DoctorName })
                .Concat(Doctors).DistinctBy(d => d.Id).OrderBy(d => d.FullName).ToList();
        }
        catch (Exception ex) when (IsRequestError(ex)) { Appointments = new(); ListError = Error(ex); }
        finally { IsLoading = false; }
    }

    public async Task LoadDoctorsAsync()
    {
        if (IsBusy) return;
        IsLoadingDoctors = true;
        DoctorsError = string.Empty;
        try
        {
            // Reuse the existing receptionist-accessible endpoints, not Admin-only scheduling APIs.
            var specialties = await GetAsync<List<SpecialtyDTO>>("/api/specialties");
            var doctors = new List<DoctorDTO>();
            foreach (var specialty in specialties)
                doctors.AddRange(await GetAsync<List<DoctorDTO>>($"/api/doctors?specialtyId={specialty.Id}"));
            Doctors = doctors.DistinctBy(d => d.Id).OrderBy(d => d.FullName).ToList();
        }
        catch (Exception ex) when (IsRequestError(ex)) { Doctors = new(); DoctorsError = Error(ex); }
        finally { IsLoadingDoctors = false; }
    }

    public async Task LoadSlotsAsync(DateOnly date)
    {
        if (IsBusy || _target == null || NeedsReload) return;
        var doctorId = _target.DoctorId;
        IsLoadingSlots = true;
        Slots = new();
        SlotsError = string.Empty;
        try
        {
            Slots = (await GetAsync<List<SlotDTO>>($"/api/doctors/{doctorId}/available-slots?date={Date(date)}"))
                .Where(s => s.IsAvailable && s.StartTime != _target.StartTime).OrderBy(s => s.StartTime).ToList();
        }
        catch (Exception ex) when (IsRequestError(ex)) { SlotsError = Error(ex); }
        finally { IsLoadingSlots = false; }
    }

    public async Task LoadNotificationsAsync()
    {
        if (IsLoadingNotifications) return;
        IsLoadingNotifications = true;
        NotificationsError = string.Empty;
        try { Notifications = (await GetAsync<List<NotificationDTO>>("/api/notifications")).OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id).ToList(); }
        catch (Exception ex) when (IsRequestError(ex)) { Notifications = new(); NotificationsError = Error(ex); }
        finally { IsLoadingNotifications = false; }
    }

    public Task<bool> RescheduleAsync(DateTime? start, string reason)
    {
        if (!start.HasValue || !Slots.Any(s => s.IsAvailable && s.StartTime == start))
        {
            ActionError = "Chọn một giờ khám còn trống trong danh sách.";
            return Task.FromResult(false);
        }
        return SubmitAsync("reschedule", new ReceptionRescheduleAppointmentRequestDTO
        {
            ExpectedVersion = _target?.Version ?? string.Empty, Reason = reason.Trim(),
            NewStartTime = DateTime.SpecifyKind(start.Value, DateTimeKind.Unspecified)
        });
    }

    public Task<bool> CancelAsync(string reason) => SubmitAsync("cancel", new ReceptionCancelAppointmentRequestDTO
    { ExpectedVersion = _target?.Version ?? string.Empty, Reason = reason.Trim() });

    private async Task<bool> SubmitAsync(string action, ReceptionCancelAppointmentRequestDTO request)
    {
        if (IsBusy || _target == null || NeedsReload) return false;
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 500)
        { ActionError = "Nhập lý do từ 1 đến 500 ký tự."; return false; }
        var id = _target.Id;
        IsSubmitting = true;
        ActionError = ActionMessage = string.Empty;
        try
        {
            // Serialize the runtime DTO so the reschedule payload retains newStartTime.
            using var content = JsonContent.Create((object)request, request.GetType(), options: JsonOptions);
            using var response = await api.PatchAsync($"/api/reception/appointments/{id}/{action}", content);
            var result = await ReadAsync<ReceptionAffectedAppointmentDTO>(response);
            if (!response.IsSuccessStatusCode || result?.Content == null || result.StatusCode != 200)
            {
                NeedsReload = response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.NotFound;
                ActionError = NonEmpty(result?.Message, HttpError(response.StatusCode));
                return false;
            }
            ActionMessage = NonEmpty(result.Message, action == "cancel" ? "Đã hủy lịch hẹn." : "Đã đổi lịch hẹn.");
            _target = null;
            Slots = new();
            return true;
        }
        catch (Exception ex) when (IsRequestError(ex)) { ActionError = Error(ex); return false; }
        finally { IsSubmitting = false; }
    }

    private async Task<T> GetAsync<T>(string uri) where T : class
    {
        using var response = await api.GetAsync(uri);
        var result = await ReadAsync<T>(response);
        if (!response.IsSuccessStatusCode || result?.Content == null || result.StatusCode is < 200 or >= 300)
            throw new ApiResponseException(NonEmpty(result?.Message, HttpError(response.StatusCode)));
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
            return new() { StatusCode = (int)response.StatusCode, Message = string.Join(" ", errors.EnumerateObject().SelectMany(p => p.Value.EnumerateArray()).Select(v => v.GetString())) };
        return null;
    }

    private static string Date(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private static string NonEmpty(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;
    private sealed class ApiResponseException(string message) : Exception(message);
    private static bool IsRequestError(Exception ex) => ex is HttpRequestException or JsonException or TaskCanceledException or ApiResponseException;
    private static string Error(Exception ex) => ex switch
    {
        ApiResponseException => ex.Message,
        JsonException => "Phản hồi hệ thống không hợp lệ. Vui lòng thử lại.",
        TaskCanceledException => "Yêu cầu quá thời gian chờ. Tải lại danh sách để kiểm tra kết quả trước khi thử lại.",
        _ => "Không thể kết nối đến hệ thống. Tải lại danh sách để kiểm tra kết quả trước khi thử lại."
    };
    private static string HttpError(HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized => "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.",
        HttpStatusCode.Forbidden => "Bạn không có quyền thực hiện thao tác lễ tân này.",
        _ => "Không thể thực hiện yêu cầu. Vui lòng thử lại."
    };
}
