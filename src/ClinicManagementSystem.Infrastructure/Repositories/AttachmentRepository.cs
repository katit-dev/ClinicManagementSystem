using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IAttachmentRepository : IRepositoryBase<Attachment>
{
}

public class AttachmentRepository : RepositoryBase<Attachment>, IAttachmentRepository
{
    public AttachmentRepository(ClinicManagementDbContext context)
        : base(context)
    {
    }
}
