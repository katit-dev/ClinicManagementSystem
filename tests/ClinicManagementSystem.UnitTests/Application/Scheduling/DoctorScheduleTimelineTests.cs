using ClinicManagementSystem.Application.DTOs.Doctor;
using ClinicManagementSystem.Application.Helpers;
using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.Repositories;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ClinicManagementSystem.UnitTests.Application.Scheduling;

public class DoctorScheduleTimelineTests
{
    private static readonly DateOnly Day = new(2026, 10, 20);
    private static readonly DateTime Now = new(2026, 10, 20, 10, 0, 0);
    private static DoctorSchedule Shift(int id = 1) => new() { Id = id, DoctorId = 7, DayOfWeek = 2, IsActive = true,
        StartTime = new(8,0), EndTime = new(17,0), BreakStart = new(12,0), BreakEnd = new(13,0), SlotMinutes = 20, EffectiveFrom = new(2026,10,1) };
    private static DoctorTimeOff Leave(int id, int? doctor, int start, int end) => new()
        { Id = id, DoctorId = doctor, StartAt = Day.ToDateTime(new(start,0)), EndAt = Day.ToDateTime(new(end,0)), Type = 0 };

    [Fact]
    public void PersonalLeaveAndBreak_AreSubtractedWithoutChangingAssignedShift()
    {
        var day = Assert.Single(DoctorScheduleTimeline.Build(7, Day, Day, [Shift()], [Leave(1,7,14,16), Leave(2,99,8,17)], Now));
        Assert.True(day.IsToday); Assert.Single(day.Shifts); Assert.Single(day.TimeOffs);
        Assert.Equal(new[] { (8,12), (13,14), (16,17) }, day.Blocks.Where(b=>b.Kind=="work").Select(b=>(b.StartAt.Hour,b.EndAt.Hour)));
        Assert.Single(day.Blocks,b=>b.Kind=="break");
        Assert.Equal(new TimeOnly(17,0), day.Shifts[0].EndTime);
    }

    [Fact]
    public void ClinicClosure_OverridesAllWork_AndExclusiveEndDoesNotSpillIntoNextDay()
    {
        var off = new DoctorTimeOff { Id=1, DoctorId=null, IsFullDay=true, StartAt=Day.ToDateTime(TimeOnly.MinValue), EndAt=Day.AddDays(1).ToDateTime(TimeOnly.MinValue) };
        var days=DoctorScheduleTimeline.Build(7,Day,Day.AddDays(1),[Shift()],[off],Now);
        Assert.DoesNotContain(days[0].Blocks,b=>b.Kind=="work");
        var closure=Assert.Single(days[0].Blocks,b=>b.Kind=="closure");
        Assert.Equal(off.EndAt,closure.EndAt); Assert.True(Assert.Single(days[0].TimeOffs).IsClinicWide);
        Assert.Empty(days[1].TimeOffs); Assert.Empty(days[1].Shifts);
    }

    [Fact]
    public void OverlappingLegacyLeave_IsUnioned_ForWorkingHours()
    {
        var day=Assert.Single(DoctorScheduleTimeline.Build(7,Day,Day,[Shift()],[Leave(1,7,9,14),Leave(2,null,11,16)],Now));
        Assert.Equal(new[]{(8,9),(16,17)},day.Blocks.Where(b=>b.Kind=="work").Select(b=>(b.StartAt.Hour,b.EndAt.Hour)));
        Assert.Equal(2,day.TimeOffs.Count);
    }

    [Fact]
    public void OnlyOwnActiveEffectiveSchedules_AreIncluded_WithLatestLegacyVersion()
    {
        var old=Shift(1); old.EffectiveFrom=new(2026,9,1);
        var current=Shift(2); current.EndTime=new(16,0);
        var future=Shift(3); future.EffectiveFrom=Day.AddDays(1);
        var expired=Shift(4); expired.EffectiveTo=Day.AddDays(-1);
        var inactive=Shift(5); inactive.IsActive=false;
        var other=Shift(6); other.DoctorId=99;
        var wrongDay=Shift(7); wrongDay.DayOfWeek=3;
        var day=Assert.Single(DoctorScheduleTimeline.Build(7,Day,Day,[old,current,future,expired,inactive,other,wrongDay],[],Now));
        Assert.Equal(2,Assert.Single(day.Shifts).Id);
        Assert.Equal(16,day.Blocks.Last(b=>b.Kind=="work").EndAt.Hour);
    }

    [Fact]
    public void LeaveWithoutAssignedShift_IsStillVisible_AndClippedToEachDay()
    {
        var off=new DoctorTimeOff{ Id=1,DoctorId=7,StartAt=Day.AddDays(-1).ToDateTime(new(15,0)),EndAt=Day.AddDays(1).ToDateTime(new(10,0)) };
        var days=DoctorScheduleTimeline.Build(7,Day,Day.AddDays(1),[],[off],Now);
        Assert.All(days,d=>Assert.Empty(d.Shifts));
        Assert.Equal(Day.ToDateTime(TimeOnly.MinValue),Assert.Single(days[0].Blocks).StartAt);
        Assert.Equal(10,Assert.Single(days[1].Blocks).EndAt.Hour);
    }

    [Theory]
    [InlineData(8,10,"ended")]
    [InlineData(10,11,"ongoing")]
    [InlineData(11,12,"upcoming")]
    public void PeriodStatus_UsesExclusiveEndAndClinicClock(int start,int end,string expected)
        => Assert.Equal(expected,DoctorScheduleTimeline.Map(Leave(1,7,start,end),Now).PeriodStatus);

