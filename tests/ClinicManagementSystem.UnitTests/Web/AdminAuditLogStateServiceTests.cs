using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Audit;
using ClinicManagementSystem.Web.Services;
using Moq;

namespace ClinicManagementSystem.UnitTests.Web;

public class AdminAuditLogStateServiceTests
{
    private static AuditLogQueryDTO Query() => new() { Entity = "medical_records", UserId = 7, EntityId = 1204, From = new(2026, 10, 1), To = new(2026, 10, 13), PageSize = 20 };
    private static AuditAccessDTO Log(int id = 9) => new() { Id = id, UserId = 7, Username = "bacsi.a", Action = "VIEW_RECORD", EntityName = "medical_records", EntityId = 1204,
        IpAddress = "10.0.0.15", Succeeded = true, IsUtcTimestamp = true, OccurredAtUtc = new(2026, 10, 13, 2, 12, 0) };
    private static PagedResult<AuditAccessDTO> Logs(int page = 1, int total = 21) => new() { Page = page, PageSize = 20, TotalItems = total, TotalPages = (int)Math.Ceiling(total / 20d), Items = total == 0 ? [] : [Log()] };

    [Fact]
    public async Task Load_UsesInvariantDatesAndAuthorizedHeader_ResetsNewSearchToFirstPage()
    {
        var h = new Handler(); h.Responses.Enqueue(Response(Logs())); var state = State(h); var query = Query(); query.Page = 9;
        var previous = CultureInfo.CurrentCulture;
        try { CultureInfo.CurrentCulture = new("vi-VN"); await state.LoadAsync(query); }
        finally { CultureInfo.CurrentCulture = previous; }
        Assert.Equal("http://fixture.local/api/admin/audit-logs/accesses?page=1&pageSize=20&entity=medical_records&userId=7&entityId=1204&from=2026-10-01&to=2026-10-13", Assert.Single(h.Uris));
        Assert.Equal("Bearer fixture-token", Assert.Single(h.Authorization)); Assert.True(state.HasLoaded); Assert.True(state.MatchesAppliedFilter(Query()));
        Assert.Empty(state.ListError); Assert.False(state.IsBusy);
    }

    [Fact]
    public async Task OptionalFilters_AreOmitted_EntityQueryIsEncoded()
    {
        var h = new Handler(); h.Responses.Enqueue(Response(Logs())); h.Responses.Enqueue(Response(Logs())); var state = State(h);
        await state.LoadAsync(new()); Assert.EndsWith("/api/admin/audit-logs/accesses?page=1&pageSize=20", h.Uris[0]);
        await state.LoadAsync(new() { Entity = " patients&userId=99 " });
        Assert.Contains("entity=patients%26userId%3D99", h.Uris[1]); Assert.DoesNotContain("&userId=99", h.Uris[1]);
    }

    [Theory]
    [InlineData("from-only")]
    [InlineData("to-only")]
    [InlineData("reversed")]
    [InlineData("too-long")]
    [InlineData("user-zero")]
    [InlineData("entity-zero")]
    [InlineData("page-size")]
    [InlineData("entity-too-long")]
    public async Task InvalidFilter_IsRejectedWithoutHttp(string kind)
    {
        var query = Query();
        switch (kind)
        {
            case "from-only": query.To = null; break;
            case "to-only": query.From = null; break;
            case "reversed": query.To = query.From!.Value.AddDays(-1); break;
            case "too-long": query.From = new(2026, 1, 1); query.To = new(2027, 1, 2); break;
            case "user-zero": query.UserId = 0; break;
            case "entity-zero": query.EntityId = 0; break;
            case "page-size": query.PageSize = 101; break;
            case "entity-too-long": query.Entity = new string('x', 129); break;
        }
        var h = new Handler(); var state = State(h); await state.LoadAsync(query);
        Assert.Empty(h.Uris); Assert.NotEmpty(state.ListError); Assert.False(state.HasLoaded); Assert.False(state.IsBusy);
    }

    [Fact]
    public async Task Paging_UsesSuccessfulFilterSnapshotDespiteFormEdits_AndRejectsOutOfRangePages()
    {
        var h = new Handler(); h.Responses.Enqueue(Response(Logs())); h.Responses.Enqueue(Response(Logs(2))); var state = State(h); var form = Query();
        await state.LoadAsync(form); form.Entity = "patients"; form.UserId = 15; form.From = new(2026, 9, 1);
        var publicSnapshot = state.AppliedFilter!; publicSnapshot.UserId = 999;
        await state.GoToPageAsync(2); await state.GoToPageAsync(3); await state.GoToPageAsync(0);
        Assert.Equal(2, h.Uris.Count); Assert.EndsWith("page=2&pageSize=20&entity=medical_records&userId=7&entityId=1204&from=2026-10-01&to=2026-10-13", h.Uris.Last());
        Assert.False(state.MatchesAppliedFilter(form)); Assert.True(state.MatchesAppliedFilter(Query())); Assert.Equal(2, state.Result.Page);
    }

