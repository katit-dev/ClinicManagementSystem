using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Doctor;
using ClinicManagementSystem.Application.Helpers;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Web.Pages.Doctor;
using ClinicManagementSystem.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClinicManagementSystem.UnitTests.Web;

public class DoctorScheduleTests
{
    [Fact]
    public async Task WeekNavigation_NormalizesToMonday_AndTodayUsesServerDefault()
    {
        var handler=new Handler(_=>Envelope(Week()));var state=new DoctorScheduleStateService(Api(handler));
        await state.LoadWeekAsync(new(2026,10,21));await state.LoadWeekAsync();
        Assert.Equal("/api/doctors/me/schedule?from=2026-10-19&to=2026-10-25",handler.Paths[0]);
        Assert.Equal("/api/doctors/me/schedule",handler.Paths[1]);
    }

    [Fact]
    public async Task LeaveFiltersAndPaging_ArePreserved_AndUnappliedChangesAreDetected()
    {
        var handler=new Handler(_=>Envelope(LeavePage()));var state=new DoctorScheduleStateService(Api(handler))
            {From=new(2026,10,1),To=new(2026,10,31),Scope=0,Type=2};
        await state.LoadTimeOffAsync(2);
        Assert.Contains("page=2&pageSize=10&from=2026-10-01&to=2026-10-31&scope=0&type=2",handler.Paths[0]);
        Assert.False(state.HasUnappliedFilters);state.Scope=1;Assert.True(state.HasUnappliedFilters);
    }

