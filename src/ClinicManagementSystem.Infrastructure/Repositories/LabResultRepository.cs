using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface ILabResultRepository : IRepositoryBase<LabResult>
{
}

public class LabResultRepository : RepositoryBase<LabResult>, ILabResultRepository
{
    public LabResultRepository(ClinicManagementDbContext context)
        : base(context)
    {
    }
}
