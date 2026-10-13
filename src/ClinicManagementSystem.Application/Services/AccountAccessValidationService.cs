using System.Security.Claims;
using System.Security.Cryptography;
using ClinicManagementSystem.Application.Helpers;
using ClinicManagementSystem.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Application.Services;

public interface IAccountAccessValidationService
{
    Task<bool> IsValidAsync(ClaimsPrincipal principal);
}

public class AccountAccessValidationService(IUserRepository users, ILogger<AccountAccessValidationService> logger) : IAccountAccessValidationService
{
    public async Task<bool> IsValidAsync(ClaimsPrincipal principal)
    {
        if (!int.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) || id <= 0) return false;
        var stamp = principal.FindFirst(AccountAccessStamp.ClaimType)?.Value;
        if (stamp == null || stamp.Length != 64) return false; // Legacy sessions must log in once after rollout.
        try
        {
            var user = await users.GetAdminDetailAsync(id);
            if (user is not { IsActive: true, IsDeleted: false } || user.LockoutEnd > DateTime.UtcNow) return false;
            var roles = user.UserRoleUsers.Select(r => r.Role.Name).Distinct(StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
            if (!roles.SetEquals(principal.FindAll(ClaimTypes.Role).Select(c => c.Value))) return false;
            var expected = Convert.FromHexString(AccountAccessStamp.Create(user, roles));
            return CryptographicOperations.FixedTimeEquals(expected, Convert.FromHexString(stamp));
        }
        catch (FormatException) { return false; }
        catch (Exception ex) { logger.LogError(ex, "Failed to validate account access for User {UserId}.", id); return false; }
    }
}