    [Fact]
    public async Task EmptyPage_IsSuccessful_AndDoesNotInventMorePages()
    {
        var h = new Handler(); h.Responses.Enqueue(Response(Logs(total: 0))); var state = State(h); await state.LoadAsync(Query()); await state.GoToPageAsync(1);
        Assert.True(state.HasLoaded); Assert.Empty(state.Result.Items); Assert.Empty(state.ListError); Assert.Equal(0, state.Result.TotalPages); Assert.Single(h.Uris);
    }

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    public async Task LostAccess_ClearsLogsDetailsAndUserOptions(int status)
    {
        var h = new Handler(); h.Responses.Enqueue(Response(Logs())); h.Responses.Enqueue(Response(UserPage()));
        h.Responses.Enqueue(Response(new AuditAccessDetailDTO { Id = 9, Details = "private detail" }));
        h.Responses.Enqueue(new((HttpStatusCode)status) { Content = new StringContent("<html>no access</html>") });
        if (status == 401) h.Responses.Enqueue(new(HttpStatusCode.BadRequest));
        var state = State(h); await state.LoadAsync(Query()); await state.LoadUsersAsync(); await state.LoadDetailAsync(9); await state.LoadDetailAsync(10);
        Assert.Empty(state.Result.Items); Assert.Empty(state.Users.Items); Assert.Null(state.Detail); Assert.False(state.HasLoaded);
        Assert.Equal(status == 401, state.RequiresLogin); Assert.Equal(status == 403, state.AccessDenied); Assert.DoesNotContain("html", state.DetailError);
        var count = h.Uris.Count; await state.LoadAsync(Query()); await state.LoadUsersAsync(); Assert.Equal(count, h.Uris.Count);
    }

    [Fact]
    public async Task FailedReload_RemovesPreviousListAndDetail()
    {
        var h = new Handler(); h.Responses.Enqueue(Response(Logs())); h.Responses.Enqueue(Response(new AuditAccessDetailDTO { Id = 9, Details = "old detail" }));
        h.Responses.Enqueue(new(HttpStatusCode.InternalServerError)); var state = State(h);
        await state.LoadAsync(Query()); await state.LoadDetailAsync(9); await state.LoadAsync(new());
        Assert.False(state.HasLoaded); Assert.Empty(state.Result.Items); Assert.Null(state.Detail); Assert.NotEmpty(state.ListError);
    }

    [Theory]
    [InlineData("page")]
    [InlineData("total")]
    [InlineData("items-null")]
    [InlineData("record-id")]
    public async Task MalformedSuccess_IsRejected(string kind)
    {
        var page = Logs();
        switch (kind) { case "page": page.Page = 2; break; case "total": page.TotalPages = 99; break; case "items-null": page.Items = null!; break; case "record-id": page.Items[0].Id = 0; break; }
        var h = new Handler(); h.Responses.Enqueue(Response(page)); var state = State(h); await state.LoadAsync(Query());
        Assert.False(state.HasLoaded); Assert.Contains("không hợp lệ", state.ListError);
    }

    [Theory]
    [InlineData("network", "kết nối")]
    [InlineData("timeout", "thời gian chờ")]
    [InlineData("html", "không hợp lệ")]
    public async Task RequestFailures_HaveSafeMessageAndCanRetry(string kind, string message)
    {
        var h = new Handler();
        if (kind == "network") h.Failure = new HttpRequestException("secret details");
        else if (kind == "timeout") h.Failure = new TaskCanceledException("secret details");
        else h.Responses.Enqueue(new(HttpStatusCode.OK) { Content = new StringContent("<html>error</html>") });
        var state = State(h); await state.LoadAsync(Query()); Assert.Contains(message, state.ListError); Assert.DoesNotContain("secret", state.ListError); Assert.False(state.IsBusy);
        h.Failure = null; h.Responses.Enqueue(Response(Logs())); await state.LoadAsync(Query()); Assert.True(state.HasLoaded); Assert.Empty(state.ListError);
    }

    [Fact]
    public async Task Users_SearchIsEscapedAndPaged_AndDoesNotReplaceLoadedLogs()
    {
        var h = new Handler(); h.Responses.Enqueue(Response(Logs())); h.Responses.Enqueue(Response(UserPage())); h.Responses.Enqueue(Response(UserPage(2))); var state = State(h);
        await state.LoadAsync(Query()); await state.LoadUsersAsync(" A&B "); await state.LoadUsersAsync(state.UserKeyword, 2);
        Assert.Contains("page=1&pageSize=50&keyword=A%26B", h.Uris[1]); Assert.Contains("page=2&pageSize=50&keyword=A%26B", h.Uris[2]);
        Assert.Equal(60, state.Users.TotalItems); Assert.Equal(2, state.Users.Page); Assert.True(state.HasLoaded); Assert.True(state.MatchesAppliedFilter(Query()));
    }

