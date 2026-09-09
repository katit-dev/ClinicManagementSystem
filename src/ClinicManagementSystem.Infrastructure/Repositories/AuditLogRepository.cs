using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IAuditLogRepository : IRepositoryBase<AuditLog>
{
}

public class AuditLogRepository : RepositoryBase<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(ClinicManagementDbContext context)
        : base(context)
    {
    }
}
