using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IUserRoleRepository : IRepositoryBase<UserRole>
{
}

public class UserRoleRepository : RepositoryBase<UserRole>, IUserRoleRepository
{
    public UserRoleRepository(ClinicManagementDbContext context)
        : base(context)
    {
    }
}
