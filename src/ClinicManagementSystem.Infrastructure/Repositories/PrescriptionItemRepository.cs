using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IPrescriptionItemRepository : IRepositoryBase<PrescriptionItem>
{
}

public class PrescriptionItemRepository : RepositoryBase<PrescriptionItem>, IPrescriptionItemRepository
{
    public PrescriptionItemRepository(ClinicManagementDbContext context)
        : base(context)
    {
    }
}
