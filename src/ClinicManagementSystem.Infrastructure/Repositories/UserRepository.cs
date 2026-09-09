
using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IUserRepository : IRepositoryBase<User>
{
    // Đã có các method từ IRepositoryBase<User>.
    // Khai báo thêm các method riêng của User tại đây.
}

public class UserRepository
    : RepositoryBase<User>, IUserRepository
{
    public UserRepository(ClinicManagementDbContext context)
        : base(context)
    {
    }

    // Cài đặt thêm các method riêng của User tại đây.
}