    [Fact]
    public async Task InvalidUserLookup_DoesNotSendHttp_AndCanRetry()
    {
        var h = new Handler(); var state = State(h); await state.LoadUsersAsync(new string('x', 101));
        Assert.Empty(h.Uris); Assert.NotEmpty(state.UsersError); h.Responses.Enqueue(Response(UserPage())); await state.LoadUsersAsync(); Assert.Empty(state.UsersError);
    }

    [Fact]
    public async Task Detail_InvalidIdAndWrongResponseId_DoNotShowStaleOrDifferentDetails()
    {
        var h = new Handler(); var state = State(h); await state.LoadDetailAsync(0); Assert.Empty(h.Uris);
        h.Responses.Enqueue(Response(new AuditAccessDetailDTO { Id = 9, Details = "first" })); await state.LoadDetailAsync(9); Assert.Equal("first", state.Detail!.Details);
        h.Responses.Enqueue(Response(new AuditAccessDetailDTO { Id = 10, Details = "different" })); await state.LoadDetailAsync(9);
        Assert.Null(state.Detail); Assert.Contains("không hợp lệ", state.DetailError); state.CloseDetail(); Assert.Empty(state.DetailError);
    }

    [Fact]
    public async Task Detail_404KeepsListButClearsOldDetail()
    {
        var h = new Handler(); h.Responses.Enqueue(Response(Logs())); h.Responses.Enqueue(new(HttpStatusCode.NotFound)); var state = State(h);
        await state.LoadAsync(Query()); await state.LoadDetailAsync(9);
        Assert.True(state.HasLoaded); Assert.Null(state.Detail); Assert.Contains("Không tìm thấy", state.DetailError);
    }

    [Fact]
    public async Task InFlightRequest_BlocksCompetingListUsersDetailAndReset()
    {
        var h = new Handler { Hold = new(TaskCreationOptions.RunContinuationsAsynchronously) }; h.Responses.Enqueue(Response(Logs())); var state = State(h);
        var pending = state.LoadAsync(Query()); Assert.True(state.IsBusy); await state.LoadAsync(new()); await state.LoadUsersAsync(); await state.LoadDetailAsync(9); state.Reset();
        h.Hold.SetResult(true); await pending; Assert.Single(h.Uris); Assert.True(state.HasLoaded); Assert.False(state.IsBusy);
    }

    [Fact]
    public async Task MvcValidation_UsesReadableServerMessage()
    {
        var h = new Handler(); h.Responses.Enqueue(new(HttpStatusCode.BadRequest) { Content = new StringContent("{\"errors\":{\"To\":[\"Khoảng ngày không hợp lệ.\"]}}") });
        var state = State(h); await state.LoadAsync(Query()); Assert.Equal("Khoảng ngày không hợp lệ.", state.ListError);
    }

    private static PagedResult<AuditUserOptionDTO> UserPage(int page = 1) => new() { Page = page, PageSize = 50, TotalItems = 60, TotalPages = 2, Items = [new() { Id = 7, Username = "bacsi.a" }, new() { Id = 8 }] };
    private static HttpResponseMessage Response<T>(T value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(new HttpResponseData<T> { StatusCode = 200, Content = value }) };
    private static AdminAuditLogStateService State(Handler handler)
    {
        var factory = new Mock<IHttpClientFactory>(); factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(handler, false) { BaseAddress = new("http://fixture.local") });
        var user = new UserStateService(null!, factory.Object, null!, new CustomAuthenticationStateProvider());
        typeof(UserStateService).GetProperty(nameof(UserStateService.AccessToken))!.SetValue(user, "fixture-token");
        typeof(UserStateService).GetProperty(nameof(UserStateService.RefreshToken))!.SetValue(user, "fixture-refresh");
        return new(new AuthorizedApiService(factory.Object, user));
    }
    private sealed class Handler : HttpMessageHandler
    {
        public Queue<HttpResponseMessage> Responses { get; } = new();
        public List<string> Uris { get; } = []; public List<string?> Authorization { get; } = [];
        public Exception? Failure { get; set; } public TaskCompletionSource<bool>? Hold { get; set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Uris.Add(request.RequestUri!.AbsoluteUri); Authorization.Add(request.Headers.Authorization?.ToString());
            if (Hold != null) await Hold.Task;
            if (Failure != null) throw Failure;
            return Responses.Dequeue();
        }
    }
}
