using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IRefreshTokenRepository : IRepositoryBase<RefreshToken>
{
}

public class RefreshTokenRepository : RepositoryBase<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(ClinicManagementDbContext context)
        : base(context)
    {
    }
}
