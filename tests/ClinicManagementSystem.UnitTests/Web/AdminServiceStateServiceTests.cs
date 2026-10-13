using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Service;
using ClinicManagementSystem.Application.DTOs.Specialty;
using ClinicManagementSystem.Web.Services;
using Moq;

namespace ClinicManagementSystem.UnitTests.Web;

public class AdminServiceStateServiceTests
{
    [Fact]
    public async Task Filters_EncodeKeywordAndPreserveNullableFiltersAndPagination()
    {
        var handler = new RecordingHandler();
        handler.Responses.Enqueue(Envelope(200, new PagedResult<AdminServiceDTO>
        { Items = [new() { Id = 5, Code = null, DurationMinutes = null }], Page = 2, PageSize = 10, TotalItems = 11, TotalPages = 2 }));
        var state = CreateState(handler);
        await state.LoadAsync(new() { Keyword = " DV & test ", Active = false, SpecialtyId = 3, Page = 2 });

        Assert.Contains("keyword=DV%20%26%20test", handler.Uris[0]);
        Assert.Contains("active=false", handler.Uris[0]);
        Assert.Contains("specialtyId=3", handler.Uris[0]);
        Assert.Contains("page=2&pageSize=10", handler.Uris[0]);
        Assert.Equal(2, state.Result.Page);
        Assert.Null(Assert.Single(state.Result.Items).DurationMinutes);
        Assert.False(state.IsLoading);
    }

    [Fact]
    public async Task DefaultFilters_DoNotSendNullValues()
    {
        var handler = new RecordingHandler(); handler.Responses.Enqueue(Envelope(200, new PagedResult<AdminServiceDTO>()));
        var state = CreateState(handler); await state.LoadAsync(new());
        Assert.EndsWith("/api/admin/services?page=1&pageSize=10", Assert.Single(handler.Uris));
    }

    [Fact]
    public async Task Specialties_LoadsEveryPageIncludingInactiveHistoricalOptions()
    {
        var handler = new RecordingHandler();
        handler.Responses.Enqueue(Envelope(200, new PagedResult<AdminSpecialtyDTO>
        { Items = [new() { Id = 1, Name = "Tim mạch", IsActive = true }], TotalPages = 2 }));
        handler.Responses.Enqueue(Envelope(200, new PagedResult<AdminSpecialtyDTO>
        { Items = [new() { Id = 2, Name = "Cũ", IsActive = false }], TotalPages = 2 }));
        var state = CreateState(handler); await state.LoadSpecialtiesAsync();
        Assert.Equal(2, state.Specialties.Count);
        Assert.Contains(state.Specialties, s => s.Id == 2 && !s.IsActive);
        Assert.Contains("page=1&pageSize=100", handler.Uris[0]);
        Assert.Contains("page=2&pageSize=100", handler.Uris[1]);
        Assert.False(state.IsBusy);
    }

    [Fact]
    public async Task Specialties_FailedSecondPageDoesNotPublishPartialDropdown()
    {
        var handler = new RecordingHandler();
        handler.Responses.Enqueue(Envelope(200, new PagedResult<AdminSpecialtyDTO>
        { Items = [new() { Id = 1 }], TotalPages = 2 }));
        handler.Responses.Enqueue(new(HttpStatusCode.Forbidden));
        var state = CreateState(handler); await state.LoadSpecialtiesAsync();
        Assert.Empty(state.Specialties); Assert.Contains("không có quyền", state.SpecialtiesError);
        Assert.False(state.IsLoadingSpecialties);
    }

    [Fact]
    public async Task Edit_ReadsCurrentDetailAndPreservesLegacyMissingDuration()
    {
        var handler = new RecordingHandler();
        handler.Responses.Enqueue(Envelope(200, new AdminServiceDTO
        { Id = 5, Name = "Thông tin mới", Price = 350000.50m, DurationMinutes = null, IsActive = false }));
        var state = CreateState(handler); var detail = await state.LoadDetailAsync(5);
        Assert.EndsWith("/api/admin/services/5", Assert.Single(handler.Uris));
        Assert.Equal("Thông tin mới", detail!.Name); Assert.False(detail.IsActive);
        Assert.Null(detail.DurationMinutes); Assert.False(state.IsLoadingDetail);
    }

