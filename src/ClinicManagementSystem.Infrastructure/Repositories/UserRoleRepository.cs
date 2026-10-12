using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IUserRoleRepository : IRepositoryBase<UserRole>
{
    Task<List<int>> GetActiveReceptionistUserIdsAsync();
}

public class UserRoleRepository : RepositoryBase<UserRole>, IUserRoleRepository
{
    private readonly ClinicManagementDbContext _context;
    public UserRoleRepository(ClinicManagementDbContext context)
        : base(context)
    {
        _context = context;
    }

    public Task<List<int>> GetActiveReceptionistUserIdsAsync()
        => _context.UserRoles.AsNoTracking().Where(r => r.Role.Name == "Receptionist" && r.User.IsActive && !r.User.IsDeleted)
            .Select(r => r.UserId).Distinct().ToListAsync();
}
