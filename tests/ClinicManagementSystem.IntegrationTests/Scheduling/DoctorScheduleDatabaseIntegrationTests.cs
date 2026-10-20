using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Doctor;
using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ClinicManagementSystem.IntegrationTests.Scheduling;

// Actual JWT -> controller -> service -> repositories -> isolated SQL Server container.
public class DoctorScheduleDatabaseIntegrationTests(CustomWebApplicationFactory factory):IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task OwnCalendarAndLeave_RespectEffectiveDates_Scope_Paging_AndInactiveDoctor()
    {
        await using var scope=factory.Services.CreateAsyncScope();
        var db=scope.ServiceProvider.GetRequiredService<ClinicManagementDbContext>();await db.Database.EnsureCreatedAsync();
        var specialty=new Specialty{Name="Schedule tests",Code="SCH"+Guid.NewGuid().ToString("N")[..10],IsActive=true,CreatedAt=DateTime.UtcNow};
        var own=Doctor(specialty);var other=Doctor(specialty);db.Doctors.AddRange(own,other);await db.SaveChangesAsync();
        var old=Shift(own,2,new(2026,9,1));old.EndTime=new(15,0);
        var current=Shift(own,2,new(2026,10,1));
        var expired=Shift(own,3,new(2026,9,1));expired.EffectiveTo=new(2026,10,18);
        var foreign=Shift(other,2,new(2026,10,1));
        db.DoctorSchedules.AddRange(old,current,expired,foreign);
        var personal=new DoctorTimeOff{DoctorId=own.Id,StartAt=new(2026,10,20,14,0,0),EndAt=new(2026,10,20,16,0,0),Type=0,Reason="Own private leave",CreatedAt=DateTime.UtcNow};
        var clinic=new DoctorTimeOff{DoctorId=null,StartAt=new(2026,10,21),EndAt=new(2026,10,22),Type=2,Reason="Clinic closure",IsFullDay=true,CreatedAt=DateTime.UtcNow};
        var foreignLeave=new DoctorTimeOff{DoctorId=other.Id,StartAt=new(2026,10,20,8,0,0),EndAt=new(2026,10,20,9,0,0),Type=0,Reason="Must not leak",CreatedAt=DateTime.UtcNow};
        db.DoctorTimeOffs.AddRange(personal,clinic,foreignLeave);await db.SaveChangesAsync();
        using var client=factory.CreateClient(new(){BaseAddress=new("https://localhost")});
        client.DefaultRequestHeaders.Authorization=new("Bearer",Token(own.UserId));
        using var response=await client.GetAsync($"/api/doctors/me/schedule?doctorId={other.Id}&from=2026-10-19&to=2026-10-25");
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        var week=(await response.Content.ReadFromJsonAsync<HttpResponseData<DoctorScheduleDTO>>())!.Content!;
        var tuesday=week.Days.Single(d=>d.Date==new DateOnly(2026,10,20));
        Assert.Equal(current.Id,Assert.Single(tuesday.Shifts).Id);
        Assert.Equal(personal.Id,Assert.Single(tuesday.TimeOffs).Id);
        Assert.Equal(new[]{(8,12),(13,14),(16,17)},tuesday.Blocks.Where(b=>b.Kind=="work").Select(b=>(b.StartAt.Hour,b.EndAt.Hour)));
        var wednesday=week.Days.Single(d=>d.Date==new DateOnly(2026,10,21));
        Assert.Empty(wednesday.Shifts);Assert.Equal(clinic.Id,Assert.Single(wednesday.TimeOffs).Id);
        Assert.Empty(week.Days.Single(d=>d.Date==new DateOnly(2026,10,22)).TimeOffs);
        Assert.DoesNotContain("Must not leak",await response.Content.ReadAsStringAsync());

        var page1=await client.GetFromJsonAsync<HttpResponseData<PagedResult<DoctorTimeOffDTO>>>($"/api/doctors/me/time-off?doctorId={other.Id}&from=2026-10-19&to=2026-10-25&pageSize=1");
        Assert.Equal(2,page1!.Content!.TotalItems);Assert.Equal(clinic.Id,Assert.Single(page1.Content.Items).Id);
        var page2=await client.GetFromJsonAsync<HttpResponseData<PagedResult<DoctorTimeOffDTO>>>("/api/doctors/me/time-off?from=2026-10-19&to=2026-10-25&pageSize=1&page=2");
        Assert.Equal(personal.Id,Assert.Single(page2!.Content!.Items).Id);
        var clinicOnly=await client.GetFromJsonAsync<HttpResponseData<PagedResult<DoctorTimeOffDTO>>>("/api/doctors/me/time-off?scope=1&type=2");
        Assert.Equal(clinic.Id,Assert.Single(clinicOnly!.Content!.Items).Id);
        var personalOnly=await client.GetFromJsonAsync<HttpResponseData<PagedResult<DoctorTimeOffDTO>>>("/api/doctors/me/time-off?scope=0&type=0");
        Assert.Equal(personal.Id,Assert.Single(personalOnly!.Content!.Items).Id);
        own.IsActive=false;await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync("/api/doctors/me/schedule")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync("/api/doctors/me/time-off")).StatusCode);
    }
    private static Doctor Doctor(Specialty specialty)
    {
        var id=Guid.NewGuid().ToString("N");
        return new(){FullName="Schedule Doctor "+id[..4],Specialty=specialty,IsActive=true,CreatedAt=DateTime.UtcNow,
            User=new(){Username="schedule_"+id,PasswordHash="only-a-fixture-hash",Email=id+"@example.com",FullName="Schedule Doctor",IsActive=true,EmailConfirmed=true,CreatedAt=DateTime.UtcNow}};
    }
    private static DoctorSchedule Shift(Doctor doctor,byte day,DateOnly from)=>new(){Doctor=doctor,DayOfWeek=day,StartTime=new(8,0),EndTime=new(17,0),BreakStart=new(12,0),BreakEnd=new(13,0),SlotMinutes=20,IsActive=true,EffectiveFrom=from};
    private static string Token(int id)=>new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("ClinicManagementSystem.IntegrationTests","ClinicManagementSystem.IntegrationTests",
        [new(ClaimTypes.NameIdentifier,id.ToString()),new(ClaimTypes.Role,"Doctor")],expires:DateTime.UtcNow.AddMinutes(5),
        signingCredentials:new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ClinicIntegrationTestSecretKey_2026_AtLeast32Characters!")),SecurityAlgorithms.HmacSha256)));
}
