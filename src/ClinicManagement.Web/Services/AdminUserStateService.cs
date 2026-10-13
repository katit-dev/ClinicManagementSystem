using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.User;

namespace ClinicManagementSystem.Web.Services;

public class AdminUserStateService(AuthorizedApiService api)
{
    public PagedResult<AdminUserDTO> Result { get; private set; } = new();
    public List<AdminRoleDTO> Roles { get; private set; } = new();
    public bool IsLoading { get; private set; }
    public bool IsLoadingRoles { get; private set; }
    public bool IsLoadingDetail { get; private set; }
    public bool IsSubmitting { get; private set; }
    public bool IsBusy => IsLoading || IsLoadingRoles || IsLoadingDetail || IsSubmitting;
    public bool RequiresLogin { get; private set; }
    public bool AccessDenied { get; private set; }
    public string ListError { get; private set; } = string.Empty;
    public string RolesError { get; private set; } = string.Empty;
    public string ActionMessage { get; private set; } = string.Empty;
    public string ActionError { get; private set; } = string.Empty;

    public void ClearActions()
    {
        ActionMessage = string.Empty;
        ActionError = string.Empty;
    }

    public async Task LoadAsync(AdminUserQueryDTO query)
    {
        if (IsBusy) return;
        IsLoading = true;
        ListError = string.Empty;
        try
        {
            var uri = $"/api/admin/users?page={query.Page}&pageSize={query.PageSize}";
            if (!string.IsNullOrWhiteSpace(query.Keyword))
                uri += $"&keyword={Uri.EscapeDataString(query.Keyword.Trim())}";
            if (!string.IsNullOrWhiteSpace(query.Role))
                uri += $"&role={Uri.EscapeDataString(query.Role.Trim())}";
            if (query.Active.HasValue)
                uri += $"&active={query.Active.Value.ToString().ToLowerInvariant()}";

            using var response = await api.GetAsync(uri);
            var result = await ReadAsync<PagedResult<AdminUserDTO>>(response);
            if (!Succeeded(response, result))
            {
                Result = new();
                ListError = Error(response, result);
                return;
            }
            Result = result!.Content!;
            MarkAuthorized();
        }
        catch (Exception ex) when (IsExpectedFailure(ex))
        {
            Result = new();
            ListError = FailureMessage(ex);
        }
        finally { IsLoading = false; }
    }

    public async Task LoadRolesAsync()
    {
        if (IsBusy) return;
        IsLoadingRoles = true;
        RolesError = string.Empty;
        try
        {
            using var response = await api.GetAsync("/api/admin/users/roles");
            var result = await ReadAsync<List<AdminRoleDTO>>(response);
            if (!Succeeded(response, result))
            {
                Roles = new();
                RolesError = Error(response, result);
                return;
            }
            Roles = result!.Content!.OrderBy(r => r.Name).ToList();
            MarkAuthorized();
        }
        catch (Exception ex) when (IsExpectedFailure(ex))
        {
            Roles = new();
            RolesError = FailureMessage(ex);
        }
        finally { IsLoadingRoles = false; }
    }

    public async Task<AdminUserDTO?> LoadDetailAsync(int id)
    {
        if (IsBusy) return null;
        IsLoadingDetail = true;
        ClearActions();
        try
        {
            using var response = await api.GetAsync($"/api/admin/users/{id}");
            var result = await ReadAsync<AdminUserDTO>(response);
            if (!Succeeded(response, result))
            {
                ActionError = Error(response, result);
                return null;
            }
            if (result!.Content!.Id != id) throw new JsonException("Unexpected user id.");
            MarkAuthorized();
            return result.Content;
        }
        catch (Exception ex) when (IsExpectedFailure(ex))
        {
            ActionError = FailureMessage(ex);
            return null;
        }
        finally { IsLoadingDetail = false; }
    }

    public async Task<bool> SetRolesAsync(int id, SetUserRolesRequestDTO request)
    {
        using var content = JsonContent.Create(request);
        return await SubmitAsync(id, () => api.PostAsync($"/api/admin/users/{id}/roles", content));
    }

    public async Task<bool> ResetPasswordAsync(int id, AdminResetPasswordRequestDTO request)
    {
        // Password fields remain in the page form/request only, never in this scoped state.
        using var content = JsonContent.Create(request);
        return await SubmitAsync(id, () => api.PostAsync($"/api/admin/users/{id}/reset-password", content));
    }

    public async Task<bool> SetLockAsync(int id, SetUserLockRequestDTO request)
    {
        using var content = JsonContent.Create(request);
        return await SubmitAsync(id, () => api.PatchAsync($"/api/admin/users/{id}/lock", content));
    }

    private async Task<bool> SubmitAsync(int id, Func<Task<HttpResponseMessage>> send)
    {
        if (IsBusy) return false;
        IsSubmitting = true;
        ClearActions();
        try
        {
            using var response = await send();
            var result = await ReadAsync<AdminUserDTO>(response);
            if (!Succeeded(response, result))
            {
                ActionError = Error(response, result);
                return false;
            }
            if (result!.Content!.Id != id) throw new JsonException("Unexpected user id.");
            ActionMessage = string.IsNullOrWhiteSpace(result.Message) ? "Đã cập nhật tài khoản." : result.Message;
            MarkAuthorized();
            return true;
        }
        catch (Exception ex) when (IsExpectedFailure(ex))
        {
            ActionError = FailureMessage(ex);
            return false;
        }
        finally { IsSubmitting = false; }
    }

    private void MarkAuthorized()
    {
        RequiresLogin = false;
        AccessDenied = false;
    }

    private static bool Succeeded<T>(HttpResponseMessage response, HttpResponseData<T>? result)
        => response.IsSuccessStatusCode && result is { StatusCode: >= 200 and < 300, Content: not null };

    private string Error<T>(HttpResponseMessage response, HttpResponseData<T>? result)
    {
        // Prefer transport authorization status; avoid continuing with a stale Admin session.
        RequiresLogin = response.StatusCode == HttpStatusCode.Unauthorized || result?.StatusCode == 401;
        AccessDenied = response.StatusCode == HttpStatusCode.Forbidden || result?.StatusCode == 403;
        if (RequiresLogin) return "Phiên đăng nhập không còn hợp lệ. Vui lòng đăng nhập lại.";
        if (AccessDenied) return "Bạn không có quyền quản lý người dùng.";
        return !string.IsNullOrWhiteSpace(result?.Message) ? result.Message : response.StatusCode switch
        {
            HttpStatusCode.NotFound => "Không tìm thấy tài khoản. Vui lòng tải lại danh sách.",
            HttpStatusCode.Conflict => "Thông tin tài khoản đã thay đổi. Vui lòng tải lại và kiểm tra.",
            _ => "Không thể thực hiện yêu cầu. Vui lòng thử lại."
        };
    }

    private static bool IsExpectedFailure(Exception ex) => ex is HttpRequestException or JsonException or TaskCanceledException;
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
        if (json.RootElement.TryGetProperty("statusCode", out _))
            return JsonSerializer.Deserialize<HttpResponseData<T>>(text, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if (json.RootElement.TryGetProperty("errors", out var errors))
        {
            var messages = errors.EnumerateObject().SelectMany(p => p.Value.EnumerateArray())
                .Select(v => v.GetString()).Where(v => !string.IsNullOrWhiteSpace(v));
            return new() { StatusCode = (int)response.StatusCode, Message = string.Join(" ", messages) };
        }
        return null;
    }
}
