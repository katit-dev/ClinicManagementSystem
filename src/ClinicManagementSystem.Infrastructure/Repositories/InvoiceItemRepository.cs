using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IInvoiceItemRepository : IRepositoryBase<InvoiceItem>
{
}

public class InvoiceItemRepository : RepositoryBase<InvoiceItem>, IInvoiceItemRepository
{
    public InvoiceItemRepository(ClinicManagementDbContext context)
        : base(context)
    {
    }
}
