using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using ClinicManagementSystem.Application.Constants;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.User;
using ClinicManagementSystem.Application.Helpers;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Application.Services;

public interface IAdminUserManagementService
{
    Task<HttpResponseData<PagedResult<AdminUserDTO>>> GetListAsync(AdminUserQueryDTO query);
    Task<HttpResponseData<AdminUserDTO>> GetByIdAsync(int id);
    Task<HttpResponseData<List<AdminRoleDTO>>> GetRolesAsync();
    Task<HttpResponseData<AdminUserDTO>> SetRolesAsync(int id, SetUserRolesRequestDTO request, int actorId);
    Task<HttpResponseData<AdminUserDTO>> ResetPasswordAsync(int id, AdminResetPasswordRequestDTO request, int actorId);
    Task<HttpResponseData<AdminUserDTO>> SetLockAsync(int id, SetUserLockRequestDTO request, int actorId);
}

public class AdminUserManagementService(IUnitOfWork uow, ILogger<AdminUserManagementService> logger, TimeProvider? clock = null)
    : IAdminUserManagementService
{
    private DateTime UtcNow => (clock ?? TimeProvider.System).GetUtcNow().UtcDateTime;

    public async Task<HttpResponseData<PagedResult<AdminUserDTO>>> GetListAsync(AdminUserQueryDTO query)
    {
        var error = Validate(query);
        if (error != null) return Response<PagedResult<AdminUserDTO>>(400, error);
        try
        {
            var (items, total) = await uow.UserRepository.GetAdminPageAsync(query.Keyword?.Trim(), query.Role?.Trim(), query.Active, query.Page, query.PageSize);
            return Response(200, "Tải danh sách tài khoản thành công.", new PagedResult<AdminUserDTO>
            {
                Items = items.Select(u => Map(u)).ToList(), Page = query.Page, PageSize = query.PageSize,
                TotalItems = total, TotalPages = (int)Math.Ceiling(total / (double)query.PageSize)
            });
        }
        catch (Exception ex) { logger.LogError(ex, "Failed to list Admin users."); return Response<PagedResult<AdminUserDTO>>(500, "Không thể tải danh sách tài khoản."); }
    }

    public async Task<HttpResponseData<AdminUserDTO>> GetByIdAsync(int id)
    {
        if (id <= 0) return Response<AdminUserDTO>(400, "Id tài khoản không hợp lệ.");
        try
        {
            var user = await uow.UserRepository.GetAdminDetailAsync(id);
            return user == null ? Response<AdminUserDTO>(404, "Không tìm thấy tài khoản.") : Response(200, "Tải tài khoản thành công.", Map(user));
        }
        catch (Exception ex) { logger.LogError(ex, "Failed to get Admin user {UserId}.", id); return Response<AdminUserDTO>(500, "Không thể tải tài khoản."); }
    }

    public async Task<HttpResponseData<List<AdminRoleDTO>>> GetRolesAsync()
    {
        try { return Response(200, "Tải vai trò thành công.", (await uow.RoleRepository.GetAdminRolesAsync()).Select(MapRole).ToList()); }
        catch (Exception ex) { logger.LogError(ex, "Failed to list roles."); return Response<List<AdminRoleDTO>>(500, "Không thể tải vai trò."); }
    }

    // roleIds is the complete desired set, including an empty set when removing all roles.
    public Task<HttpResponseData<AdminUserDTO>> SetRolesAsync(int id, SetUserRolesRequestDTO request, int actorId)
        => Mutate(id, actorId, request, async user =>
        {
            var roles = (await uow.RoleRepository.GetAdminRolesAsync()).Where(r => request.RoleIds!.Contains(r.Id)).ToList();
            if (roles.Count != request.RoleIds!.Length) return Response<AdminUserDTO>(400, "Có vai trò không tồn tại.");
            var keepsAdmin = roles.Any(r => r.Name == UserRoleConstant.Admin);
            if (id == actorId && !keepsAdmin) return Response<AdminUserDTO>(409, "Không được tự gỡ vai trò Admin của mình.");
            if (user.IsActive && IsAdmin(user) && !keepsAdmin && await uow.UserRepository.CountActiveAdminsAsync() <= 1)
                return Response<AdminUserDTO>(409, "Phải giữ ít nhất một tài khoản Admin đang hoạt động.");
            if (roles.Any(r => r.Name == UserRoleConstant.Doctor) && user.Doctor == null)
                return Response<AdminUserDTO>(409, "Tài khoản chưa có hồ sơ bác sĩ. Tạo hồ sơ bác sĩ trước khi gán vai trò Doctor.");
            if (roles.Any(r => r.Name == UserRoleConstant.Patient) && user.Patient == null)
                return Response<AdminUserDTO>(409, "Tài khoản chưa có hồ sơ bệnh nhân. Tạo hồ sơ bệnh nhân trước khi gán vai trò Patient.");
            var before = user.UserRoleUsers.Select(r => r.RoleId).Order().ToArray();
            var after = request.RoleIds.Order().ToArray();
            if (before.SequenceEqual(after)) return Response(200, "Vai trò không thay đổi.", Map(user));
            var now = AdvanceAccessStamp(user);
            await uow.UserRoleRepository.SetRolesAsync(user, roles, actorId, now);
            await RevokeSessions(user.Id, now);
            await Audit(user.Id, actorId, "SetUserRoles", new { BeforeRoleIds = before, AfterRoleIds = after }, now);
            return Response(200, "Đã cập nhật vai trò. Tài khoản cần đăng nhập lại.", Map(user, roles));
        });

    public Task<HttpResponseData<AdminUserDTO>> ResetPasswordAsync(int id, AdminResetPasswordRequestDTO request, int actorId)
        => Mutate(id, actorId, request, async user =>
        {
            // Only a BCrypt hash is persisted. Never serialize the password or hash in response/audit.
            user.PasswordHash = HelperFunction.HashPassword(request.NewPassword);
            var now = AdvanceAccessStamp(user);
            user.FailedLoginCount = 0; user.LockoutEnd = null;
            await RevokeSessions(user.Id, now);
            await Audit(user.Id, actorId, "AdminResetPassword", new { PasswordReset = true }, now);
            return Response(200, "Đã reset mật khẩu và thu hồi phiên đăng nhập.", Map(user));
        });

    public Task<HttpResponseData<AdminUserDTO>> SetLockAsync(int id, SetUserLockRequestDTO request, int actorId)
        => Mutate(id, actorId, request, async user =>
        {
            var active = request.IsActive!.Value;
            if (id == actorId && !active) return Response<AdminUserDTO>(409, "Không được tự khóa tài khoản của mình.");
            if (!active && user.IsActive && IsAdmin(user) && await uow.UserRepository.CountActiveAdminsAsync() <= 1)
                return Response<AdminUserDTO>(409, "Phải giữ ít nhất một tài khoản Admin đang hoạt động.");
            if (user.IsActive == active) return Response(200, "Trạng thái tài khoản không thay đổi.", Map(user));
            var before = user.IsActive; user.IsActive = active;
            var now = AdvanceAccessStamp(user);
            if (active) { user.FailedLoginCount = 0; user.LockoutEnd = null; }
            await RevokeSessions(user.Id, now);
            await Audit(user.Id, actorId, active ? "UnlockUser" : "LockUser", new { BeforeActive = before, AfterActive = active, Reason = request.Reason.Trim() }, now);
            return Response(200, active ? "Đã mở khóa tài khoản. Tài khoản cần đăng nhập lại." : "Đã khóa tài khoản và thu hồi phiên đăng nhập.", Map(user));
        });

    private async Task<HttpResponseData<AdminUserDTO>> Mutate(int id, int actorId, object? request, Func<User, Task<HttpResponseData<AdminUserDTO>>> change)
    {
        if (actorId <= 0) return Response<AdminUserDTO>(401, "Người dùng không hợp lệ.");
        if (id <= 0) return Response<AdminUserDTO>(400, "Id tài khoản không hợp lệ.");
        var error = Validate(request);
        if (error != null) return Response<AdminUserDTO>(400, error);
        var started = false;
        try
        {
            await uow.BeginTransactionAsync(); started = true;
            await uow.UserRepository.AcquireAccessLockAsync();
            // Re-check the actor under the same lock; stale Admin tokens cannot perform mutations.
            var actor = await uow.UserRepository.GetAdminDetailAsync(actorId);
            if (actor is not { IsActive: true, IsDeleted: false } || !IsAdmin(actor))
            { await uow.RollbackTransactionAsync(); started = false; return Response<AdminUserDTO>(403, "Bạn không có quyền quản lý tài khoản."); }
            var user = await uow.UserRepository.GetAdminDetailAsync(id, true);
            var result = user == null ? Response<AdminUserDTO>(404, "Không tìm thấy tài khoản.") : await change(user);
            if (result.StatusCode >= 400) await uow.RollbackTransactionAsync();
            else { await uow.SaveChangesAsync(); await uow.CommitTransactionAsync(); }
            started = false;
            return result;
        }
        catch (Exception ex)
        {
            if (started) try { await uow.RollbackTransactionAsync(); } catch (Exception rollback) { logger.LogError(rollback, "Failed to roll back user management."); }
            logger.LogError(ex, "Failed to manage user {UserId} by actor {ActorId}.", id, actorId);
            return ex is DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 or 547 } }
                ? Response<AdminUserDTO>(409, "Dữ liệu tài khoản hoặc vai trò đã thay đổi. Vui lòng tải lại.")
                : Response<AdminUserDTO>(500, "Không thể cập nhật tài khoản. Vui lòng thử lại.");
        }
    }

    private DateTime AdvanceAccessStamp(User user)
    {
        var now = UtcNow;
        if (user.UpdatedAt >= now) now = user.UpdatedAt.Value.AddTicks(1);
        user.UpdatedAt = now;
        return now;
    }
    private async Task RevokeSessions(int id, DateTime now)
    { await uow.RefreshTokenRepository.RevokeAllAsync(id, now); await uow.PasswordResetTokenRepository.RevokeAllAsync(id, now); }
    private Task Audit(int id, int actorId, string action, object details, DateTime now)
        => uow.AuditLogRepository.AddAsync(new AuditLog { UserId = actorId, EntityName = "User", EntityId = id, Action = action,
            Details = JsonSerializer.Serialize(details), OccurredAt = now, Succeeded = true });
    private static bool IsAdmin(User user) => user.UserRoleUsers.Any(r => r.Role.Name == UserRoleConstant.Admin);
    private static string? Validate(object? model)
    {
        if (model == null) return "Yêu cầu không hợp lệ.";
        var errors = new List<ValidationResult>();
        return Validator.TryValidateObject(model, new ValidationContext(model), errors, true) ? null : string.Join(" ", errors.Select(e => e.ErrorMessage));
    }
    private static AdminRoleDTO MapRole(Role role) => new() { Id = role.Id, Name = role.Name, Description = role.Description };
    private static AdminUserDTO Map(User user, List<Role>? roles = null) => new()
    {
        Id = user.Id, Username = user.Username, FullName = user.FullName, Email = user.Email, Phone = user.Phone,
        IsActive = user.IsActive, LastLoginAt = user.LastLoginAt,
        Roles = (roles ?? user.UserRoleUsers.Select(r => r.Role).ToList()).OrderBy(r => r.Name).Select(MapRole).ToList()
    };
    private static HttpResponseData<T> Response<T>(int status, string message, T? content = default) => new() { StatusCode = status, Message = message, Content = content };
}
