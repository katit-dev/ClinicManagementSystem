using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IAppointmentStatusHistoryRepository : IRepositoryBase<AppointmentStatusHistory>
{
}

public class AppointmentStatusHistoryRepository : RepositoryBase<AppointmentStatusHistory>, IAppointmentStatusHistoryRepository
{
    public AppointmentStatusHistoryRepository(ClinicManagementDbContext context)
        : base(context)
    {
    }
}
