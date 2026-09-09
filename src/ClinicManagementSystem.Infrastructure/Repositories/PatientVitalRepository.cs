using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IPatientVitalRepository : IRepositoryBase<PatientVital>
{
}

public class PatientVitalRepository : RepositoryBase<PatientVital>, IPatientVitalRepository
{
    public PatientVitalRepository(ClinicManagementDbContext context)
        : base(context)
    {
    }
}