    [Fact]
    public void VisibleWorkAndLeave_AgreeWithBookingAvailability()
    {
        var shift=Shift(); var leave=Leave(1,7,14,16);
        var day=Assert.Single(DoctorScheduleTimeline.Build(7,Day,Day,[shift],[leave],Now));
        var slots=DoctorSlotGenerator.Generate(Day,[shift],[leave],[],Day.ToDateTime(TimeOnly.MinValue));
        Assert.All(slots,slot=>Assert.Contains(day.Blocks,b=>b.Kind=="work"&&b.StartAt<=slot.StartTime&&b.EndAt>=slot.EndTime));
        Assert.Equal(18,slots.Count);
    }
}

public class DoctorScheduleServiceTests
{
    [Fact]
    public async Task DefaultWeek_UsesClinicDateAndLinkedDoctor_WithoutAnyWrites()
    {
        var (unit,doctors,schedules,leave)=Mocks();
        doctors.Setup(r=>r.GetActiveByUserIdAsync(42)).ReturnsAsync(new Doctor{ Id=7,FullName="Doctor" });
        // UTC Sunday 18:00 is already Monday in the clinic.
        var clock=new Clock(new DateTimeOffset(2026,10,18,18,0,0,TimeSpan.Zero));
        schedules.Setup(r=>r.GetInRangeAsync(7,new(2026,10,19),new(2026,10,25))).ReturnsAsync([]);
        leave.Setup(r=>r.GetOverlappingAsync(7,new(2026,10,19),new(2026,10,26))).ReturnsAsync([]);
        var service=new DoctorScheduleService(unit.Object,NullLogger<DoctorScheduleService>.Instance,clock);
        var result=await service.GetMyScheduleAsync(new(),42);
        Assert.Equal(200,result.StatusCode); Assert.Equal(7,result.Content!.Days.Count); Assert.Equal(new DateOnly(2026,10,19),result.Content.From);
        Assert.True(result.Content.Days[0].IsToday); schedules.VerifyAll(); leave.VerifyAll();
        unit.Verify(x=>x.SaveChangesAsync(),Times.Never); unit.Verify(x=>x.BeginTransactionAsync(),Times.Never);
    }

    [Theory]
    [InlineData(0,401)]
    [InlineData(42,403)]
    public async Task MissingActorOrActiveDoctor_DoesNotReadScheduleOrLeave(int actor,int status)
    {
        var (unit,_,schedules,leave)=Mocks();
        var service=new DoctorScheduleService(unit.Object,NullLogger<DoctorScheduleService>.Instance);
        Assert.Equal(status,(await service.GetMyScheduleAsync(new(),actor)).StatusCode);
        Assert.Equal(status,(await service.GetMyTimeOffAsync(new(),actor)).StatusCode);
        schedules.VerifyNoOtherCalls(); leave.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TimeOffPaging_PassesOwnScopeAndExclusiveRangeToRepository()
    {
        var (unit,doctors,_,leave)=Mocks();
        doctors.Setup(r=>r.GetActiveByUserIdAsync(42)).ReturnsAsync(new Doctor{ Id=7 });
        leave.Setup(r=>r.GetDoctorPageAsync(7,new(2026,10,1),new(2026,11,1),1,2,2,10))
            .ReturnsAsync((new List<DoctorTimeOff>{new(){Id=5,DoctorId=null,StartAt=new(2026,10,20,8,0,0),EndAt=new(2026,10,20,17,0,0)}},21));
        var service=new DoctorScheduleService(unit.Object,NullLogger<DoctorScheduleService>.Instance,new Clock(new(2026,10,20,3,0,0,TimeSpan.Zero)));
        var result=await service.GetMyTimeOffAsync(new(){From=new(2026,10,1),To=new(2026,10,31),Scope=1,Type=2,Page=2},42);
        Assert.Equal(200,result.StatusCode); Assert.Equal(3,result.Content!.TotalPages);
        Assert.Equal("ongoing",Assert.Single(result.Content.Items).PeriodStatus); leave.VerifyAll();
    }

    [Fact]
    public async Task InvalidFilters_AreRejectedBeforeRepositoryAccess()
    {
        var (unit,doctors,_,_)=Mocks();
        var service=new DoctorScheduleService(unit.Object,NullLogger<DoctorScheduleService>.Instance);
        Assert.Equal(400,(await service.GetMyScheduleAsync(new(){From=new(2026,1,1),To=new(2026,2,12)},42)).StatusCode);
        Assert.Equal(400,(await service.GetMyScheduleAsync(new(){From=new(2026,1,1)},42)).StatusCode);
        Assert.Equal(400,(await service.GetMyTimeOffAsync(new(){From=new(2026,1,1),To=DateOnly.MaxValue},42)).StatusCode);
        Assert.Equal(400,(await service.GetMyTimeOffAsync(new(){Page=0},42)).StatusCode);
        doctors.VerifyNoOtherCalls();
    }

    private static (Mock<IUnitOfWork>,Mock<IDoctorRepository>,Mock<IDoctorScheduleRepository>,Mock<IDoctorTimeOffRepository>) Mocks()
    {
        var unit=new Mock<IUnitOfWork>();var doctors=new Mock<IDoctorRepository>();var schedules=new Mock<IDoctorScheduleRepository>();var leave=new Mock<IDoctorTimeOffRepository>();
        unit.SetupGet(u=>u.DoctorRepository).Returns(doctors.Object);unit.SetupGet(u=>u.DoctorScheduleRepository).Returns(schedules.Object);unit.SetupGet(u=>u.DoctorTimeOffRepository).Returns(leave.Object);
        return(unit,doctors,schedules,leave);
    }
    private sealed class Clock(DateTimeOffset instant):TimeProvider{public override DateTimeOffset GetUtcNow()=>instant;}
}
