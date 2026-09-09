using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IMedicalRecordRepository : IRepositoryBase<MedicalRecord>
{
}

public class MedicalRecordRepository : RepositoryBase<MedicalRecord>, IMedicalRecordRepository
{
    public MedicalRecordRepository(ClinicManagementDbContext context)
        : base(context)
    {
    }
}
