using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IPatientRepository : IRepositoryBase<Patient>
{
}

public class PatientRepository : RepositoryBase<Patient>, IPatientRepository
{
    public PatientRepository(ClinicManagementDbContext context)
        : base(context)
    {
    }
}
