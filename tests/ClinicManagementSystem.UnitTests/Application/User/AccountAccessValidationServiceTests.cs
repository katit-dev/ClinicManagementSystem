using UserEntity = ClinicManagementSystem.Infrastructure.Models.User;
using System.Security.Claims;
using ClinicManagementSystem.Application.Helpers;
using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ClinicManagementSystem.UnitTests.Application.AdminUsers;

public class AccountAccessValidationServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly UserEntity _user = new() { Id = 5, Username = "test", IsActive = true, PasswordHash = "test-only-hash",
        UserRoleUsers = new List<UserRole> { new() { RoleId = 1, Role = new() { Id = 1, Name = "Admin" } } } };
    private readonly AccountAccessValidationService _validator;
    public AccountAccessValidationServiceTests()
    {
        _users.Setup(x => x.GetAdminDetailAsync(5, false)).ReturnsAsync(_user);
        _validator = new(_users.Object, NullLogger<AccountAccessValidationService>.Instance);
    }
    private ClaimsPrincipal Token(string? stamp = null, string role = "Admin") => new(new ClaimsIdentity(new[]
    { new Claim(ClaimTypes.NameIdentifier, "5"), new Claim(ClaimTypes.Role, role),
        new Claim(AccountAccessStamp.ClaimType, stamp ?? AccountAccessStamp.Create(_user, new[] { role })) }, "test"));
    [Fact] public async Task CurrentAccountAndStampAreAccepted() => Assert.True(await _validator.IsValidAsync(Token()));
    [Theory] [InlineData("lock")] [InlineData("deleted")] [InlineData("password")] [InlineData("roles")] [InlineData("stamp")] [InlineData("lockout")]
    public async Task ExistingTokenIsRejectedAfterSecurityChange(string change)
    {
        var token = Token();
        switch(change)
        {
            case "lock": _user.IsActive = false; break;
            case "deleted": _user.IsDeleted = true; break;
            case "password": _user.PasswordHash = "new-hash"; break;
            case "roles": _user.UserRoleUsers.Clear(); break;
            case "stamp": _user.UpdatedAt = DateTime.UtcNow; break;
            case "lockout": _user.LockoutEnd = DateTime.UtcNow.AddMinutes(1); break;
        }
        Assert.False(await _validator.IsValidAsync(token));
    }
    [Fact]
    public async Task RestoringRolesOrUnlockingDoesNotReviveOldJwt()
    {
        var token = Token(); _user.IsActive = false; _user.UpdatedAt = DateTime.UtcNow;
        _user.IsActive = true; Assert.False(await _validator.IsValidAsync(token)); Assert.True(await _validator.IsValidAsync(Token()));
    }
    [Fact]
    public async Task LegacyTokenWithNoStampIsRejected()
        => Assert.False(await _validator.IsValidAsync(new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "5"), new Claim(ClaimTypes.Role, "Admin") }, "test"))));
    [Theory] [InlineData("invalid")] [InlineData("000000000000000000000000000000000000000000000000000000000000000Z")]
    public async Task MalformedStampIsRejected(string stamp) => Assert.False(await _validator.IsValidAsync(Token(stamp)));
    [Fact] public async Task JwtRoleCannotGrantMoreThanDatabaseRoles() => Assert.False(await _validator.IsValidAsync(Token(role:"Doctor")));
    [Fact]
    public async Task RepositoryFailureFailsClosed()
    { _users.Setup(x => x.GetAdminDetailAsync(5, false)).ThrowsAsync(new InvalidOperationException("test")); Assert.False(await _validator.IsValidAsync(Token())); }
}
