using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.User;
using ClinicManagementSystem.Web.Services;
using Moq;

namespace ClinicManagementSystem.UnitTests.Web;

public class AdminUserStateServiceTests
{
    [Fact]
    public async Task List_EncodesKeywordRoleAndPreservesFiltersAndPagination()
    {
        var handler = new RecordingHandler();
        handler.Responses.Enqueue(Envelope(200, Page(9)));
        var state = CreateState(handler);
        await state.LoadAsync(new() { Keyword = " Lê & Hà ", Role = " Role & test ", Active = false, Page = 2, PageSize = 20 });
        Assert.Contains("keyword=L%C3%AA%20%26%20H%C3%A0", handler.Uris[0]);
        Assert.Contains("role=Role%20%26%20test", handler.Uris[0]);
        Assert.Contains("active=false", handler.Uris[0]);
        Assert.Contains("page=2&pageSize=20", handler.Uris[0]);
        Assert.Null(Assert.Single(state.Result.Items).LastLoginAt);
        Assert.False(state.IsBusy);
    }

    [Fact]
    public async Task Roles_LoadsDatabaseIdsIncludingCustomRoles()
    {
        var handler = new RecordingHandler();
        handler.Responses.Enqueue(Envelope(200, new List<AdminRoleDTO> { new() { Id = 40, Name = "Patient" }, new() { Id = 25, Name = "Cashier" } }));
        var state = CreateState(handler);
        await state.LoadRolesAsync();
        Assert.EndsWith("/api/admin/users/roles", handler.Uris[0]);
        Assert.Equal(new[] { 25, 40 }, state.Roles.Select(r => r.Id));
        Assert.False(state.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SetRoles_SendsCompleteSetOrExplicitEmptyArray(bool removeAll)
    {
        var handler = new RecordingHandler();
        handler.Responses.Enqueue(Envelope(200, User(9), "Đã đổi vai trò."));
        var state = CreateState(handler);
        int[] ids = removeAll ? [] : [40, 25];
        Assert.True(await state.SetRolesAsync(9, new() { RoleIds = ids }));
        Assert.Equal(HttpMethod.Post, handler.Methods[0]);
        Assert.EndsWith("/api/admin/users/9/roles", handler.Uris[0]);
        using var body = JsonDocument.Parse(handler.Bodies[0]);
        Assert.Equal(ids, body.RootElement.GetProperty("roleIds").EnumerateArray().Select(v => v.GetInt32()));
        Assert.Equal("Đã đổi vai trò.", state.ActionMessage);
    }

    [Fact]
    public async Task ResetPassword_SendsMatchingFieldsWithoutPersistingThemInState()
    {
        var handler = new RecordingHandler();
        handler.Responses.Enqueue(Envelope(200, User(9)));
        var state = CreateState(handler);
        // Dummy data used solely by this in-memory HTTP fixture.
        const string fixturePassword = "VC22-fixture-only!";
        Assert.True(await state.ResetPasswordAsync(9, new() { NewPassword = fixturePassword, ConfirmPassword = fixturePassword }));
        Assert.EndsWith("/api/admin/users/9/reset-password", handler.Uris[0]);
        using var body = JsonDocument.Parse(handler.Bodies[0]);
        Assert.Equal(fixturePassword, body.RootElement.GetProperty("newPassword").GetString());
        Assert.Equal(fixturePassword, body.RootElement.GetProperty("confirmPassword").GetString());
        Assert.DoesNotContain(fixturePassword, JsonSerializer.Serialize(state));
        Assert.DoesNotContain("newPassword", JsonSerializer.Serialize(state), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LockAndUnlock_SendPatchWithStatusAndReason(bool active)
    {
        var handler = new RecordingHandler();
        handler.Responses.Enqueue(Envelope(200, User(9)));
        var state = CreateState(handler);
        Assert.True(await state.SetLockAsync(9, new() { IsActive = active, Reason = "Local fixture" }));
        Assert.Equal(HttpMethod.Patch, handler.Methods[0]);
        Assert.EndsWith("/api/admin/users/9/lock", handler.Uris[0]);
        using var body = JsonDocument.Parse(handler.Bodies[0]);
        Assert.Equal(active, body.RootElement.GetProperty("isActive").GetBoolean());
        Assert.Equal("Local fixture", body.RootElement.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task Conflict_DoesNotOptimisticallyChangeRowsAndShowsBackendReason()
    {
        var handler = new RecordingHandler();
        handler.Responses.Enqueue(Envelope(200, Page(9)));
        handler.Responses.Enqueue(Envelope<AdminUserDTO>(409, null, "Tài khoản chưa có hồ sơ bác sĩ."));
        var state = CreateState(handler);
        await state.LoadAsync(new());
        Assert.False(await state.SetRolesAsync(9, new() { RoleIds = [99] }));
        Assert.Equal("Tài khoản chưa có hồ sơ bác sĩ.", state.ActionError);
        Assert.Empty(state.ActionMessage);
        Assert.Equal("Patient", Assert.Single(Assert.Single(state.Result.Items).Roles).Name);
        Assert.False(state.IsBusy);
    }

    [Fact]
    public async Task ValidationProblem_ShowsBothPasswordAndReasonErrors()
    {
        var handler = new RecordingHandler();
        handler.Responses.Enqueue(new(HttpStatusCode.BadRequest) { Content = JsonContent.Create(new
            { errors = new { NewPassword = new[] { "Mật khẩu quá ngắn." }, ConfirmPassword = new[] { "Mật khẩu không khớp." } } }) });
        var state = CreateState(handler);
        Assert.False(await state.ResetPasswordAsync(9, new()));
        Assert.Contains("Mật khẩu quá ngắn.", state.ActionError);
        Assert.Contains("Mật khẩu không khớp.", state.ActionError);
        Assert.False(state.IsBusy);
    }

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    public async Task AuthorizationFailure_FlagsSessionEvenWithNonJsonErrorBody(int status)
    {
        var handler = new RecordingHandler();
        handler.Responses.Enqueue(new((HttpStatusCode)status) { Content = new StringContent("<html>Denied</html>") });
        if (status == 401) handler.Responses.Enqueue(Envelope<object>(401, null, "Revoked refresh token"));
        var state = CreateState(handler);
        await state.LoadAsync(new());
        Assert.Equal(status == 401, state.RequiresLogin);
        Assert.Equal(status == 403, state.AccessDenied);
        Assert.Empty(state.Result.Items);
        Assert.False(state.IsBusy);
        Assert.Contains(status == 401 ? "đăng nhập lại" : "không có quyền", state.ListError);
    }

    [Theory]
    [InlineData("network", "kết nối")]
    [InlineData("timeout", "thời gian")]
    [InlineData("malformed", "không hợp lệ")]
    public async Task RoleLookupFailure_ClearsStaleRolesAndCanRetry(string failure, string message)
    {
        var handler = new RecordingHandler();
        handler.Responses.Enqueue(Envelope(200, new List<AdminRoleDTO> { new() { Id = 40, Name = "Patient" } }));
        var state = CreateState(handler);
        await state.LoadRolesAsync();
        if (failure == "network") handler.Failure = new HttpRequestException("fixture detail");
        else if (failure == "timeout") handler.Failure = new TaskCanceledException("fixture detail");
        else handler.Responses.Enqueue(new(HttpStatusCode.OK) { Content = new StringContent("<html>Error</html>") });
        await state.LoadRolesAsync();
        Assert.Empty(state.Roles);
        Assert.Contains(message, state.RolesError);
        Assert.DoesNotContain("fixture detail", state.RolesError);
        Assert.False(state.IsBusy);
        handler.Failure = null;
        handler.Responses.Enqueue(Envelope(200, new List<AdminRoleDTO> { new() { Id = 40, Name = "Patient" } }));
        await state.LoadRolesAsync();
        Assert.Empty(state.RolesError);
        Assert.Single(state.Roles);
    }

    [Fact]
    public async Task DetailResponseForDifferentAccount_IsRejected()
    {
        var handler = new RecordingHandler();
        handler.Responses.Enqueue(Envelope(200, User(10)));
        var state = CreateState(handler);
        Assert.Null(await state.LoadDetailAsync(9));
        Assert.Contains("không hợp lệ", state.ActionError);
        Assert.False(state.IsBusy);
    }

    [Fact]
    public async Task DoubleSubmit_BlocksCompetingMutationsAndLoads()
    {
        var handler = new RecordingHandler { Hold = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        handler.Responses.Enqueue(Envelope(200, User(9)));
        var state = CreateState(handler);
        var first = state.SetLockAsync(9, new() { IsActive = false, Reason = "fixture" });
        Assert.True(state.IsBusy);
        Assert.False(await state.SetRolesAsync(9, new() { RoleIds = [] }));
        Assert.False(await state.ResetPasswordAsync(9, new()));
        await state.LoadRolesAsync();
        await state.LoadAsync(new());
        Assert.Null(await state.LoadDetailAsync(9));
        handler.Hold.SetResult(true);
        Assert.True(await first);
        Assert.Single(handler.Uris);
        Assert.False(state.IsBusy);
    }

    [Fact]
    public async Task FailedReload_ClearsRowsAndPreservesSuccessfulMutationMessage()
    {
        var handler = new RecordingHandler();
        handler.Responses.Enqueue(Envelope(200, Page(9)));
        handler.Responses.Enqueue(Envelope(200, User(9)));
        handler.Responses.Enqueue(new(HttpStatusCode.InternalServerError));
        var state = CreateState(handler);
        await state.LoadAsync(new());
        Assert.True(await state.SetLockAsync(9, new() { IsActive = false, Reason = "fixture" }));
        await state.LoadAsync(new());
        Assert.Empty(state.Result.Items);
        Assert.NotEmpty(state.ListError);
        Assert.Equal("Thành công", state.ActionMessage);
    }

    [Fact]
    public async Task HttpSuccessWithErrorEnvelope_IsNotACompletedMutation()
    {
        var handler = new RecordingHandler();
        handler.Responses.Enqueue(new(HttpStatusCode.OK) { Content = JsonContent.Create(new HttpResponseData<AdminUserDTO>
            { StatusCode = 409, Message = "Conflict", Content = User(9) }) });
        var state = CreateState(handler);
        Assert.False(await state.SetLockAsync(9, new() { IsActive = false, Reason = "fixture" }));
        Assert.Equal("Conflict", state.ActionError);
        Assert.Empty(state.ActionMessage);
    }

    private static AdminUserDTO User(int id) => new() { Id = id, Username = "fixture", IsActive = true, Roles = [new() { Id = 40, Name = "Patient" }] };
    private static PagedResult<AdminUserDTO> Page(int id) => new() { Items = [User(id)], Page = 1, PageSize = 10, TotalItems = 1, TotalPages = 1 };
    private static HttpResponseMessage Envelope<T>(int status, T? content, string message = "Thành công") => new((HttpStatusCode)status)
        { Content = JsonContent.Create(new HttpResponseData<T> { StatusCode = status, Content = content, Message = message }) };

    private static AdminUserStateService CreateState(RecordingHandler handler)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(handler, false) { BaseAddress = new("http://fixture.local") });
        var user = new UserStateService(null!, factory.Object, null!, new CustomAuthenticationStateProvider());
        typeof(UserStateService).GetProperty(nameof(UserStateService.AccessToken))!.SetValue(user, "fixture-token");
        typeof(UserStateService).GetProperty(nameof(UserStateService.RefreshToken))!.SetValue(user, "fixture-refresh");
        return new(new AuthorizedApiService(factory.Object, user));
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public Queue<HttpResponseMessage> Responses { get; } = new();
        public List<string> Uris { get; } = new();
        public List<HttpMethod> Methods { get; } = new();
        public List<string> Bodies { get; } = new();
        public Exception? Failure { get; set; }
        public TaskCompletionSource<bool>? Hold { get; set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Uris.Add(request.RequestUri!.AbsoluteUri);
            Methods.Add(request.Method);
            Bodies.Add(request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));
            if (Hold != null) await Hold.Task;
            if (Failure != null) throw Failure;
            return Responses.Dequeue();
        }
    }
}
