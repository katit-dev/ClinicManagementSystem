using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IMedicineRepository : IRepositoryBase<Medicine>
{
}

public class MedicineRepository : RepositoryBase<Medicine>, IMedicineRepository
{
    public MedicineRepository(ClinicManagementDbContext context)
        : base(context)
    {
    }
}
