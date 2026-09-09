using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IAppointmentRepository : IRepositoryBase<Appointment>
{
}

public class AppointmentRepository : RepositoryBase<Appointment>, IAppointmentRepository
{
    public AppointmentRepository(ClinicManagementDbContext context)
        : base(context)
    {
    }
}
