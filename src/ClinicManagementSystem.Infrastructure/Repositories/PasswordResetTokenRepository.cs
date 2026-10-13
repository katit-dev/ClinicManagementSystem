using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IPasswordResetTokenRepository
    : IRepositoryBase<PasswordResetToken>
{
    Task RevokeAllAsync(int userId, DateTime now);
}

public class PasswordResetTokenRepository
    : RepositoryBase<PasswordResetToken>,
      IPasswordResetTokenRepository
{
    private readonly ClinicManagementDbContext _context;
    public PasswordResetTokenRepository(
        ClinicManagementDbContext context)
        : base(context)
    {
        _context = context;
    }
    public async Task RevokeAllAsync(int userId, DateTime now)
    {
        var tokens = await _context.PasswordResetTokens.Where(t => t.UserId == userId && t.RevokedAt == null && t.UsedAt == null).ToListAsync();
        foreach (var token in tokens) token.RevokedAt = now;
    }
}
