using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IRoleRepository : IRepositoryBase<Role>
{
    Task<List<Role>> GetAdminRolesAsync();
}

public class RoleRepository : RepositoryBase<Role>, IRoleRepository
{
    private readonly ClinicManagementDbContext _context;
    public RoleRepository(ClinicManagementDbContext context)
        : base(context)
    {
        _context = context;
    }
    public Task<List<Role>> GetAdminRolesAsync() => _context.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync();
}
