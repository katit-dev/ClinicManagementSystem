using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IRoleRepository : IRepositoryBase<Role>
{
}

public class RoleRepository : RepositoryBase<Role>, IRoleRepository
{
    public RoleRepository(ClinicManagementDbContext context)
        : base(context)
    {
    }
}
