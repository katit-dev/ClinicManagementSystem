using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IDoctorScheduleRepository : IRepositoryBase<DoctorSchedule>
{
}

public class DoctorScheduleRepository : RepositoryBase<DoctorSchedule>, IDoctorScheduleRepository
{
    public DoctorScheduleRepository(ClinicManagementDbContext context)
        : base(context)
    {
    }
}
