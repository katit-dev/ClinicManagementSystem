using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface INotificationRepository : IRepositoryBase<Notification>
{
}

public class NotificationRepository : RepositoryBase<Notification>, INotificationRepository
{
    public NotificationRepository(ClinicManagementDbContext context)
        : base(context)
    {
    }
}
