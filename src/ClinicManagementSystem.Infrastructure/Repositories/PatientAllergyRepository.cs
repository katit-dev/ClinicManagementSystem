using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IPatientAllergyRepository : IRepositoryBase<PatientAllergy>
{
}

public class PatientAllergyRepository : RepositoryBase<PatientAllergy>, IPatientAllergyRepository
{
    public PatientAllergyRepository(ClinicManagementDbContext context)
        : base(context)
    {
    }
}
