using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Audit;
using ClinicManagementSystem.Infrastructure.Repositories;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Application.Services;

public interface IAdminAuditLogService
{
    Task<HttpResponseData<PagedResult<AuditLogDTO>>> GetListAsync(AuditLogQueryDTO query);
    Task<HttpResponseData<AuditLogDetailDTO>> GetByIdAsync(int id);
    Task<HttpResponseData<PagedResult<AuditUserOptionDTO>>> GetUsersAsync(AuditUserQueryDTO query);
    Task<HttpResponseData<PagedResult<AuditAccessDTO>>> GetAccessesAsync(AuditLogQueryDTO query);
    Task<HttpResponseData<AuditAccessDetailDTO>> GetAccessByIdAsync(int id);
}

public class AdminAuditLogService(IUnitOfWork unitOfWork, ILogger<AdminAuditLogService> logger) : IAdminAuditLogService
{
    public async Task<HttpResponseData<PagedResult<AuditAccessDTO>>> GetAccessesAsync(AuditLogQueryDTO query)
    {
        if (!Validate(query, out var message)) return Response<PagedResult<AuditAccessDTO>>(400, message);
        try
        {
            DateTime? fromUtc = query.From.HasValue ? DateTime.SpecifyKind(query.From.Value.ToDateTime(TimeOnly.MinValue).AddHours(-7), DateTimeKind.Utc) : null;
            DateTime? endUtc = query.To.HasValue ? DateTime.SpecifyKind(query.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue).AddHours(-7), DateTimeKind.Utc) : null;
            var (items, total) = await unitOfWork.AuditLogRepository.GetAuditAccessPageAsync(EntityAliases(query.Entity), query.UserId,
                query.EntityId, fromUtc, endUtc, query.Page, query.PageSize);
            return Response(200, "Tải lượt truy cập thành công.", Page(items.Select(MapAccess).ToList(), total, query.Page, query.PageSize));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to query audit access groups.");
            return Response<PagedResult<AuditAccessDTO>>(500, "Không thể tải lượt truy cập. Vui lòng thử lại.");
        }
    }

    public async Task<HttpResponseData<AuditAccessDetailDTO>> GetAccessByIdAsync(int id)
    {
        if (id <= 0) return Response<AuditAccessDetailDTO>(400, "Mã lượt truy cập không hợp lệ.");
        try
        {
            var anchor = await unitOfWork.AuditLogRepository.GetAuditDetailAsync(id);
            if (anchor == null) return Response<AuditAccessDetailDTO>(404, "Không tìm thấy lượt truy cập.");
            var entries = await unitOfWork.AuditLogRepository.GetAuditAccessEntriesAsync(id);
            if (entries.Count == 0) return Response<AuditAccessDetailDTO>(404, "Không tìm thấy lượt truy cập.");
            var entityIds = entries.Where(x => x.EntityId.HasValue).Select(x => x.EntityId!.Value).Distinct().ToList();
            var (requestId, metadata) = Metadata(anchor.Details);
            var row = Map(anchor);
            return Response(200, "Tải chi tiết lượt truy cập thành công.", new AuditAccessDetailDTO
            {
                Id = id, UserId = row.UserId, Username = row.Username, FullName = row.FullName, Action = row.Action,
                EntityName = row.EntityName, EntityId = entityIds.Count == 1 ? entityIds[0] : null,
                IpAddress = row.IpAddress, Succeeded = row.Succeeded, OccurredAtUtc = row.OccurredAtUtc,
                IsUtcTimestamp = row.IsUtcTimestamp || metadata != null, RequestId = requestId, Metadata = metadata,
                EntryCount = entries.Count, EntityCount = entityIds.Count, Entries = entries.Select(Map).ToList(), Details = anchor.Details
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read audit access {AuditLogId}.", id);
            return Response<AuditAccessDetailDTO>(500, "Không thể tải chi tiết lượt truy cập. Vui lòng thử lại.");
        }
    }

    private static AuditAccessDTO MapAccess(AuditAccessReadModel x) => new()
    {
        Id = x.Id, UserId = x.UserId, Username = x.Username, FullName = x.FullName, Action = x.Action,
        EntityName = x.EntityName, EntityId = x.EntityId, IpAddress = x.IpAddress, Succeeded = x.Succeeded,
        OccurredAtUtc = DateTime.SpecifyKind(x.OccurredAt, DateTimeKind.Utc), IsUtcTimestamp = x.IsUtcTimestamp,
        RequestId = x.RequestId, EntryCount = x.EntryCount, EntityCount = x.EntityCount
    };

    private static (string? RequestId, AuditRequestMetadataDTO? Metadata) Metadata(string? details)
    {
        if (string.IsNullOrWhiteSpace(details)) return (null, null);
        try
        {
            using var json = JsonDocument.Parse(details);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return (null, null);
            string? Text(string name) => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
            if (Text("source") != "VC24_MEDICAL_ACCESS") return (null, null);
            var status = root.TryGetProperty("statusCode", out var code) && code.ValueKind == JsonValueKind.Number
                && code.TryGetInt32(out var number) ? (int?)number : null;
            return (Text("requestId"), new AuditRequestMetadataDTO { Method = Text("method"), Endpoint = Text("endpoint"), StatusCode = status, Phase = Text("phase") });
        }
        catch (JsonException) { return (null, null); }
    }

    public async Task<HttpResponseData<PagedResult<AuditLogDTO>>> GetListAsync(AuditLogQueryDTO query)
    {
        if (!Validate(query, out var message)) return Response<PagedResult<AuditLogDTO>>(400, message);
        try
        {
            // audit_logs uses SYSUTCDATETIME; filter dates refer to the clinic's UTC+7 calendar.
            DateTime? fromUtc = query.From.HasValue ? DateTime.SpecifyKind(query.From.Value.ToDateTime(TimeOnly.MinValue).AddHours(-7), DateTimeKind.Utc) : null;
            DateTime? endUtc = query.To.HasValue ? DateTime.SpecifyKind(query.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue).AddHours(-7), DateTimeKind.Utc) : null;
            var (items, total) = await unitOfWork.AuditLogRepository.GetAuditPageAsync(EntityAliases(query.Entity), query.UserId,
                query.EntityId, fromUtc, endUtc, query.Page, query.PageSize);
            return Response(200, "Tải nhật ký thành công.", Page(items.Select(Map).ToList(), total, query.Page, query.PageSize));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to query audit logs.");
            return Response<PagedResult<AuditLogDTO>>(500, "Không thể tải nhật ký. Vui lòng thử lại.");
        }
    }

    public async Task<HttpResponseData<AuditLogDetailDTO>> GetByIdAsync(int id)
    {
        if (id <= 0) return Response<AuditLogDetailDTO>(400, "Mã nhật ký không hợp lệ.");
        try
        {
            var item = await unitOfWork.AuditLogRepository.GetAuditDetailAsync(id);
            if (item == null) return Response<AuditLogDetailDTO>(404, "Không tìm thấy nhật ký.");
            var row = Map(item);
            return Response(200, "Tải chi tiết nhật ký thành công.", new AuditLogDetailDTO
            {
                Id = row.Id, UserId = row.UserId, Username = row.Username, FullName = row.FullName, Action = row.Action,
                EntityName = row.EntityName, EntityId = row.EntityId, IpAddress = row.IpAddress, Succeeded = row.Succeeded,
                OccurredAtUtc = row.OccurredAtUtc, IsUtcTimestamp = row.IsUtcTimestamp, Details = item.Details
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read audit log {AuditLogId}.", id);
            return Response<AuditLogDetailDTO>(500, "Không thể tải chi tiết nhật ký. Vui lòng thử lại.");
        }
    }

    public async Task<HttpResponseData<PagedResult<AuditUserOptionDTO>>> GetUsersAsync(AuditUserQueryDTO query)
    {
        if (!Validate(query, out var message)) return Response<PagedResult<AuditUserOptionDTO>>(400, message);
        try
        {
            var (items, total) = await unitOfWork.AuditLogRepository.GetAuditUsersAsync(query.Keyword?.Trim(), query.Page, query.PageSize);
            return Response(200, "Tải người dùng trong nhật ký thành công.", Page(items.Select(x => new AuditUserOptionDTO
                { Id = x.Id, Username = x.Username, FullName = x.FullName }).ToList(), total, query.Page, query.PageSize));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to query audit user options.");
            return Response<PagedResult<AuditUserOptionDTO>>(500, "Không thể tải danh sách người dùng. Vui lòng thử lại.");
        }
    }

    private static AuditLogDTO Map(AuditLogReadModel x) => new()
    {
        Id = x.Id, UserId = x.UserId, Username = x.Username, FullName = x.FullName, Action = x.Action,
        EntityName = x.EntityName, EntityId = x.EntityId, IpAddress = x.IpAddress, Succeeded = x.Succeeded,
        OccurredAtUtc = DateTime.SpecifyKind(x.OccurredAt, DateTimeKind.Utc), IsUtcTimestamp = x.IsUtcTimestamp
    };

    private static bool Validate(object? query, out string message)
    {
        var errors = new List<ValidationResult>();
        if (query == null) { message = "Bộ lọc nhật ký không hợp lệ."; return false; }
        var valid = Validator.TryValidateObject(query, new ValidationContext(query), errors, true);
        message = string.Join(" ", errors.Select(x => x.ErrorMessage));
        return valid;
    }

    private static PagedResult<T> Page<T>(List<T> items, int total, int page, int size) => new()
    {
        Items = items, TotalItems = total, Page = page, PageSize = size,
        TotalPages = (int)(((long)total + size - 1) / size)
    };

    private static string[]? EntityAliases(string? entity)
    {
        var term = entity?.Trim();
        if (string.IsNullOrEmpty(term)) return null;
        string[][] aliases =
        [
            ["patients", "Patient"], ["medical_records", "MedicalRecord"], ["prescriptions", "Prescription"],
            ["attachments", "Attachment"], ["patient_allergies", "PatientAllergy"], ["patient_vitals", "PatientVital"],
            ["lab_results", "LabResult"], ["medical_record_services", "MedicalRecordService"], ["prescription_items", "PrescriptionItem"],
            ["users", "User"], ["services", "Service"], ["specialties", "Specialty"], ["appointments", "Appointment"],
            ["doctor_schedules", "DoctorSchedule"], ["doctor_time_off", "DoctorTimeOff"], ["invoices", "Invoice"],
            ["medicines", "Medicine"], ["medicine_stock_transactions", "MedicineStockTransaction"]
        ];
        return aliases.FirstOrDefault(group => group.Contains(term, StringComparer.OrdinalIgnoreCase)) ?? [term];
    }

    private static HttpResponseData<T> Response<T>(int code, string message, T? content = default)
        => new() { StatusCode = code, Message = message, Content = content };
}
