using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface ISpecialtyRepository : IRepositoryBase<Specialty>
{
}

public class SpecialtyRepository : RepositoryBase<Specialty>, ISpecialtyRepository
{
    public SpecialtyRepository(ClinicManagementDbContext context)
        : base(context)
    {
    }
}
