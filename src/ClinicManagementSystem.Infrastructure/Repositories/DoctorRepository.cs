using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IDoctorRepository : IRepositoryBase<Doctor>
{
}

public class DoctorRepository : RepositoryBase<Doctor>, IDoctorRepository
{
    public DoctorRepository(ClinicManagementDbContext context)
        : base(context)
    {
    }
}
