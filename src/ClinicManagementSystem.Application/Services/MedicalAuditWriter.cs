using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.UnitOfWork;

namespace ClinicManagementSystem.Application.Services;

public interface IMedicalAuditWriter
{
    Task WriteAsync(IReadOnlyCollection<AuditLog> entries);
}

// Resolved in a separate scope: audit persistence must not flush a clinical
// request's pending changes or participate in its failed transaction.
public sealed class MedicalAuditWriter(IUnitOfWork unitOfWork) : IMedicalAuditWriter
{
    public async Task WriteAsync(IReadOnlyCollection<AuditLog> entries)
    {
        if (entries.Count == 0) return;
        await unitOfWork.AuditLogRepository.AddListItemsAsync(entries.ToList());
        await unitOfWork.SaveChangesAsync();
    }
}
