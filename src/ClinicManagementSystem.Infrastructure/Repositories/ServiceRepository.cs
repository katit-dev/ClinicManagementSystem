using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IServiceRepository : IRepositoryBase<Service>
{
}

public class ServiceRepository : RepositoryBase<Service>, IServiceRepository
{
    public ServiceRepository(ClinicManagementDbContext context)
        : base(context)
    {
    }
}
