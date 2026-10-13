using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Application.Helpers;

public static class AccountAccessStamp
{
    public const string ClaimType = "clinic_access_stamp";
    public static string Create(User user, IEnumerable<string> roles)
    {
        // An opaque fingerprint, not the password/hash itself. Existing schema remains unchanged.
        var input = JsonSerializer.Serialize(new { user.Id, user.PasswordHash, UpdatedTicks = user.UpdatedAt?.Ticks.ToString(CultureInfo.InvariantCulture),
            Roles = roles.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray() });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)));
    }
}
