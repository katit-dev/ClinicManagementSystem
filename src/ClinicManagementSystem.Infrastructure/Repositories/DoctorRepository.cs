using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface IDoctorRepository : IRepositoryBase<Doctor>
{
    Task<List<Doctor>> GetAdminOptionsAsync();
    Task<Doctor?> GetActiveByUserIdAsync(int userId);
}

public class DoctorRepository : RepositoryBase<Doctor>, IDoctorRepository
{
    private readonly ClinicManagementDbContext _context;
    public DoctorRepository(ClinicManagementDbContext context)
        : base(context)
    {
        _context = context;
    }

    public Task<Doctor?> GetActiveByUserIdAsync(int userId)
        => _context.Doctors.AsNoTracking().FirstOrDefaultAsync(d => d.UserId == userId && d.IsActive);

    public Task<List<Doctor>> GetAdminOptionsAsync()
        => _context.Doctors.AsNoTracking().OrderBy(d => d.FullName).ThenBy(d => d.Id).ToListAsync();
}
