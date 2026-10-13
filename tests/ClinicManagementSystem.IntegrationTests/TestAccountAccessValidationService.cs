using System.Security.Claims;
using ClinicManagementSystem.Application.Services;

namespace ClinicManagementSystem.IntegrationTests;

// Existing route/role fixtures use synthetic JWTs without a database account.
// Account-state validation itself is covered by VC22's real-validator HTTP/unit tests.
public sealed class TestAccountAccessValidationService : IAccountAccessValidationService
{
    public Task<bool> IsValidAsync(ClaimsPrincipal principal) => Task.FromResult(true);
}
