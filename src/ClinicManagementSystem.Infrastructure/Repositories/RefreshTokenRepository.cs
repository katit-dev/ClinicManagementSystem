using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IRefreshTokenRepository : IRepositoryBase<RefreshToken>
{
    Task RevokeAllAsync(int userId, DateTime now);
}

public class RefreshTokenRepository : RepositoryBase<RefreshToken>, IRefreshTokenRepository
{
    private readonly ClinicManagementDbContext _context;
    public RefreshTokenRepository(ClinicManagementDbContext context)
        : base(context)
    {
        _context = context;
    }
    public async Task RevokeAllAsync(int userId, DateTime now)
    {
        var tokens = await _context.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null).ToListAsync();
        foreach (var token in tokens) token.RevokedAt = now;
    }
}
