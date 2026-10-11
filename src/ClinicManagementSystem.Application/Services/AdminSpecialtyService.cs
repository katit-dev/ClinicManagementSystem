using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Specialty;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Application.Services;

public interface IAdminSpecialtyService
{
    Task<HttpResponseData<PagedResult<AdminSpecialtyDTO>>> GetListAsync(AdminSpecialtyQueryDTO query);
    Task<HttpResponseData<AdminSpecialtyDTO>> CreateAsync(SaveSpecialtyRequestDTO request, int actorId);
    Task<HttpResponseData<AdminSpecialtyDTO>> UpdateAsync(int id, SaveSpecialtyRequestDTO request, int actorId);
    Task<HttpResponseData<AdminSpecialtyDTO>> SetStatusAsync(int id, bool isActive, int actorId);
}

public class AdminSpecialtyService : IAdminSpecialtyService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdminSpecialtyService> _logger;

    public AdminSpecialtyService(IUnitOfWork unitOfWork, ILogger<AdminSpecialtyService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<HttpResponseData<PagedResult<AdminSpecialtyDTO>>> GetListAsync(AdminSpecialtyQueryDTO query)
    {
        var validation = Validate(query);
        if (validation != null)
            return Response<PagedResult<AdminSpecialtyDTO>>(400, validation);

        try
        {
            var (items, total) = await _unitOfWork.SpecialtyRepository.GetAdminPageAsync(
                query.Keyword?.Trim(), query.Active, query.Page, query.PageSize);
            return Response(200, "Tải danh sách chuyên khoa thành công.", new PagedResult<AdminSpecialtyDTO>
            {
                Items = items.Select(Map).ToList(),
                Page = query.Page,
                PageSize = query.PageSize,
                TotalItems = total,
                TotalPages = (int)Math.Ceiling(total / (double)query.PageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list specialties for Admin.");
            return Response<PagedResult<AdminSpecialtyDTO>>(500, "Không thể tải danh sách chuyên khoa.");
        }
    }

    public Task<HttpResponseData<AdminSpecialtyDTO>> CreateAsync(SaveSpecialtyRequestDTO request, int actorId)
        => SaveAsync(null, request, actorId);

    public Task<HttpResponseData<AdminSpecialtyDTO>> UpdateAsync(int id, SaveSpecialtyRequestDTO request, int actorId)
        => SaveAsync(id, request, actorId);

    private async Task<HttpResponseData<AdminSpecialtyDTO>> SaveAsync(
        int? id, SaveSpecialtyRequestDTO request, int actorId)
    {
        var normalized = new SaveSpecialtyRequestDTO
        {
            Code = (request.Code ?? string.Empty).Trim().ToUpperInvariant(),
            Name = (request.Name ?? string.Empty).Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()
        };
        var validation = Validate(normalized);
        if (validation != null)
            return Response<AdminSpecialtyDTO>(400, validation);

        try
        {
            var specialty = id.HasValue
                ? await _unitOfWork.SpecialtyRepository.GetByIdAsync(id.Value)
                : new Specialty { IsActive = true, CreatedAt = DateTime.UtcNow };

            if (specialty == null)
                return Response<AdminSpecialtyDTO>(404, "Không tìm thấy chuyên khoa.");

            if (await _unitOfWork.SpecialtyRepository.CodeExistsAsync(normalized.Code, id))
                return Response<AdminSpecialtyDTO>(409, "Mã chuyên khoa đã được sử dụng.");

            var before = id.HasValue ? Map(specialty) : null;
            specialty.Code = normalized.Code;
            specialty.Name = normalized.Name;
            specialty.Description = normalized.Description;
            if (id.HasValue)
                specialty.UpdatedAt = DateTime.UtcNow;

            await PersistAsync(specialty, !id.HasValue, actorId,
                id.HasValue ? "UpdateSpecialty" : "CreateSpecialty", before);

            return Response(id.HasValue ? 200 : 201,
                id.HasValue ? "Cập nhật chuyên khoa thành công." : "Tạo chuyên khoa thành công.",
                Map(specialty));
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && sql.Number is 2601 or 2627)
        {
            return Response<AdminSpecialtyDTO>(409, "Mã chuyên khoa đã được sử dụng.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save specialty {SpecialtyId}.", id);
            return Response<AdminSpecialtyDTO>(500, "Không thể lưu chuyên khoa. Vui lòng thử lại.");
        }
    }

    public async Task<HttpResponseData<AdminSpecialtyDTO>> SetStatusAsync(int id, bool isActive, int actorId)
    {
        try
        {
            var specialty = await _unitOfWork.SpecialtyRepository.GetByIdAsync(id);
            if (specialty == null)
                return Response<AdminSpecialtyDTO>(404, "Không tìm thấy chuyên khoa.");

            if (specialty.IsActive == isActive)
                return Response(200, "Chuyên khoa đã ở trạng thái yêu cầu.", Map(specialty));

            if (!isActive && await _unitOfWork.SpecialtyRepository.HasActiveDependenciesAsync(id))
                return Response<AdminSpecialtyDTO>(409,
                    "Chuyên khoa còn bác sĩ hoặc dịch vụ đang hoạt động. Hãy chuyển hoặc ngừng hoạt động các mục liên quan trước.");

            var before = Map(specialty);
            specialty.IsActive = isActive;
            specialty.UpdatedAt = DateTime.UtcNow;
            await PersistAsync(specialty, false, actorId, isActive ? "ActivateSpecialty" : "DeactivateSpecialty", before);

            return Response(200, isActive ? "Đã bật hoạt động chuyên khoa." : "Đã ngừng hoạt động chuyên khoa.", Map(specialty));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change specialty status {SpecialtyId}.", id);
            return Response<AdminSpecialtyDTO>(500, "Không thể đổi trạng thái chuyên khoa.");
        }
    }

    private async Task PersistAsync(Specialty specialty, bool isNew, int actorId, string action, AdminSpecialtyDTO? before)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            if (isNew)
                await _unitOfWork.SpecialtyRepository.AddAsync(specialty);
            else
                await _unitOfWork.SpecialtyRepository.UpdateAsync(specialty);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
            {
                UserId = actorId,
                Action = action,
                EntityName = "Specialty",
                EntityId = specialty.Id,
                Details = JsonSerializer.Serialize(new { Before = before, After = Map(specialty) }),
                Succeeded = true,
                OccurredAt = DateTime.UtcNow
            });
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    private static string? Validate(object model)
    {
        var results = new List<ValidationResult>();
        return Validator.TryValidateObject(model, new ValidationContext(model), results, true)
            ? null : string.Join(" ", results.Select(r => r.ErrorMessage));
    }

    private static AdminSpecialtyDTO Map(Specialty specialty) => new()
    {
        Id = specialty.Id,
        Code = specialty.Code,
        Name = specialty.Name,
        Description = specialty.Description,
        IsActive = specialty.IsActive,
        CreatedAt = specialty.CreatedAt,
        UpdatedAt = specialty.UpdatedAt
    };

    private static HttpResponseData<T> Response<T>(int status, string message, T? content = default)
        => new() { StatusCode = status, Message = message, Content = content };
}
