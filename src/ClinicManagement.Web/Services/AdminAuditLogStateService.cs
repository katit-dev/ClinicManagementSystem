using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;
using System.Text.Json;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Audit;

namespace ClinicManagementSystem.Web.Services;

public class AdminAuditLogStateService(AuthorizedApiService api)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private AuditLogQueryDTO? _appliedQuery;
    public PagedResult<AuditAccessDTO> Result { get; private set; } = new();
    public PagedResult<AuditUserOptionDTO> Users { get; private set; } = new();
    public AuditAccessDetailDTO? Detail { get; private set; }
    public bool HasLoaded { get; private set; }
    public AuditLogQueryDTO? AppliedFilter => _appliedQuery == null ? null : Clone(_appliedQuery, _appliedQuery.Page);
    public bool IsLoading { get; private set; }
    public bool IsLoadingUsers { get; private set; }
    public bool IsLoadingDetail { get; private set; }
    public bool IsBusy => IsLoading || IsLoadingUsers || IsLoadingDetail;
    public bool RequiresLogin { get; private set; }
    public bool AccessDenied { get; private set; }
    public string ListError { get; private set; } = string.Empty;
    public string UsersError { get; private set; } = string.Empty;
    public string DetailError { get; private set; } = string.Empty;
    public string UserKeyword { get; private set; } = string.Empty;

    public void Reset()
    {
        if (IsBusy) return;
        ClearSensitive(); Users = new(); UserKeyword = string.Empty;
        RequiresLogin = AccessDenied = false;
        ListError = UsersError = DetailError = string.Empty;
    }

    public bool MatchesAppliedFilter(AuditLogQueryDTO query) => _appliedQuery != null
        && Normalize(query.Entity) == Normalize(_appliedQuery.Entity) && query.UserId == _appliedQuery.UserId
        && query.EntityId == _appliedQuery.EntityId && query.From == _appliedQuery.From && query.To == _appliedQuery.To
        && query.PageSize == _appliedQuery.PageSize;

    public Task LoadAsync(AuditLogQueryDTO query) => LoadListAsync(Clone(query, 1));

    public Task GoToPageAsync(int page)
    {
        if (_appliedQuery == null || page < 1 || page > Result.TotalPages || IsBusy || RequiresLogin || AccessDenied)
            return Task.CompletedTask;
        return LoadListAsync(Clone(_appliedQuery, page));
    }

    private async Task LoadListAsync(AuditLogQueryDTO query)
    {
        if (IsBusy || RequiresLogin || AccessDenied) return;
        ClearSensitive(); ListError = DetailError = string.Empty;
        var validation = new List<ValidationResult>();
        if (!Validator.TryValidateObject(query, new ValidationContext(query), validation, true))
        {
            ListError = string.Join(" ", validation.Select(v => v.ErrorMessage));
            return;
        }
        IsLoading = true;
        try
        {
            using var response = await api.GetAsync(QueryUri(query));
            var envelope = await ReadAsync<PagedResult<AuditAccessDTO>>(response);
            if (!Succeeded(response, envelope)) { ListError = Error(response, envelope); return; }
            var result = envelope!.Content!;
            ValidatePage(result, query.Page, query.PageSize);
            if (result.Items.Any(item => item.Id <= 0 || string.IsNullOrWhiteSpace(item.Action) || string.IsNullOrWhiteSpace(item.EntityName)
                || item.EntryCount < 1 || item.EntityCount < 0 || item.EntityCount > item.EntryCount))
                throw new JsonException("Unexpected audit record.");
            Result = result; _appliedQuery = query; HasLoaded = true; MarkAuthorized();
        }
        catch (Exception ex) when (IsRequestFailure(ex)) { ListError = FailureMessage(ex); }
        finally { IsLoading = false; }
    }

    public async Task LoadUsersAsync(string? keyword = null, int page = 1)
    {
        if (IsBusy || RequiresLogin || AccessDenied) return;
        Users = new(); UsersError = string.Empty;
        var search = keyword?.Trim() ?? string.Empty;
        if (page < 1 || search.Length > 100) { UsersError = "Từ khóa tối đa 100 ký tự; trang phải lớn hơn 0."; return; }
        IsLoadingUsers = true;
        try
        {
            var uri = "/api/admin/audit-logs/users?page=" + Number(page) + "&pageSize=50";
            if (search.Length > 0) uri += "&keyword=" + Uri.EscapeDataString(search);
            using var response = await api.GetAsync(uri);
            var envelope = await ReadAsync<PagedResult<AuditUserOptionDTO>>(response);
            if (!Succeeded(response, envelope)) { UsersError = Error(response, envelope); return; }
            var users = envelope!.Content!;
            ValidatePage(users, page, 50);
            if (users.Items.Any(item => item.Id <= 0)) throw new JsonException("Unexpected actor id.");
            Users = users; UserKeyword = search; MarkAuthorized();
        }
        catch (Exception ex) when (IsRequestFailure(ex)) { UsersError = FailureMessage(ex); }
        finally { IsLoadingUsers = false; }
    }

    public async Task LoadDetailAsync(int id)
    {
        if (IsBusy || RequiresLogin || AccessDenied) return;
        Detail = null; DetailError = string.Empty;
        if (id < 1) { DetailError = "Mã nhật ký không hợp lệ."; return; }
        IsLoadingDetail = true;
        try
        {
            using var response = await api.GetAsync("/api/admin/audit-logs/accesses/" + Number(id));
            var envelope = await ReadAsync<AuditAccessDetailDTO>(response);
            if (!Succeeded(response, envelope)) { DetailError = Error(response, envelope); return; }
            if (envelope!.Content!.Id != id) throw new JsonException("Unexpected detail id.");
            Detail = envelope.Content; MarkAuthorized();
        }
        catch (Exception ex) when (IsRequestFailure(ex)) { DetailError = FailureMessage(ex); }
        finally { IsLoadingDetail = false; }
    }

    public void CloseDetail() { if (IsBusy) return; Detail = null; DetailError = string.Empty; }

    private void ClearSensitive() { Result = new(); _appliedQuery = null; HasLoaded = false; Detail = null; }
    private void MarkAuthorized() => RequiresLogin = AccessDenied = false;
    private string Error<T>(HttpResponseMessage response, HttpResponseData<T>? result)
    {
        RequiresLogin = response.StatusCode == HttpStatusCode.Unauthorized || result?.StatusCode == 401;
        AccessDenied = response.StatusCode == HttpStatusCode.Forbidden || result?.StatusCode == 403;
        if (RequiresLogin || AccessDenied) { ClearSensitive(); Users = new(); }
        if (RequiresLogin) return "Phiên đăng nhập không còn hợp lệ. Vui lòng đăng nhập lại.";
        if (AccessDenied) return "Bạn không có quyền xem nhật ký truy cập.";
        return !string.IsNullOrWhiteSpace(result?.Message) ? result.Message : response.StatusCode switch
        {
            HttpStatusCode.NotFound => "Không tìm thấy nhật ký. Vui lòng tải lại danh sách.",
            _ => "Không thể tải nhật ký truy cập. Vui lòng thử lại."
        };
    }

    private static AuditLogQueryDTO Clone(AuditLogQueryDTO query, int page) => new()
    {
        Entity = Normalize(query.Entity), UserId = query.UserId, EntityId = query.EntityId,
        From = query.From, To = query.To, Page = page, PageSize = query.PageSize
    };
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    private static string QueryUri(AuditLogQueryDTO query)
    {
        var uri = "/api/admin/audit-logs/accesses?page=" + Number(query.Page) + "&pageSize=" + Number(query.PageSize);
        if (!string.IsNullOrWhiteSpace(query.Entity)) uri += "&entity=" + Uri.EscapeDataString(query.Entity);
        if (query.UserId.HasValue) uri += "&userId=" + Number(query.UserId.Value);
        if (query.EntityId.HasValue) uri += "&entityId=" + Number(query.EntityId.Value);
        if (query.From.HasValue) uri += "&from=" + query.From.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (query.To.HasValue) uri += "&to=" + query.To.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return uri;
    }
    private static void ValidatePage<T>(PagedResult<T> result, int page, int pageSize)
    {
        if (result.Items == null || result.Page != page || result.PageSize != pageSize || result.TotalItems < 0
            || result.TotalPages != (int)Math.Ceiling(result.TotalItems / (double)pageSize) || result.Items.Count > pageSize)
            throw new JsonException("Unexpected pagination metadata.");
    }
    private static bool Succeeded<T>(HttpResponseMessage response, HttpResponseData<T>? envelope)
        => response.IsSuccessStatusCode && envelope is { StatusCode: >= 200 and < 300, Content: not null };
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
