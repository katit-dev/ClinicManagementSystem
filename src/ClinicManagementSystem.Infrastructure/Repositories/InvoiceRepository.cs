using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IInvoiceRepository : IRepositoryBase<Invoice>
{
}

public class InvoiceRepository : RepositoryBase<Invoice>, IInvoiceRepository
{
    public InvoiceRepository(ClinicManagementDbContext context)
        : base(context)
    {
    }
}
