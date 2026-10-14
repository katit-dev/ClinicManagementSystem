using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Application.DTOs.Audit;

public class AuditLogQueryDTO : IValidatableObject
{
    [StringLength(128, ErrorMessage = "Đối tượng không được vượt quá 128 ký tự.")]
    public string? Entity { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Người dùng không hợp lệ.")]
    public int? UserId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Mã đối tượng không hợp lệ.")]
    public int? EntityId { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Trang phải lớn hơn 0.")]
    public int Page { get; set; } = 1;
    [Range(1, 100, ErrorMessage = "Số dòng mỗi trang phải từ 1 đến 100.")]
    public int PageSize { get; set; } = 20;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (From.HasValue != To.HasValue)
            yield return new("Vui lòng nhập cả ngày bắt đầu và ngày kết thúc.", [nameof(From), nameof(To)]);
        if (!From.HasValue || !To.HasValue) yield break;
        if (From.Value == DateOnly.MinValue || To.Value == DateOnly.MaxValue)
            yield return new("Khoảng ngày nằm ngoài giới hạn hỗ trợ.", [nameof(From), nameof(To)]);
        if (To < From)
            yield return new("Ngày kết thúc phải từ ngày bắt đầu trở đi.", [nameof(To)]);
        else if (To.Value.DayNumber - From.Value.DayNumber > 365)
            yield return new("Khoảng tra cứu tối đa 366 ngày, tính cả hai ngày đầu và cuối.", [nameof(To)]);
    }
}

public class AuditUserQueryDTO
{
    [StringLength(100, ErrorMessage = "Từ khóa không được vượt quá 100 ký tự.")]
    public string? Keyword { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Trang phải lớn hơn 0.")]
    public int Page { get; set; } = 1;
    [Range(1, 100, ErrorMessage = "Số dòng mỗi trang phải từ 1 đến 100.")]
    public int PageSize { get; set; } = 50;
}

public class AuditLogDTO
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string? Username { get; set; }
    public string? FullName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string? IpAddress { get; set; }
    public bool Succeeded { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public bool IsUtcTimestamp { get; set; }
}

public class AuditLogDetailDTO : AuditLogDTO
{
    public string? Details { get; set; }
}

public class AuditUserOptionDTO
{
    public int Id { get; set; }
    public string? Username { get; set; }
    public string? FullName { get; set; }
}

// Id is an existing audit row identifying this access, not a new stored entity.
public class AuditAccessDTO : AuditLogDTO
{
    public string? RequestId { get; set; }
    public int EntryCount { get; set; } = 1;
    public int EntityCount { get; set; }
}

public class AuditAccessDetailDTO : AuditAccessDTO
{
    public List<AuditLogDTO> Entries { get; set; } = new();
    public AuditRequestMetadataDTO? Metadata { get; set; }
    public string? Details { get; set; }
}

public class AuditRequestMetadataDTO
{
    public string? Method { get; set; }
    public string? Endpoint { get; set; }
    public int? StatusCode { get; set; }
    public string? Phase { get; set; }
}
