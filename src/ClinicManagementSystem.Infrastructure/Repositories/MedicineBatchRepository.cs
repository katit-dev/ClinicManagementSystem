using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IMedicineBatchRepository : IRepositoryBase<MedicineBatch>
{
}

public class MedicineBatchRepository : RepositoryBase<MedicineBatch>, IMedicineBatchRepository
{
    public MedicineBatchRepository(ClinicManagementDbContext context)
        : base(context)
    {
    }
}
