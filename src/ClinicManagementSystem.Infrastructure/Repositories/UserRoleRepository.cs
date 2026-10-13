using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IUserRoleRepository : IRepositoryBase<UserRole>
{
    Task<List<int>> GetActiveReceptionistUserIdsAsync();
    Task SetRolesAsync(User user, List<Role> roles, int actorId, DateTime now);
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

    public Task SetRolesAsync(User user, List<Role> roles, int actorId, DateTime now)
    {
        var desiredIds = roles.Select(r => r.Id).ToHashSet();
        var removed = user.UserRoleUsers.Where(r => !desiredIds.Contains(r.RoleId)).ToList();
        _context.UserRoles.RemoveRange(removed);
        foreach (var item in removed) user.UserRoleUsers.Remove(item);
        foreach (var role in roles.Where(r => !user.UserRoleUsers.Any(ur => ur.RoleId == r.Id)))
        {
            var assignment = new UserRole { UserId = user.Id, RoleId = role.Id, AssignedBy = actorId, AssignedAt = now };
            // Role is a read-only lookup. Do not attach it as a newly inserted entity.
            _context.UserRoles.Add(assignment);
            user.UserRoleUsers.Add(assignment);
        }
        return Task.CompletedTask;
    }
}
