using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IPasswordResetTokenRepository
    : IRepositoryBase<PasswordResetToken>
{
}

public class PasswordResetTokenRepository
    : RepositoryBase<PasswordResetToken>,
      IPasswordResetTokenRepository
{
    public PasswordResetTokenRepository(
        ClinicManagementDbContext context)
        : base(context)
    {
    }
}