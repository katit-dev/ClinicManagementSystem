using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IInsurancePolicyRepository : IRepositoryBase<InsurancePolicy>
{
}

public class InsurancePolicyRepository : RepositoryBase<InsurancePolicy>, IInsurancePolicyRepository
{
    public InsurancePolicyRepository(ClinicManagementDbContext context)
        : base(context)
    {
    }
}
