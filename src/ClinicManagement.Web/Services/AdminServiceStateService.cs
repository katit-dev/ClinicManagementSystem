using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Service;
using ClinicManagementSystem.Application.DTOs.Specialty;

namespace ClinicManagementSystem.Web.Services;

public class AdminServiceStateService(AuthorizedApiService api)
{
    public PagedResult<AdminServiceDTO> Result { get; private set; } = new();
    public List<AdminSpecialtyDTO> Specialties { get; private set; } = new();
    public bool IsLoading { get; private set; }
    public bool IsLoadingSpecialties { get; private set; }
    public bool IsLoadingDetail { get; private set; }
    public bool IsSubmitting { get; private set; }
    public bool IsBusy => IsLoading || IsLoadingSpecialties || IsLoadingDetail || IsSubmitting;
    public string ListError { get; private set; } = string.Empty;
    public string SpecialtiesError { get; private set; } = string.Empty;
    public string ActionMessage { get; private set; } = string.Empty;
    public string ActionError { get; private set; } = string.Empty;

    public void ClearActions()
    {
        ActionMessage = string.Empty;
        ActionError = string.Empty;
    }

    public async Task LoadAsync(AdminServiceQueryDTO query)
    {
        if (IsBusy) return;
        IsLoading = true;
        ListError = string.Empty;
        try
        {
            var uri = $"/api/admin/services?page={query.Page}&pageSize={query.PageSize}";
            if (!string.IsNullOrWhiteSpace(query.Keyword)) uri += $"&keyword={Uri.EscapeDataString(query.Keyword.Trim())}";
            if (query.Active.HasValue) uri += $"&active={query.Active.Value.ToString().ToLowerInvariant()}";
            if (query.SpecialtyId.HasValue) uri += $"&specialtyId={query.SpecialtyId.Value}";
            using var response = await api.GetAsync(uri);
            var result = await ReadAsync<PagedResult<AdminServiceDTO>>(response);
            if (!Succeeded(response, result))
            {
                Result = new();
                ListError = Error(response, result);
                return;
            }
            Result = result!.Content!;
        }
        catch (Exception ex) when (IsExpectedFailure(ex))
        {
            Result = new();
            ListError = FailureMessage(ex);
        }
        finally { IsLoading = false; }
    }

    public async Task LoadSpecialtiesAsync()
    {
        if (IsBusy) return;
        IsLoadingSpecialties = true;
        SpecialtiesError = string.Empty;
        try
        {
            // Load every page, including inactive specialties used by historical services.
            var items = new List<AdminSpecialtyDTO>();
            var page = 1;
            while (true)
            {
                using var response = await api.GetAsync($"/api/admin/specialties?page={page}&pageSize=100");
                var result = await ReadAsync<PagedResult<AdminSpecialtyDTO>>(response);
                if (!Succeeded(response, result))
                {
                    Specialties = new();
                    SpecialtiesError = Error(response, result);
                    return;
                }
                var content = result!.Content!;
                items.AddRange(content.Items);
                if (page >= content.TotalPages) break;
                if (content.Items.Count == 0) throw new JsonException("Incomplete specialty page.");
                page++;
            }
            Specialties = items.DistinctBy(s => s.Id).OrderBy(s => s.Name).ToList();
        }
        catch (Exception ex) when (IsExpectedFailure(ex))
        {
            Specialties = new();
            SpecialtiesError = FailureMessage(ex);
        }
        finally { IsLoadingSpecialties = false; }
    }

    public async Task<AdminServiceDTO?> LoadDetailAsync(int id)
    {
        if (IsBusy) return null;
        IsLoadingDetail = true;
        ClearActions();
        try
        {
            using var response = await api.GetAsync($"/api/admin/services/{id}");
            var result = await ReadAsync<AdminServiceDTO>(response);
            if (!Succeeded(response, result))
            {
                ActionError = Error(response, result);
                return null;
            }
            if (result!.Content!.Id != id) throw new JsonException("Unexpected service id.");
            return result.Content;
        }
        catch (Exception ex) when (IsExpectedFailure(ex))
        {
            ActionError = FailureMessage(ex);
            return null;
        }
        finally { IsLoadingDetail = false; }
    }

    public Task<bool> SaveAsync(int? id, SaveServiceRequestDTO request)
        => SubmitAsync(() => id.HasValue
            ? api.PutAsync($"/api/admin/services/{id.Value}", JsonContent.Create(request))
            : api.PostAsync("/api/admin/services", JsonContent.Create(request)));

    public Task<bool> DeactivateAsync(int id)
        => SubmitAsync(() => api.PatchAsync($"/api/admin/services/{id}/deactivate", new StringContent(string.Empty)));

    private async Task<bool> SubmitAsync(Func<Task<HttpResponseMessage>> send)
    {
        if (IsBusy) return false;
        IsSubmitting = true;
        ClearActions();
        try
        {
            using var response = await send();
            var result = await ReadAsync<AdminServiceDTO>(response);
            if (!Succeeded(response, result))
            {
                ActionError = Error(response, result);
                return false;
            }
            ActionMessage = string.IsNullOrWhiteSpace(result!.Message) ? "Đã lưu thay đổi dịch vụ." : result.Message;
            return true;
        }
        catch (Exception ex) when (IsExpectedFailure(ex))
        {
            ActionError = FailureMessage(ex);
            return false;
        }
        finally { IsSubmitting = false; }
    }

    private static bool Succeeded<T>(HttpResponseMessage response, HttpResponseData<T>? result)
        => response.IsSuccessStatusCode && result is { StatusCode: >= 200 and < 300, Content: not null };

    private static string Error<T>(HttpResponseMessage response, HttpResponseData<T>? result)
        => !string.IsNullOrWhiteSpace(result?.Message) ? result.Message : response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.",
            HttpStatusCode.Forbidden => "Bạn không có quyền quản lý danh mục dịch vụ.",
            HttpStatusCode.NotFound => "Không tìm thấy dịch vụ. Vui lòng tải lại danh sách.",
            _ => "Không thể thực hiện yêu cầu. Vui lòng thử lại."
        };

    private static bool IsExpectedFailure(Exception ex) => ex is HttpRequestException or JsonException or TaskCanceledException;
    private static string FailureMessage(Exception ex) => ex switch
    {
        HttpRequestException => "Không thể kết nối đến hệ thống. Vui lòng thử lại.",
        TaskCanceledException => "Yêu cầu quá thời gian chờ. Vui lòng thử lại.",
        _ => "Phản hồi hệ thống không hợp lệ. Vui lòng thử lại."
    };

    private static async Task<HttpResponseData<T>?> ReadAsync<T>(HttpResponseMessage response)
    {
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