    [Fact]
    public async Task PartialDateRange_IsRejectedBeforeHttp()
    {
        var handler=new Handler(_=>Envelope(LeavePage()));var state=new DoctorScheduleStateService(Api(handler)){From=new(2026,10,1)};
        await state.LoadTimeOffAsync();Assert.Empty(handler.Paths);Assert.Contains("chọn đủ",state.TimeOffError);
    }

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(500)]
    public async Task FailedRefresh_DoesNotRetainOldScheduleOrLeaveReasons(int status)
    {
        var fail=false;
        var handler=new Handler(r=>fail?new((HttpStatusCode)status){Content=JsonContent.Create(new{status})}:
            r.RequestUri!.AbsolutePath.EndsWith("time-off")?Envelope(LeavePage()):Envelope(Week()));
        var state=new DoctorScheduleStateService(Api(handler));
        await state.LoadWeekAsync();await state.LoadTimeOffAsync();Assert.NotNull(state.Schedule);Assert.NotEmpty(state.TimeOff.Items);
        fail=true;await state.LoadWeekAsync();await state.LoadTimeOffAsync();
        Assert.Null(state.Schedule);Assert.Empty(state.TimeOff.Items);Assert.NotEmpty(state.WeekError);Assert.NotEmpty(state.TimeOffError);
    }

    [Fact]
    public async Task LateResponse_AfterReset_CannotRestorePreviousUserData()
    {
        var pending=new TaskCompletionSource<HttpResponseMessage>();var state=new DoctorScheduleStateService(Api(new PendingHandler(pending.Task)));
        var task=state.LoadWeekAsync();state.Reset();pending.SetResult(Envelope(Week()));await task;
        Assert.Null(state.Schedule);Assert.False(state.IsLoadingWeek);
    }

    [Fact]
    public async Task LaterWeekResponse_Wins_WhenRequestsFinishOutOfOrder()
    {
        var first=new TaskCompletionSource<HttpResponseMessage>();var second=new TaskCompletionSource<HttpResponseMessage>();
        var state=new DoctorScheduleStateService(Api(new QueueHandler(first.Task,second.Task)));
        var old=state.LoadWeekAsync(new(2026,10,20));var latest=state.LoadWeekAsync(new(2026,10,27));
        var next=Week();next.From=new(2026,10,26);second.SetResult(Envelope(next));await latest;
        first.SetResult(Envelope(Week()));await old;Assert.Equal(new DateOnly(2026,10,26),state.Schedule!.From);
    }

    [Theory]
    [InlineData("week")]
    [InlineData("empty")]
    [InlineData("error")]
    public async Task ActualRazor_RendersOwnSchedule_AndCorrectEmptyOrDeniedState(string scenario)
    {
        var handler=new Handler(_=>scenario=="error"?new(HttpStatusCode.Forbidden):Envelope(Week(scenario=="empty")));
        var state=new DoctorScheduleStateService(Api(handler));
        using var services=new ServiceCollection().AddLogging().AddSingleton(state).BuildServiceProvider();
        await using var renderer=new HtmlRenderer(services,services.GetRequiredService<ILoggerFactory>());
        var raw=await renderer.Dispatcher.InvokeAsync(async()=>(await renderer.RenderComponentAsync<Schedule>()).ToHtmlString());
        var html=WebUtility.HtmlDecode(raw);
        Assert.Contains("Lịch làm việc & nghỉ phép",html);Assert.Contains("Nghỉ đã đăng ký",html);
        if(scenario=="week")
        {
            Assert.Contains("Phòng khám nghỉ",html);Assert.Contains("Nghỉ cá nhân",html);Assert.Contains("08:00 – 12:00",html);
            Assert.Contains("Nghỉ giữa ca: 12:00 – 13:00",html);Assert.Contains("GMT+7",html);
            var preview=Environment.GetEnvironmentVariable("DOCTOR_SCHEDULE_PREVIEW_DIR");
            if(!string.IsNullOrWhiteSpace(preview)){Directory.CreateDirectory(preview);await File.WriteAllTextAsync(Path.Combine(preview,"week.html"),raw);}
        }
        else if(scenario=="empty")Assert.Contains("chưa được phân công ca làm",html);
        else{Assert.Contains("role=\"alert\"",raw);Assert.Contains("Thử lại",html);Assert.DoesNotContain("doctor-schedule-block work",raw);}
        Assert.DoesNotContain("Đăng ký nghỉ</button>",html);
        Assert.Equal("Doctor",typeof(Schedule).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
    }

    [Fact]
    public void FullDayEnd_IsPresentedAsLastIncludedDay_NotNextMidnight()
    {
        Assert.Equal("21/10/2026 · Cả ngày",DoctorScheduleDisplay.Period(new(){IsFullDay=true,StartAt=new(2026,10,21),EndAt=new(2026,10,22)}));
        Assert.Equal("21/10/2026 – 23/10/2026 · Cả ngày",DoctorScheduleDisplay.Period(new(){IsFullDay=true,StartAt=new(2026,10,21),EndAt=new(2026,10,24)}));
        Assert.Equal("00:00 – 24:00",DoctorScheduleDisplay.TimeRange(new(2026,10,21),new(2026,10,22)));
    }

    private static DoctorScheduleDTO Week(bool empty=false)
    {
        var from=new DateOnly(2026,10,19);var now=new DateTime(2026,10,20,9,0,0);
        var shifts=Enumerable.Range(1,6).Select(day=>new DoctorSchedule{Id=day,DoctorId=7,DayOfWeek=(byte)day,IsActive=true,
            StartTime=new(8,0),EndTime=day==6?new(12,0):new(17,0),BreakStart=day==6?null:new(12,0),BreakEnd=day==6?null:new(13,0),SlotMinutes=20,EffectiveFrom=new(2026,10,1)}).ToList();
        var leave=new List<DoctorTimeOff>{new(){Id=1,DoctorId=7,Type=0,Reason="Việc cá nhân",StartAt=new(2026,10,21,13,0,0),EndAt=new(2026,10,21,17,0,0)},
            new(){Id=2,DoctorId=null,Type=2,Reason="Lịch nghỉ chung minh họa",IsFullDay=true,StartAt=new(2026,10,22),EndAt=new(2026,10,23)}};
        return new(){From=from,To=from.AddDays(6),ClinicNow=now,DoctorName="Lê Thu Hà",Days=DoctorScheduleTimeline.Build(7,from,from.AddDays(6),empty?[]:shifts,empty?[]:leave,now)};
    }
    private static PagedResult<DoctorTimeOffDTO> LeavePage()=>new(){Page=1,PageSize=10,TotalItems=1,TotalPages=1,Items=[new(){Id=1,Reason="Private reason"}]};
    private static HttpResponseMessage Envelope<T>(T data)=>new(HttpStatusCode.OK){Content=JsonContent.Create(new HttpResponseData<T>{StatusCode=200,Content=data})};
    private static AuthorizedApiService Api(HttpMessageHandler handler)
    {
        var factory=new Mock<IHttpClientFactory>();factory.Setup(f=>f.CreateClient(It.IsAny<string>())).Returns(()=>new HttpClient(handler,false){BaseAddress=new("http://fixture.local")});
        var user=new UserStateService(null!,factory.Object,null!,new CustomAuthenticationStateProvider());
        typeof(UserStateService).GetProperty(nameof(UserStateService.AccessToken))!.SetValue(user,"test-token");return new(factory.Object,user);
    }
    private sealed class Handler(Func<HttpRequestMessage,HttpResponseMessage> respond):HttpMessageHandler
    {public List<string> Paths{get;}=[];protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken t){Paths.Add(r.RequestUri!.PathAndQuery);return Task.FromResult(respond(r));}}
    private sealed class PendingHandler(Task<HttpResponseMessage> pending):HttpMessageHandler
    {protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken t)=>pending;}
    private sealed class QueueHandler(params Task<HttpResponseMessage>[] responses):HttpMessageHandler
    {private int _index;protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken t)=>responses[_index++];}
}
