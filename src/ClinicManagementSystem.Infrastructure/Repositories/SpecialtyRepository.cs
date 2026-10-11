using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public interface ISpecialtyRepository : IRepositoryBase<Specialty>
{
    Task<(List<Specialty> Items, int TotalItems)> GetAdminPageAsync(
        string? keyword, bool? active, int page, int pageSize);
    Task<bool> CodeExistsAsync(string code, int? excludedId);
    Task<bool> HasActiveDependenciesAsync(int id);
}

public class SpecialtyRepository : RepositoryBase<Specialty>, ISpecialtyRepository
{
    private readonly ClinicManagementDbContext _context;

    public SpecialtyRepository(ClinicManagementDbContext context) : base(context)
        => _context = context;

    public async Task<(List<Specialty> Items, int TotalItems)> GetAdminPageAsync(
        string? keyword, bool? active, int page, int pageSize)
    {
        var query = _context.Specialties.AsNoTracking().AsQueryable();
        if (active.HasValue)
            query = query.Where(s => s.IsActive == active.Value);
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(s => s.Name.Contains(keyword) || (s.Code != null && s.Code.Contains(keyword)));

        var total = await query.CountAsync();
        // Use a wide intermediate to avoid overflowing on invalidly large page numbers.
        var offset = (long)(page - 1) * pageSize;
        if (offset >= total)
            return (new List<Specialty>(), total);

        var items = await query.OrderBy(s => s.Name).ThenBy(s => s.Id)
            .Skip((int)offset).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public Task<bool> CodeExistsAsync(string code, int? excludedId)
        => _context.Specialties.AnyAsync(s => s.Code != null && s.Code.ToUpper() == code
            && (!excludedId.HasValue || s.Id != excludedId.Value));

    public async Task<bool> HasActiveDependenciesAsync(int id)
        => await _context.Doctors.AnyAsync(d => d.SpecialtyId == id && d.IsActive)
            || await _context.Services.AnyAsync(s => s.SpecialtyId == id && s.IsActive);
}
