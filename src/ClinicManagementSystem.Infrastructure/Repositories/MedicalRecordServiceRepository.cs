using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IMedicalRecordServiceRepository : IRepositoryBase<MedicalRecordService>
{
}

public class MedicalRecordServiceRepository : RepositoryBase<MedicalRecordService>, IMedicalRecordServiceRepository
{
    public MedicalRecordServiceRepository(ClinicManagementDbContext context)
        : base(context)
    {
    }
}
