using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IMedicineStockTransactionRepository : IRepositoryBase<MedicineStockTransaction>
{
}

public class MedicineStockTransactionRepository : RepositoryBase<MedicineStockTransaction>, IMedicineStockTransactionRepository
{
    public MedicineStockTransactionRepository(ClinicManagementDbContext context)
        : base(context)
    {
    }
}
