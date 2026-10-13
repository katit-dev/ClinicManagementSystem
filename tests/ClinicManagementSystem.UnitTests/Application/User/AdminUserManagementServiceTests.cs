using UserEntity = ClinicManagementSystem.Infrastructure.Models.User;
using System.Text.Json;
using ClinicManagementSystem.Application.DTOs.User;
using ClinicManagementSystem.Application.Helpers;
using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.Repositories;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ClinicManagementSystem.UnitTests.Application.AdminUsers;

public class AdminUserManagementServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRoleRepository> _roles = new();
    private readonly Mock<IUserRoleRepository> _assignments = new();
    private readonly Mock<IRefreshTokenRepository> _refresh = new();
    private readonly Mock<IPasswordResetTokenRepository> _otp = new();
    private readonly Mock<IAuditLogRepository> _audit = new();
    private readonly AdminUserManagementService _service;
    private readonly UserEntity _target = new() { Id = 5, Username = "vc22.local", FullName = "Test local", IsActive = true, PasswordHash = "old-hash" };
    private readonly List<Role> _catalog = new() { new() { Id = 1, Name = "Admin" }, new() { Id = 2, Name = "Receptionist" },
        new() { Id = 3, Name = "Doctor" }, new() { Id = 4, Name = "Patient" }, new() { Id = 5, Name = "Pharmacist" } };
    private static readonly DateTime Now = new(2026, 10, 13, 8, 0, 0, DateTimeKind.Utc);
    public AdminUserManagementServiceTests()
    {
        _uow.Setup(x => x.UserRepository).Returns(_users.Object); _uow.Setup(x => x.RoleRepository).Returns(_roles.Object);
        _uow.Setup(x => x.UserRoleRepository).Returns(_assignments.Object); _uow.Setup(x => x.RefreshTokenRepository).Returns(_refresh.Object);
        _uow.Setup(x => x.PasswordResetTokenRepository).Returns(_otp.Object); _uow.Setup(x => x.AuditLogRepository).Returns(_audit.Object);
        var actor = new UserEntity { Id = 7, Username = "admin", IsActive = true, UserRoleUsers = new List<UserRole> { new() { RoleId = 1, Role = _catalog[0] } } };
        _users.Setup(x => x.GetAdminDetailAsync(7, false)).ReturnsAsync(actor);
        _users.Setup(x => x.GetAdminDetailAsync(5, true)).ReturnsAsync(_target);
        _users.Setup(x => x.GetAdminDetailAsync(5, false)).ReturnsAsync(_target);
        _users.Setup(x => x.CountActiveAdminsAsync()).ReturnsAsync(2);
        _roles.Setup(x => x.GetAdminRolesAsync()).ReturnsAsync(_catalog);
        _service = new(_uow.Object, NullLogger<AdminUserManagementService>.Instance, new FixedClock());
    }
    private sealed class FixedClock : TimeProvider { public override DateTimeOffset GetUtcNow() => new(Now); }
    private void TargetAdmin() => _target.UserRoleUsers.Add(new() { RoleId = 1, Role = _catalog[0] });
    private static AdminResetPasswordRequestDTO Password() => new() { NewPassword = "VC22-Local-Only!", ConfirmPassword = "VC22-Local-Only!" };

    [Fact]
    public async Task SetRoles_ReplacesDesiredSetRevokesTokensAndAuditsAtomically()
    {
        AuditLog? audit = null; var sequence = new List<string>();
        _uow.Setup(x => x.BeginTransactionAsync()).Callback(() => sequence.Add("begin")).Returns(Task.CompletedTask);
        _users.Setup(x => x.AcquireAccessLockAsync()).Callback(() => sequence.Add("lock")).Returns(Task.CompletedTask);
        _assignments.Setup(x => x.SetRolesAsync(_target, It.IsAny<List<Role>>(), 7, Now)).Callback(() => sequence.Add("roles")).Returns(Task.CompletedTask);
        _refresh.Setup(x => x.RevokeAllAsync(5, Now)).Callback(() => sequence.Add("refresh")).Returns(Task.CompletedTask);
        _otp.Setup(x => x.RevokeAllAsync(5, Now)).Callback(() => sequence.Add("otp")).Returns(Task.CompletedTask);
        _audit.Setup(x => x.AddAsync(It.IsAny<AuditLog>())).Callback<AuditLog>(a => { audit = a; sequence.Add("audit"); }).Returns(Task.CompletedTask);
        _uow.Setup(x => x.SaveChangesAsync()).Callback(() => sequence.Add("save")).Returns(Task.CompletedTask);
        _uow.Setup(x => x.CommitTransactionAsync()).Callback(() => sequence.Add("commit")).Returns(Task.CompletedTask);
        var result = await _service.SetRolesAsync(5, new() { RoleIds = new[] { 2, 5 } }, 7);
        Assert.Equal(200, result.StatusCode); Assert.Equal(new[] { "Pharmacist", "Receptionist" }, result.Content!.Roles.Select(r => r.Name));
        Assert.Equal(Now, _target.UpdatedAt); Assert.Equal(7, audit!.UserId); Assert.Equal(5, audit.EntityId);
        Assert.Equal("SetUserRoles", audit.Action); Assert.Contains("AfterRoleIds", audit.Details!);
        Assert.Equal(new[] { "begin", "lock", "roles", "refresh", "otp", "audit", "save", "commit" }, sequence);
    }
    [Theory]
    [InlineData(null)] [InlineData(new int[] { 0 })] [InlineData(new int[] { 2, 2 })]
    public async Task InvalidRoleArraysDoNotStartTransaction(int[]? roleIds)
    {
        Assert.Equal(400, (await _service.SetRolesAsync(5, new() { RoleIds = roleIds }, 7)).StatusCode);
        _uow.Verify(x => x.BeginTransactionAsync(), Times.Never);
    }
    [Fact]
    public async Task UnknownRoleRollsBackWithoutRemovingExistingRoles()
    {
        Assert.Equal(400, (await _service.SetRolesAsync(5, new() { RoleIds = new[] { 999 } }, 7)).StatusCode);
        _assignments.Verify(x => x.SetRolesAsync(It.IsAny<UserEntity>(), It.IsAny<List<Role>>(), It.IsAny<int>(), It.IsAny<DateTime>()), Times.Never);
        _uow.Verify(x => x.RollbackTransactionAsync(), Times.Once);
    }
    [Theory]
    [InlineData(3)] [InlineData(4)]
    public async Task ClinicalRolesRequireAnExistingProfile(int roleId)
        => Assert.Equal(409, (await _service.SetRolesAsync(5, new() { RoleIds = new[] { roleId } }, 7)).StatusCode);
    [Fact]
    public async Task ClinicalRolesWithProfilesCanBeAssigned()
    {
        _target.Doctor = new() { Id = 10 }; _target.Patient = new() { Id = 20 };
        Assert.Equal(200, (await _service.SetRolesAsync(5, new() { RoleIds = new[] { 3, 4 } }, 7)).StatusCode);
    }
    [Fact]
    public async Task EmptyRolesRemovesAllRolesFromNonAdmin()
    {
        _target.UserRoleUsers.Add(new() { RoleId = 2, Role = _catalog[1] });
        var result = await _service.SetRolesAsync(5, new() { RoleIds = Array.Empty<int>() }, 7);
        Assert.Equal(200, result.StatusCode); Assert.Empty(result.Content!.Roles);
        _refresh.Verify(x => x.RevokeAllAsync(5, Now), Times.Once);
    }
    [Fact]
    public async Task SameRoleSetDoesNotInvalidateSessionsOrAddAudit()
    {
        _target.UserRoleUsers.Add(new() { RoleId = 2, Role = _catalog[1] });
        Assert.Equal(200, (await _service.SetRolesAsync(5, new() { RoleIds = new[] { 2 } }, 7)).StatusCode);
        _refresh.Verify(x => x.RevokeAllAsync(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Never);
        _audit.Verify(x => x.AddAsync(It.IsAny<AuditLog>()), Times.Never); Assert.Null(_target.UpdatedAt);
    }
    [Fact]
    public async Task SelfDemotionAndSelfLockAreRejected()
    {
        _target.Id = 7; TargetAdmin(); _users.Setup(x => x.GetAdminDetailAsync(7, true)).ReturnsAsync(_target);
        Assert.Equal(409, (await _service.SetRolesAsync(7, new() { RoleIds = new[] { 2 } }, 7)).StatusCode);
        Assert.Equal(409, (await _service.SetLockAsync(7, new() { IsActive = false, Reason = "Test" }, 7)).StatusCode);
    }
    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task LastActiveAdminCannotBeDemotedOrLocked(bool removeRole)
    {
        TargetAdmin(); _users.Setup(x => x.CountActiveAdminsAsync()).ReturnsAsync(1);
        var result = removeRole ? await _service.SetRolesAsync(5, new() { RoleIds = Array.Empty<int>() }, 7)
            : await _service.SetLockAsync(5, new() { IsActive = false, Reason = "Test" }, 7);
        Assert.Equal(409, result.StatusCode); Assert.True(_target.IsActive); _audit.Verify(x => x.AddAsync(It.IsAny<AuditLog>()), Times.Never);
    }
    [Fact]
    public async Task PasswordResetHashesAndNeverLeaksCredentialsToResponseOrAudit()
    {
        AuditLog? audit = null; _audit.Setup(x => x.AddAsync(It.IsAny<AuditLog>())).Callback<AuditLog>(a => audit = a).Returns(Task.CompletedTask);
        _target.FailedLoginCount = 4; _target.LockoutEnd = Now.AddHours(1);
        var request = Password(); var result = await _service.ResetPasswordAsync(5, request, 7);
        Assert.Equal(200, result.StatusCode); Assert.True(HelperFunction.VerifyPassword(request.NewPassword, _target.PasswordHash));
        Assert.Equal(0, _target.FailedLoginCount); Assert.Null(_target.LockoutEnd);
        var responseJson = JsonSerializer.Serialize(result);
        Assert.DoesNotContain(request.NewPassword, responseJson); Assert.DoesNotContain(_target.PasswordHash, responseJson);
        Assert.DoesNotContain(request.NewPassword, audit!.Details!); Assert.DoesNotContain(_target.PasswordHash, audit.Details!);
        _refresh.Verify(x => x.RevokeAllAsync(5, Now), Times.Once); _otp.Verify(x => x.RevokeAllAsync(5, Now), Times.Once);
    }
    [Fact]
    public async Task ResetLockedAccountDoesNotUnlockIt()
    {
        _target.IsActive = false; Assert.Equal(200, (await _service.ResetPasswordAsync(5, Password(), 7)).StatusCode); Assert.False(_target.IsActive);
    }
    [Theory]
    [InlineData("short", "short")] [InlineData("password-long", "mismatch")]
    public async Task InvalidPasswordsDoNotWrite(string password, string confirm)
    {
        Assert.Equal(400, (await _service.ResetPasswordAsync(5, new() { NewPassword = password, ConfirmPassword = confirm }, 7)).StatusCode);
        Assert.Equal("old-hash", _target.PasswordHash); _uow.Verify(x => x.BeginTransactionAsync(), Times.Never);
    }
    [Fact]
    public async Task MultibytePasswordCannotExceedBcrypt72ByteLimit()
    {
        var password = new string('ấ', 25);
        Assert.Equal(400, (await _service.ResetPasswordAsync(5, new() { NewPassword = password, ConfirmPassword = password }, 7)).StatusCode);
    }
    [Fact]
    public async Task LockAndUnlockInvalidateAllSessionsAndAuditReason()
    {
        var audits = new List<AuditLog>(); _audit.Setup(x => x.AddAsync(It.IsAny<AuditLog>())).Callback<AuditLog>(audits.Add).Returns(Task.CompletedTask);
        Assert.Equal(200, (await _service.SetLockAsync(5, new() { IsActive = false, Reason = "  Test local  " }, 7)).StatusCode); Assert.False(_target.IsActive);
        Assert.Equal(200, (await _service.SetLockAsync(5, new() { IsActive = true, Reason = "Mở lại" }, 7)).StatusCode); Assert.True(_target.IsActive);
        Assert.Equal(Now.AddTicks(1), _target.UpdatedAt); Assert.Contains("Test local", audits[0].Details!);
        Assert.Equal(new[] { "LockUser", "UnlockUser" }, audits.Select(a => a.Action));
        _refresh.Verify(x => x.RevokeAllAsync(5, It.IsAny<DateTime>()), Times.Exactly(2));
    }
    [Theory]
    [InlineData(null, "Test")] [InlineData(false, "   ")]
    public async Task LockRequiresExplicitStatusAndReason(bool? active, string reason)
    {
        Assert.Equal(400, (await _service.SetLockAsync(5, new() { IsActive = active, Reason = reason }, 7)).StatusCode);
        _uow.Verify(x => x.BeginTransactionAsync(), Times.Never);
    }
    [Fact]
    public async Task MissingTargetAndRevokedActorCannotMutate()
    {
        _users.Setup(x => x.GetAdminDetailAsync(5, true)).ReturnsAsync((UserEntity?)null);
        Assert.Equal(404, (await _service.SetLockAsync(5, new() { IsActive = false, Reason = "Test" }, 7)).StatusCode);
        _users.Setup(x => x.GetAdminDetailAsync(7, false)).ReturnsAsync(new UserEntity { Id = 7, IsActive = true });
        Assert.Equal(403, (await _service.SetRolesAsync(5, new() { RoleIds = new[] { 2 } }, 7)).StatusCode);
        _audit.Verify(x => x.AddAsync(It.IsAny<AuditLog>()), Times.Never);
    }
    [Fact]
    public async Task AuditFailureRollsBackAndDoesNotCommit()
    {
        _audit.Setup(x => x.AddAsync(It.IsAny<AuditLog>())).ThrowsAsync(new InvalidOperationException("test"));
        Assert.Equal(500, (await _service.SetLockAsync(5, new() { IsActive = false, Reason = "Test" }, 7)).StatusCode);
        _uow.Verify(x => x.RollbackTransactionAsync(), Times.Once); _uow.Verify(x => x.CommitTransactionAsync(), Times.Never);
    }
    [Fact]
    public async Task ListAndDetailDoNotExposePasswordHash()
    {
        _users.Setup(x => x.GetAdminPageAsync("test", "Receptionist", false, 2, 10)).ReturnsAsync((new List<UserEntity> { _target }, 11));
        var list = await _service.GetListAsync(new() { Keyword = " test ", Role = " Receptionist ", Active = false, Page = 2 });
        Assert.Equal(200, list.StatusCode); Assert.Equal(2, list.Content!.TotalPages);
        var detail = await _service.GetByIdAsync(5); Assert.DoesNotContain("PasswordHash", JsonSerializer.Serialize(detail));
        Assert.DoesNotContain("old-hash", JsonSerializer.Serialize(list));
    }
}
