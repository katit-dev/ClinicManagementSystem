using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IDoctorTimeOffRepository : IRepositoryBase<DoctorTimeOff>
{
}

public class DoctorTimeOffRepository : RepositoryBase<DoctorTimeOff>, IDoctorTimeOffRepository
{
    public DoctorTimeOffRepository(ClinicManagementDbContext context)
        : base(context)
    {
    }
}
