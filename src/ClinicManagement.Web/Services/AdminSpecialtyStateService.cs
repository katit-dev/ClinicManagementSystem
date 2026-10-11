using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Specialty;

namespace ClinicManagementSystem.Web.Services;

public class AdminSpecialtyStateService
{
    private readonly AuthorizedApiService _api;
    public AdminSpecialtyStateService(AuthorizedApiService api) => _api = api;

    public PagedResult<AdminSpecialtyDTO> Result { get; private set; } = new();
    public bool IsLoading { get; private set; }
    public bool IsSubmitting { get; private set; }
    public string ErrorMessage { get; private set; } = string.Empty;
    public string ActionMessage { get; private set; } = string.Empty;
    public string ActionError { get; private set; } = string.Empty;

    public void ClearActionMessages()
    {
        ActionMessage = string.Empty;
        ActionError = string.Empty;
    }

    public async Task LoadAsync(string? keyword, bool? active, int page = 1, int pageSize = 10)
    {
        if (IsLoading) return;
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var uri = $"/api/admin/specialties?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(keyword))
                uri += $"&keyword={Uri.EscapeDataString(keyword.Trim())}";
            if (active.HasValue)
                uri += $"&active={active.Value.ToString().ToLowerInvariant()}";

            using var response = await _api.GetAsync(uri);
            var result = await ReadAsync<PagedResult<AdminSpecialtyDTO>>(response);
            if (!response.IsSuccessStatusCode || result?.Content == null)
            {
                Result = new();
                ErrorMessage = result?.Message ?? GetHttpError(response.StatusCode);
                return;
            }
            Result = result.Content;
        }
        catch (HttpRequestException)
        {
            Result = new();
            ErrorMessage = "Không thể kết nối đến hệ thống. Vui lòng thử lại.";
        }
        catch (JsonException)
        {
            Result = new();
            ErrorMessage = "Phản hồi hệ thống không hợp lệ. Vui lòng thử lại.";
        }
        catch (TaskCanceledException)
        {
            Result = new();
            ErrorMessage = "Yêu cầu quá thời gian chờ. Vui lòng thử lại.";
        }
        finally { IsLoading = false; }
    }

    public Task<bool> SaveAsync(int? id, SaveSpecialtyRequestDTO request)
        => SubmitAsync(() => id.HasValue
            ? _api.PutAsync($"/api/admin/specialties/{id}", JsonContent.Create(request))
            : _api.PostAsync("/api/admin/specialties", JsonContent.Create(request)));

    public Task<bool> SetStatusAsync(int id, bool isActive)
        => SubmitAsync(() => _api.PatchAsync($"/api/admin/specialties/{id}/status",
            JsonContent.Create(new SetSpecialtyStatusRequestDTO { IsActive = isActive })));

    private async Task<bool> SubmitAsync(Func<Task<HttpResponseMessage>> send)
    {
        if (IsSubmitting) return false;
        IsSubmitting = true;
        ClearActionMessages();
        try
        {
            using var response = await send();
            var result = await ReadAsync<AdminSpecialtyDTO>(response);
            if (!response.IsSuccessStatusCode || result?.Content == null)
            {
                ActionError = result?.Message ?? GetHttpError(response.StatusCode);
                return false;
            }
            ActionMessage = result.Message;
            return true;
        }
        catch (HttpRequestException)
        {
            ActionError = "Không thể kết nối đến hệ thống. Vui lòng thử lại.";
            return false;
        }
        catch (JsonException)
        {
            ActionError = "Phản hồi hệ thống không hợp lệ. Vui lòng thử lại.";
            return false;
        }
        catch (TaskCanceledException)
        {
            ActionError = "Yêu cầu quá thời gian chờ. Vui lòng thử lại.";
            return false;
        }
        finally { IsSubmitting = false; }
    }

    private static async Task<HttpResponseData<T>?> ReadAsync<T>(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(text)) return null;
        using var json = JsonDocument.Parse(text);
        if (json.RootElement.TryGetProperty("statusCode", out _))
            return JsonSerializer.Deserialize<HttpResponseData<T>>(text, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        // [ApiController] returns ValidationProblemDetails for invalid DTOs.
        if (json.RootElement.TryGetProperty("errors", out var errors))
        {
            var messages = errors.EnumerateObject()
                .SelectMany(property => property.Value.EnumerateArray())
                .Select(value => value.GetString())
                .Where(value => !string.IsNullOrWhiteSpace(value));
            return new HttpResponseData<T> { StatusCode = (int)response.StatusCode, Message = string.Join(" ", messages) };
        }
        return null;
    }

    private static string GetHttpError(HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized => "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.",
        HttpStatusCode.Forbidden => "Bạn không có quyền quản trị chuyên khoa.",
        _ => "Không thể thực hiện yêu cầu. Vui lòng thử lại."
    };
}
