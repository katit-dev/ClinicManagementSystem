using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Service;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Application.Services;

public interface IAdminServiceCatalogService
{
    Task<HttpResponseData<PagedResult<AdminServiceDTO>>> GetListAsync(AdminServiceQueryDTO query);
    Task<HttpResponseData<AdminServiceDTO>> GetByIdAsync(int id);
    Task<HttpResponseData<AdminServiceDTO>> CreateAsync(SaveServiceRequestDTO request, int actorId);
    Task<HttpResponseData<AdminServiceDTO>> UpdateAsync(int id, SaveServiceRequestDTO request, int actorId);
    Task<HttpResponseData<AdminServiceDTO>> DeactivateAsync(int id, int actorId);
}

public class AdminServiceCatalogService(IUnitOfWork unitOfWork, ILogger<AdminServiceCatalogService> logger, TimeProvider? clock = null)
    : IAdminServiceCatalogService
{
    private DateTime UtcNow => (clock ?? TimeProvider.System).GetUtcNow().UtcDateTime;

    public async Task<HttpResponseData<PagedResult<AdminServiceDTO>>> GetListAsync(AdminServiceQueryDTO query)
    {
        var error = Validate(query);
        if (error != null) return Response<PagedResult<AdminServiceDTO>>(400, error);
        try
        {
            var (items, total) = await unitOfWork.ServiceRepository.GetAdminPageAsync(query.Keyword?.Trim(), query.Active, query.SpecialtyId, query.Page, query.PageSize);
            return Response(200, "Tải danh mục dịch vụ thành công.", new PagedResult<AdminServiceDTO>
            {
                Items = items.Select(Map).ToList(), Page = query.Page, PageSize = query.PageSize,
                TotalItems = total, TotalPages = (int)Math.Ceiling(total / (double)query.PageSize)
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to list services for Admin.");
            return Response<PagedResult<AdminServiceDTO>>(500, "Không thể tải danh mục dịch vụ.");
        }
    }

    public async Task<HttpResponseData<AdminServiceDTO>> GetByIdAsync(int id)
    {
        if (id <= 0) return Response<AdminServiceDTO>(400, "Id dịch vụ không hợp lệ.");
        try
        {
            var service = await unitOfWork.ServiceRepository.GetAdminDetailAsync(id);
            return service == null ? Response<AdminServiceDTO>(404, "Không tìm thấy dịch vụ.")
                : Response(200, "Tải dịch vụ thành công.", Map(service));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get service {ServiceId} for Admin.", id);
            return Response<AdminServiceDTO>(500, "Không thể tải dịch vụ.");
        }
    }

    public Task<HttpResponseData<AdminServiceDTO>> CreateAsync(SaveServiceRequestDTO request, int actorId) => SaveAsync(null, request, actorId);
    public Task<HttpResponseData<AdminServiceDTO>> UpdateAsync(int id, SaveServiceRequestDTO request, int actorId) => SaveAsync(id, request, actorId);

    private async Task<HttpResponseData<AdminServiceDTO>> SaveAsync(int? id, SaveServiceRequestDTO request, int actorId)
    {
        if (actorId <= 0) return Response<AdminServiceDTO>(401, "Người dùng không hợp lệ.");
        if (id <= 0 || request == null) return Response<AdminServiceDTO>(400, "Yêu cầu dịch vụ không hợp lệ.");
        var normalized = new SaveServiceRequestDTO
        {
            Code = (request.Code ?? string.Empty).Trim().ToUpperInvariant(), Name = (request.Name ?? string.Empty).Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            SpecialtyId = request.SpecialtyId, Price = request.Price, DurationMinutes = request.DurationMinutes
        };
        var error = Validate(normalized);
        if (error != null) return Response<AdminServiceDTO>(400, error);
        var started = false;
        try
        {
            await unitOfWork.BeginTransactionAsync(); started = true;
            await unitOfWork.ServiceRepository.AcquireAdminLockAsync();
            var service = id.HasValue ? await unitOfWork.ServiceRepository.GetAdminDetailAsync(id.Value, true)
                : new Service { IsActive = true, CreatedAt = UtcNow };
            if (service == null) return await Reject(404, "Không tìm thấy dịch vụ.");
            if (await unitOfWork.ServiceRepository.CodeExistsAsync(normalized.Code, id))
                return await Reject(409, "Mã dịch vụ đã được sử dụng, kể cả dịch vụ đã ngừng sử dụng.");
            Specialty? specialty = null;
            if (normalized.SpecialtyId.HasValue)
            {
                specialty = await unitOfWork.SpecialtyRepository.GetByIdAsync(normalized.SpecialtyId.Value);
                if (specialty == null) return await Reject(400, "Chuyên khoa không tồn tại.");
                if (service.IsActive && !specialty.IsActive)
                    return await Reject(409, "Không thể gán dịch vụ đang dùng cho chuyên khoa đã ngừng hoạt động.");
            }
            var before = id.HasValue ? Map(service) : null;
            service.Code = normalized.Code; service.Name = normalized.Name; service.Description = normalized.Description;
            service.SpecialtyId = normalized.SpecialtyId; service.Specialty = specialty;
            service.Price = normalized.Price!.Value; service.DurationMinutes = normalized.DurationMinutes;
            if (id.HasValue) service.UpdatedAt = UtcNow;
            // Preserve IsActive and CreatedAt when editing. No invoice/order snapshots are rewritten.
            if (id.HasValue) await unitOfWork.ServiceRepository.UpdateAsync(service);
            else await unitOfWork.ServiceRepository.AddAsync(service);
            await PersistAuditAsync(service, actorId, id.HasValue ? "UpdateService" : "CreateService", before);
            await unitOfWork.CommitTransactionAsync(); started = false;
            return Response(id.HasValue ? 200 : 201, id.HasValue ? "Cập nhật dịch vụ thành công." : "Tạo dịch vụ thành công.", Map(service));
        }
        catch (Exception ex)
        {
            if (started) await RollbackSafely();
            logger.LogError(ex, "Failed to save service {ServiceId}.", id);
            return MutationError(ex, "Không thể lưu dịch vụ. Vui lòng thử lại.");
        }

        async Task<HttpResponseData<AdminServiceDTO>> Reject(int status, string message)
        { await unitOfWork.RollbackTransactionAsync(); started = false; return Response<AdminServiceDTO>(status, message); }
    }

    public async Task<HttpResponseData<AdminServiceDTO>> DeactivateAsync(int id, int actorId)
    {
        if (actorId <= 0) return Response<AdminServiceDTO>(401, "Người dùng không hợp lệ.");
        if (id <= 0) return Response<AdminServiceDTO>(400, "Id dịch vụ không hợp lệ.");
        var started = false;
        try
        {
            await unitOfWork.BeginTransactionAsync(); started = true;
            await unitOfWork.ServiceRepository.AcquireAdminLockAsync();
            var service = await unitOfWork.ServiceRepository.GetAdminDetailAsync(id, true);
            if (service == null)
            { await unitOfWork.RollbackTransactionAsync(); started = false; return Response<AdminServiceDTO>(404, "Không tìm thấy dịch vụ."); }
            if (!service.IsActive)
            {
                await unitOfWork.CommitTransactionAsync(); started = false;
                return Response(200, "Dịch vụ đã ngừng sử dụng.", Map(service));
            }
            var before = Map(service);
            service.IsActive = false; service.UpdatedAt = UtcNow;
            await unitOfWork.ServiceRepository.UpdateAsync(service);
            await PersistAuditAsync(service, actorId, "DeactivateService", before);
            await unitOfWork.CommitTransactionAsync(); started = false;
            return Response(200, "Đã ngừng sử dụng dịch vụ.", Map(service));
        }
        catch (Exception ex)
        {
            if (started) await RollbackSafely();
            logger.LogError(ex, "Failed to deactivate service {ServiceId}.", id);
            return MutationError(ex, "Không thể ngừng sử dụng dịch vụ. Vui lòng thử lại.");
        }
    }

    private async Task PersistAuditAsync(Service service, int actorId, string action, AdminServiceDTO? before)
    {
        // First save obtains the generated id; both saves and audit commit in the same transaction.
        await unitOfWork.SaveChangesAsync();
        await unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            UserId = actorId, Action = action, EntityName = "Service", EntityId = service.Id,
            Details = JsonSerializer.Serialize(new { Before = before, After = Map(service) }),
            Succeeded = true, OccurredAt = UtcNow
        });
        await unitOfWork.SaveChangesAsync();
    }

    private async Task RollbackSafely()
    {
        try { await unitOfWork.RollbackTransactionAsync(); }
        catch (Exception ex) { logger.LogError(ex, "Failed to roll back service catalog mutation."); }
    }

    private static HttpResponseData<AdminServiceDTO> MutationError(Exception ex, string fallback) => ex switch
    {
        DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } } => Response<AdminServiceDTO>(409, "Mã dịch vụ đã được sử dụng."),
        DbUpdateConcurrencyException => Response<AdminServiceDTO>(409, "Dịch vụ đã thay đổi. Tải lại danh sách trước khi thao tác."),
        DbUpdateException { InnerException: SqlException { Number: 547 } } => Response<AdminServiceDTO>(409, "Dữ liệu liên quan đã thay đổi. Kiểm tra lại chuyên khoa và dịch vụ."),
        _ => Response<AdminServiceDTO>(500, fallback)
    };

    private static string? Validate(object? model)
    {
        if (model == null) return "Yêu cầu không hợp lệ.";
        var results = new List<ValidationResult>();
        return Validator.TryValidateObject(model, new ValidationContext(model), results, true) ? null
            : string.Join(" ", results.Select(r => r.ErrorMessage));
    }
    private static AdminServiceDTO Map(Service service) => new()
    {
        Id = service.Id, Code = service.Code, Name = service.Name, Description = service.Description,
        SpecialtyId = service.SpecialtyId, SpecialtyName = service.Specialty?.Name, SpecialtyIsActive = service.Specialty?.IsActive,
        Price = service.Price, DurationMinutes = service.DurationMinutes, IsActive = service.IsActive,
        CreatedAt = service.CreatedAt, UpdatedAt = service.UpdatedAt
    };
    private static HttpResponseData<T> Response<T>(int status, string message, T? content = default)
        => new() { StatusCode = status, Message = message, Content = content };
}
