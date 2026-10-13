using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.User;
using ClinicManagementSystem.Application.Helpers;
using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace ClinicManagementSystem.IntegrationTests.AdminUsers;

public class AdminUserAuthorizationTests(AdminUserApiFactory factory) : IClassFixture<AdminUserApiFactory>
{
    private static readonly (string Method, string Path)[] Routes = {
        ("GET", "/api/admin/users"), ("GET", "/api/admin/users/5"), ("GET", "/api/admin/users/roles"),
        ("POST", "/api/admin/users/5/roles"), ("POST", "/api/admin/users/5/reset-password"), ("PATCH", "/api/admin/users/5/lock") };
    public static IEnumerable<object[]> AccessCases()
    {
        foreach(var route in Routes)
        foreach(var role in new string?[] { null, "Admin", "Doctor", "Receptionist", "Patient", "Pharmacist" })
            yield return new object[] { route.Method, route.Path, role!, role == null ? 401 : role == "Admin" ? 200 : 403 };
    }
    private HttpClient Client(string? role)
    {
        var client = factory.CreateClient(new() { BaseAddress = new("https://localhost") });
        if (role != null) client.DefaultRequestHeaders.Authorization = new("Bearer", AccessToken(role));
        return client;
    }
    private string AccessToken(string role)
    {
        var user = factory.Users.Items.Values.Single(u => u.UserRoleUsers.Any(r => r.Role.Name == role));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["Jwt:Key"] = AdminUserApiFactory.Key,
            ["Jwt:Issuer"] = "AdminUserTests", ["Jwt:Audience"] = "AdminUserTests", ["Jwt:AccessTokenExpirationMinutes"] = "5" }).Build();
        return new JwtAuthService(config).GenerateAccessToken(user, user.UserRoleUsers.Select(r => r.Role.Name).ToList());
    }
    private static HttpRequestMessage Request(string method, string path)
    {
        var request = new HttpRequestMessage(new(method), path);
        if (path.EndsWith("/roles") && method == "POST") request.Content = JsonContent.Create(new SetUserRolesRequestDTO { RoleIds = new[] { 2, 3 } });
        if (path.EndsWith("/reset-password")) request.Content = JsonContent.Create(new AdminResetPasswordRequestDTO { NewPassword = "VC22-Test-Only!", ConfirmPassword = "VC22-Test-Only!" });
        if (path.EndsWith("/lock")) request.Content = JsonContent.Create(new SetUserLockRequestDTO { IsActive = false, Reason = "Test local" });
        return request;
    }
    [Theory] [MemberData(nameof(AccessCases))]
    public async Task EveryRouteRequiresCurrentAccountAndAdminRole(string method, string path, string? role, int expected)
    {
        using var client = Client(role); using var request = Request(method, path); using var response = await client.SendAsync(request);
        Assert.Equal(expected, (int)response.StatusCode);
    }
    [Theory]
    [InlineData("GET", "/api/admin/users?page=0", null)]
    [InlineData("GET", "/api/admin/users?pageSize=101", null)]
    [InlineData("GET", "/api/admin/users?active=invalid", null)]
    [InlineData("POST", "/api/admin/users/5/roles", "{}")]
    [InlineData("POST", "/api/admin/users/5/roles", "{\"roleIds\":[0]}")]
    [InlineData("POST", "/api/admin/users/5/roles", "{\"roleIds\":[2,2]}")]
    [InlineData("POST", "/api/admin/users/5/reset-password", "{\"newPassword\":\"short\",\"confirmPassword\":\"short\"}")]
    [InlineData("POST", "/api/admin/users/5/reset-password", "{\"newPassword\":\"test-password\",\"confirmPassword\":\"different\"}")]
    [InlineData("PATCH", "/api/admin/users/5/lock", "{\"reason\":\"Test\"}")]
    [InlineData("PATCH", "/api/admin/users/5/lock", "{\"isActive\":false,\"reason\":\"   \"}")]
    public async Task InvalidInputCannotReachTheApplication(string method, string path, string? json)
    {
        using var client = Client("Admin"); using var request = new HttpRequestMessage(new(method), path);
        if(json != null) request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    [Theory] [InlineData("locked")] [InlineData("reset")] [InlineData("changedRoles")] [InlineData("deleted")]
    public async Task AlreadyIssuedJwtFailsAfterAccountChanges(string change)
    {
        using var client = Client("Admin"); var user = factory.Users.Items[7];
        var oldHash = user.PasswordHash; var oldRoles = user.UserRoleUsers; var oldUpdated = user.UpdatedAt;
        try
        {
            if(change == "locked") user.IsActive = false;
            if(change == "deleted") user.IsDeleted = true;
            if(change == "reset") user.PasswordHash = "new-test-only-hash";
            if(change == "changedRoles") user.UserRoleUsers = new List<UserRole>();
            using var response = await client.GetAsync("/api/admin/users"); Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        finally { user.IsActive = true; user.IsDeleted = false; user.PasswordHash = oldHash; user.UserRoleUsers = oldRoles; user.UpdatedAt = oldUpdated; }
    }
    [Fact]
    public async Task LegacyJwtWithoutStampIsRejectedEvenWithAdminRole()
    {
        using var client = Client(null);
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AdminUserApiFactory.Key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken("AdminUserTests", "AdminUserTests", new[] {new Claim(ClaimTypes.NameIdentifier,"7"),new Claim(ClaimTypes.Role,"Admin")},
            expires:DateTime.UtcNow.AddMinutes(5),signingCredentials:credentials);
        client.DefaultRequestHeaders.Authorization = new("Bearer",new JwtSecurityTokenHandler().WriteToken(token));
        using var response = await client.GetAsync("/api/admin/users"); Assert.Equal(HttpStatusCode.Unauthorized,response.StatusCode);
    }
    [Fact]
    public async Task FreshJwtWorksAfterUnlockWhileOldJwtStaysInvalid()
    {
        using var oldClient = Client("Admin"); var user = factory.Users.Items[7]; var oldUpdated = user.UpdatedAt;
        try
        {
            user.UpdatedAt = DateTime.UtcNow;
            using var fresh = Client("Admin");
            Assert.Equal(HttpStatusCode.Unauthorized,(await oldClient.GetAsync("/api/admin/users")).StatusCode);
            Assert.Equal(HttpStatusCode.OK,(await fresh.GetAsync("/api/admin/users")).StatusCode);
        }
        finally { user.UpdatedAt = oldUpdated; }
    }
}

public class AdminUserApiFactory : WebApplicationFactory<Program>
{
    public const string Key = "AdminUserTests_Key_AtLeast32Characters_2026!";
    public FixtureUsers Users { get; } = new();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing"); builder.UseSetting("ConnectionStrings:ClinicDb", "Server=localhost;Database=UnusedAdminUserTests;Integrated Security=true;TrustServerCertificate=true");
        builder.UseSetting("Jwt:Key",Key); builder.UseSetting("Jwt:Issuer","AdminUserTests"); builder.UseSetting("Jwt:Audience","AdminUserTests");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IUserRepository>(); services.AddSingleton<IUserRepository>(Users);
            services.RemoveAll<IAdminUserManagementService>(); services.AddSingleton<IAdminUserManagementService, StubUserManagement>();
            // Production JWT account validation remains installed; only the repository is in memory.
        });
    }
    private sealed class StubUserManagement : IAdminUserManagementService
    {
        private static Task<HttpResponseData<AdminUserDTO>> Result(int id) => Task.FromResult(new HttpResponseData<AdminUserDTO> { StatusCode = 200, Content = new() { Id=id, Username="test" } });
        public Task<HttpResponseData<PagedResult<AdminUserDTO>>> GetListAsync(AdminUserQueryDTO query) => Task.FromResult(new HttpResponseData<PagedResult<AdminUserDTO>> {StatusCode=200,Content=new()});
        public Task<HttpResponseData<List<AdminRoleDTO>>> GetRolesAsync() => Task.FromResult(new HttpResponseData<List<AdminRoleDTO>> {StatusCode=200,Content=new()});
        public Task<HttpResponseData<AdminUserDTO>> GetByIdAsync(int id) => Result(id);
        public Task<HttpResponseData<AdminUserDTO>> SetRolesAsync(int id,SetUserRolesRequestDTO r,int actorId) => Result(id);
        public Task<HttpResponseData<AdminUserDTO>> ResetPasswordAsync(int id,AdminResetPasswordRequestDTO r,int actorId) => Result(id);
        public Task<HttpResponseData<AdminUserDTO>> SetLockAsync(int id,SetUserLockRequestDTO r,int actorId) => Result(id);
    }
    public sealed class FixtureUsers : IUserRepository
    {
        public Dictionary<int,User> Items {get;} = new();
        public FixtureUsers()
        {
            var id=7; foreach(var name in new[]{"Admin","Doctor","Receptionist","Patient","Pharmacist"})
            { Items[id]=new User {Id=id,Username=name,IsActive=true,PasswordHash="test-only-hash",UserRoleUsers=new List<UserRole>{new(){RoleId=id,Role=new(){Id=id,Name=name}}}}; id++; }
        }
        public Task<User?> GetAdminDetailAsync(int id,bool tracking=false) => Task.FromResult(Items.GetValueOrDefault(id));
        public Task AcquireAccessLockAsync() => Task.CompletedTask;
        public Task<int> CountActiveAdminsAsync() => throw new NotSupportedException();
        public Task<(List<User> Items,int TotalItems)> GetAdminPageAsync(string? k,string? r,bool? a,int p,int s)=>throw new NotSupportedException();
        public Task<IQueryable<User>> GetAllAsync()=>throw new NotSupportedException();
        public Task<User?> GetByIdAsync(dynamic id)=>throw new NotSupportedException();
        public Task AddAsync(User u)=>throw new NotSupportedException();
        public Task UpdateAsync(User u)=>throw new NotSupportedException();
        public Task DeleteAsync(dynamic id)=>throw new NotSupportedException();
        public Task AddListItemsAsync(List<User> u)=>throw new NotSupportedException();
        public Task UpdateListItemsAsync(List<User> u)=>throw new NotSupportedException();
        public Task<User?> SingleOrDefault(Expression<Func<User,bool>> p)=>throw new NotSupportedException();
        public Task<IQueryable<User>> Where(Expression<Func<User,bool>> p)=>throw new NotSupportedException();
        public IQueryable<User> WhereSql(Expression<Func<User,bool>> p)=>throw new NotSupportedException();
    }
}
