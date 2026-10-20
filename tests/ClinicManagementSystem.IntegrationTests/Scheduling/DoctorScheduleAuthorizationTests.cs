using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Doctor;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace ClinicManagementSystem.IntegrationTests.Scheduling;

public class DoctorScheduleAuthorizationTests(DoctorScheduleApiFactory factory):IClassFixture<DoctorScheduleApiFactory>
{
    public static IEnumerable<object?[]> AccessCases => new[]{"schedule","time-off"}.SelectMany(route=>
        new string?[]{null,"Patient","Admin","Receptionist","Pharmacist","Doctor"}.Select(role=>new object?[]{route,role,role==null?401:role=="Doctor"?200:403}));
    [Theory,MemberData(nameof(AccessCases))]
    public async Task BothRoutes_RequireDoctor(string route,string? role,int status)
    {
        using var client=Client(role);using var response=await client.GetAsync("/api/doctors/me/"+route);
        Assert.Equal(status,(int)response.StatusCode);
    }

    [Fact]
    public async Task Schedule_BindsDatesAndActor_AndDoesNotAcceptDoctorOverride()
    {
        using var client=Client("Doctor");using var response=await client.GetAsync("/api/doctors/me/schedule?doctorId=999&from=2026-10-19&to=2026-10-25");
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);Assert.True(response.Headers.CacheControl!.NoStore);
        var data=await response.Content.ReadFromJsonAsync<HttpResponseData<DoctorScheduleDTO>>();
        Assert.Equal("Actor 42",data!.Content!.DoctorName);Assert.Equal(new DateOnly(2026,10,19),data.Content.From);Assert.Equal(new DateOnly(2026,10,25),data.Content.To);
    }

    [Fact]
    public async Task Leave_BindsPagingAndScope_AndDoesNotAcceptDoctorOverride()
    {
        using var client=Client("Doctor");using var response=await client.GetAsync("/api/doctors/me/time-off?doctorId=999&scope=1&type=2&page=3&pageSize=5");
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);Assert.True(response.Headers.CacheControl!.NoStore);
        var data=await response.Content.ReadFromJsonAsync<HttpResponseData<PagedResult<DoctorTimeOffDTO>>>();
        Assert.Equal(3,data!.Content!.Page);Assert.Equal(5,data.Content.PageSize);
        var row=Assert.Single(data.Content.Items);Assert.Equal(42,row.Id);Assert.True(row.IsClinicWide);Assert.Equal((byte)2,row.Type);
    }

    [Theory]
    [InlineData("schedule?from=2026-10-19")]
    [InlineData("schedule?from=2026-10-25&to=2026-10-19")]
    [InlineData("schedule?from=2026-10-01&to=2026-11-12")]
    [InlineData("schedule?from=9999-12-30&to=9999-12-31")]
    [InlineData("schedule?from=invalid&to=2026-10-19")]
    [InlineData("time-off?from=2026-10-01")]
    [InlineData("time-off?from=2025-01-01&to=2026-10-20")]
    [InlineData("time-off?scope=2")]
    [InlineData("time-off?type=3")]
    [InlineData("time-off?page=0")]
    [InlineData("time-off?pageSize=101")]
    public async Task InvalidQuery_IsRejectedByMvc(string route)
    {
        using var client=Client("Doctor");Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync("/api/doctors/me/"+route)).StatusCode);
    }

    [Theory]
    [InlineData("schedule")]
    [InlineData("time-off")]
    public async Task DoctorEndpoint_HasNoWriteActions(string route)
    {
        using var client=Client("Doctor");Assert.Equal(HttpStatusCode.MethodNotAllowed,(await client.PostAsJsonAsync("/api/doctors/me/"+route,new{})).StatusCode);
    }

    private HttpClient Client(string? role)
    {
        var client=factory.CreateClient(new(){BaseAddress=new("https://localhost")});
        if(role!=null)
        {
            var token=new JwtSecurityToken("DoctorScheduleTests","DoctorScheduleTests",[new(ClaimTypes.NameIdentifier,"42"),new(ClaimTypes.Role,role)],
                expires:DateTime.UtcNow.AddMinutes(5),signingCredentials:new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(DoctorScheduleApiFactory.Key)),SecurityAlgorithms.HmacSha256));
            client.DefaultRequestHeaders.Authorization=new("Bearer",new JwtSecurityTokenHandler().WriteToken(token));
        }
        return client;
    }
}

public class DoctorScheduleApiFactory:WebApplicationFactory<Program>
{
    public const string Key="DoctorScheduleTests_OnlyTestSigningKey_2026!";
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:ClinicDb","Server=unused.invalid;Database=UnusedDoctorScheduleTests;Integrated Security=true;TrustServerCertificate=true");
        builder.UseSetting("Jwt:Key",Key);builder.UseSetting("Jwt:Issuer","DoctorScheduleTests");builder.UseSetting("Jwt:Audience","DoctorScheduleTests");
        builder.ConfigureTestServices(services=>
        {
            services.RemoveAll<IAccountAccessValidationService>();services.AddSingleton<IAccountAccessValidationService,TestAccountAccessValidationService>();
            services.RemoveAll<IMedicalAuditWriter>();services.AddSingleton<IMedicalAuditWriter,Audit.NoopMedicalAuditWriter>();
            services.RemoveAll<IDoctorScheduleService>();services.AddSingleton<IDoctorScheduleService,Stub>();
        });
    }
    private sealed class Stub:IDoctorScheduleService
    {
        public Task<HttpResponseData<DoctorScheduleDTO>> GetMyScheduleAsync(DoctorScheduleQueryDTO query,int userId)
            =>Task.FromResult(new HttpResponseData<DoctorScheduleDTO>{StatusCode=200,Content=new(){DoctorName=$"Actor {userId}",From=query.From??new(2026,10,19),To=query.To??new(2026,10,25)}});
        public Task<HttpResponseData<PagedResult<DoctorTimeOffDTO>>> GetMyTimeOffAsync(DoctorTimeOffQueryDTO query,int userId)
            =>Task.FromResult(new HttpResponseData<PagedResult<DoctorTimeOffDTO>>{StatusCode=200,Content=new(){Page=query.Page,PageSize=query.PageSize,TotalItems=1,TotalPages=1,
                Items=[new(){Id=userId,IsClinicWide=query.Scope==1,Type=query.Type}]}});
    }
}
