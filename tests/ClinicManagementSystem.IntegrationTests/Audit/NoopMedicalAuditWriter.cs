using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.IntegrationTests.Audit;

// Existing authorization fixtures test JWT/MVC rather than audit persistence.
// Dedicated VC24 pipeline tests capture writes and failures separately.
public sealed class NoopMedicalAuditWriter : IMedicalAuditWriter
{
    public Task WriteAsync(IReadOnlyCollection<AuditLog> entries) => Task.CompletedTask;
}
