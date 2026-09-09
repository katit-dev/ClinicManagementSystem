using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IPrescriptionRepository : IRepositoryBase<Prescription>
{
}

public class PrescriptionRepository : RepositoryBase<Prescription>, IPrescriptionRepository
{
    public PrescriptionRepository(ClinicManagementDbContext context)
        : base(context)
    {
    }
}
