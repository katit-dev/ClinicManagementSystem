using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IPaymentRepository : IRepositoryBase<Payment>
{
}

public class PaymentRepository : RepositoryBase<Payment>, IPaymentRepository
{
    public PaymentRepository(ClinicManagementDbContext context)
        : base(context)
    {
    }
}