    [Fact]
    public async Task Edit_MissingDetailIsSafeAndDoesNotReturnStaleData()
    {
        var handler = new RecordingHandler(); handler.Responses.Enqueue(new(HttpStatusCode.NotFound));
        var state = CreateState(handler); Assert.Null(await state.LoadDetailAsync(5));
        Assert.Contains("tải lại", state.ActionError); Assert.False(state.IsBusy);
    }

    [Theory]
    [InlineData(null, "POST", "/api/admin/services", 201)]
    [InlineData(5, "PUT", "/api/admin/services/5", 200)]
    public async Task Save_SendsCorrectMethodAndFullBodyWithoutStatusField(int? id, string method, string path, int status)
    {
        var handler = new RecordingHandler(); handler.Responses.Enqueue(Envelope(status, new AdminServiceDTO { Id = 5 }));
        var state = CreateState(handler);
        Assert.True(await state.SaveAsync(id, Request()));
        Assert.Equal(method, Assert.Single(handler.Methods).Method);
        Assert.EndsWith(path, Assert.Single(handler.Uris));
        using var json = JsonDocument.Parse(Assert.Single(handler.Bodies));
        Assert.Equal(250000.50m, json.RootElement.GetProperty("price").GetDecimal());
        Assert.Equal(15, json.RootElement.GetProperty("durationMinutes").GetInt32());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("specialtyId").ValueKind);
        Assert.False(json.RootElement.TryGetProperty("isActive", out _));
        Assert.Equal("Thành công", state.ActionMessage); Assert.False(state.IsSubmitting);
    }

    [Fact]
    public async Task Deactivate_UsesPatchAndEmptyBodyForExactId()
    {
        var handler = new RecordingHandler(); handler.Responses.Enqueue(Envelope(200, new AdminServiceDTO { Id = 23, IsActive = false }));
        var state = CreateState(handler); Assert.True(await state.DeactivateAsync(23));
        Assert.Equal(HttpMethod.Patch, Assert.Single(handler.Methods));
        Assert.EndsWith("/api/admin/services/23/deactivate", Assert.Single(handler.Uris));
        Assert.Empty(Assert.Single(handler.Bodies));
    }

    [Fact]
    public async Task Conflict_LeavesOriginalRequestIntactAndDoesNotShowSuccess()
    {
        var handler = new RecordingHandler(); handler.Responses.Enqueue(Envelope<AdminServiceDTO>(409, null, "Mã đã được sử dụng."));
        var state = CreateState(handler); var request = Request();
        Assert.False(await state.SaveAsync(5, request));
        Assert.Equal("Mã đã được sử dụng.", state.ActionError); Assert.Empty(state.ActionMessage);
        Assert.Equal(250000.50m, request.Price); Assert.Equal("DV001", request.Code);
        Assert.False(state.IsSubmitting);
    }

    [Fact]
    public async Task ValidationProblem_ShowsAllFieldErrors()
    {
        var handler = new RecordingHandler();
        handler.Responses.Enqueue(new(HttpStatusCode.BadRequest) { Content = JsonContent.Create(new
        { errors = new { Price = new[] { "Giá không hợp lệ." }, DurationMinutes = new[] { "Cần nhập thời lượng." } } }) });
        var state = CreateState(handler); Assert.False(await state.SaveAsync(null, Request()));
        Assert.Contains("Giá không hợp lệ.", state.ActionError); Assert.Contains("Cần nhập thời lượng.", state.ActionError);
    }

    [Fact]
    public async Task FailedReload_ClearsStaleRowsButKeepsSuccessfulMutationMessage()
    {
        var handler = new RecordingHandler();
        handler.Responses.Enqueue(Envelope(200, new PagedResult<AdminServiceDTO> { Items = [new() { Id = 5 }] }));
        handler.Responses.Enqueue(Envelope(200, new AdminServiceDTO { Id = 5 }));
        handler.Responses.Enqueue(new(HttpStatusCode.Forbidden));
        var state = CreateState(handler); await state.LoadAsync(new());
        Assert.True(await state.DeactivateAsync(5)); await state.LoadAsync(new());
        Assert.Empty(state.Result.Items); Assert.Contains("không có quyền", state.ListError);
        Assert.Equal("Thành công", state.ActionMessage); Assert.False(state.IsBusy);
    }

    [Theory]
    [InlineData("network", "kết nối")]
    [InlineData("timeout", "thời gian")]
    [InlineData("malformed", "không hợp lệ")]
    public async Task LoadFailure_ResetsFlagsAndSupportsRetry(string failure, string message)
    {
        var handler = new RecordingHandler();
        if (failure == "network") handler.Failure = new HttpRequestException("Internal fixture");
        else if (failure == "timeout") handler.Failure = new TaskCanceledException("Internal fixture");
        else handler.Responses.Enqueue(new(HttpStatusCode.OK) { Content = new StringContent("<html>Bad gateway</html>") });
        var state = CreateState(handler); await state.LoadAsync(new());
        Assert.Contains(message, state.ListError); Assert.DoesNotContain("Internal", state.ListError); Assert.False(state.IsBusy);
        handler.Failure = null; handler.Responses.Enqueue(Envelope(200, new PagedResult<AdminServiceDTO> { Items = [new() { Id = 5 }] }));
        await state.LoadAsync(new()); Assert.Empty(state.ListError); Assert.Single(state.Result.Items);
    }

    [Fact]
    public async Task Http200WithErrorEnvelope_IsNotSuccess()
    {
        var handler = new RecordingHandler(); handler.Responses.Enqueue(new(HttpStatusCode.OK)
        { Content = JsonContent.Create(new HttpResponseData<AdminServiceDTO> { StatusCode = 409, Message = "Conflict", Content = new() { Id = 5 } }) });
        var state = CreateState(handler); Assert.False(await state.SaveAsync(null, Request()));
        Assert.Empty(state.ActionMessage); Assert.Equal("Conflict", state.ActionError);
    }

    [Fact]
    public async Task DoubleSubmit_SendsOnlyOneMutationAndBlocksCompetingLoads()
    {
        var handler = new RecordingHandler { Hold = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        handler.Responses.Enqueue(Envelope(201, new AdminServiceDTO { Id = 5 }));
        var state = CreateState(handler); var first = state.SaveAsync(null, Request());
        Assert.True(state.IsBusy); Assert.False(await state.SaveAsync(null, Request()));
        Assert.False(await state.DeactivateAsync(5));
        await state.LoadAsync(new()); await state.LoadSpecialtiesAsync();
        Assert.Null(await state.LoadDetailAsync(5));
        handler.Hold.SetResult(true); Assert.True(await first);
        Assert.Single(handler.Uris); Assert.False(state.IsBusy);
    }

    private static SaveServiceRequestDTO Request() => new() { Code = "DV001", Name = "Xét nghiệm", Price = 250000.50m, DurationMinutes = 15 };
    private static HttpResponseMessage Envelope<T>(int status, T? content, string message = "Thành công") => new((HttpStatusCode)status)
    { Content = JsonContent.Create(new HttpResponseData<T> { StatusCode = status, Content = content, Message = message }) };
    private static AdminServiceStateService CreateState(RecordingHandler handler)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(handler, false) { BaseAddress = new("http://fixture.local") });
        var user = new UserStateService(null!, factory.Object, null!, new CustomAuthenticationStateProvider());
        typeof(UserStateService).GetProperty(nameof(UserStateService.AccessToken))!.SetValue(user, "fixture-token");
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
            Uris.Add(request.RequestUri!.AbsoluteUri); Methods.Add(request.Method);
            Bodies.Add(request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));
            if (Hold != null) await Hold.Task;
            if (Failure != null) throw Failure;
            return Responses.Dequeue();
        }
    }
}
