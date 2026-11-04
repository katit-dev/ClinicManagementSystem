using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Application.DTOs.Support;

public enum SupportSide : byte { Patient, Receptionist, System }
public enum SupportStatus : byte { Waiting, InProgress, Closed }
public enum SupportEventType : byte { Created, Claimed, Released, Transferred, Closed }

public class SupportMessageRequest
{
    [Required, StringLength(2000)] public string Content { get; set; } = "";
    public Guid ClientMessageId { get; set; }
}
public sealed class SupportCreateRequest : SupportMessageRequest
{
    [StringLength(150)] public string? Subject { get; set; }
}
public sealed class SupportSubjectRequest
{
    [StringLength(150)] public string? Subject { get; set; }
    [Required, StringLength(150)] public string ExpectedSubject { get; set; } = "";
}
public static class SupportTopics
{
    public const string Default = "Hỗ trợ chung";
    public const string Other = "Khác";
    public static IReadOnlyList<string> Presets { get; } = Array.AsReadOnly(new[] {
        Default, "Đặt lịch khám", "Đổi/hủy lịch hẹn", "Dịch vụ & chi phí",
        "Thủ tục khám", "Thanh toán & hóa đơn", "Hỗ trợ xem hồ sơ/kết quả", "Tài khoản & sử dụng website"
    });
}
public sealed class SupportActionRequest
{
    public int? ReceptionistUserId { get; set; }
    [StringLength(500)] public string? Reason { get; set; }
    public string? ExpectedVersion { get; set; }
}
public sealed class SupportReadRequest
{
    [Range(0, long.MaxValue)] public long Sequence { get; set; }
    // A delayed read request from an old assignment must never advance a new assignee's cursor.
    public string? ExpectedVersion { get; set; }
}
public sealed class SupportListQuery
{
    public string Scope { get; set; } = "waiting";
    [Range(0,2)] public byte? Status { get; set; }
    public int? AssignedUserId { get; set; }
    [StringLength(150)] public string? Search { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    [Range(1, 1000000)] public int Page { get; set; } = 1;
    [Range(1,100)] public int PageSize { get; set; } = 20;
}
public sealed class SupportCursorQuery : IValidatableObject
{
    [Range(0,long.MaxValue)] public long? BeforeSequence { get; set; }
    [Range(0,long.MaxValue)] public long? AfterSequence { get; set; }
    [Range(1,100)] public int PageSize { get; set; } = 30;
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (BeforeSequence.HasValue && AfterSequence.HasValue)
            yield return new ValidationResult("Chỉ chọn một hướng tải tin.");
    }
}
public sealed record SupportPage<T>(List<T> Items, int TotalItems, int Page, int PageSize);
public sealed record SupportCursorPage<T>(List<T> Items, bool HasMore, long? NextCursor);
public record SupportPatientConversation(long Id, string Subject, byte Status, DateTime CreatedAt,
    DateTime UpdatedAt, DateTime? LastMessageAt, long LastMessageSequence, long LastReadSequence, int UnreadCount);
public sealed record SupportStaffConversation(long Id, string Subject, byte Status, DateTime CreatedAt,
    DateTime UpdatedAt, DateTime? LastMessageAt, long LastMessageSequence, long LastReadSequence, int UnreadCount,
    int PatientId, string PatientName, string PatientCode, int? AssignedUserId, string? AssignedName, string Version);
public sealed record SupportPatientMessage(long Id, long Sequence, byte SenderSide, byte MessageKind,
    string SenderLabel, string Content, DateTime SentAt, Guid ClientMessageId);
public sealed record SupportStaffMessage(long Id, long Sequence, byte SenderSide, byte MessageKind,
    string SenderLabel, string Content, DateTime SentAt, Guid ClientMessageId, int? SenderUserId);
public sealed record SupportEventDto(long Id, long Sequence, byte EventType, string ActorName,
    string? FromAssignee, string? ToAssignee, string? Reason, long MessageSequenceAtEvent, DateTime OccurredAt);
public sealed record SupportReceptionistDto(int Id, string Name, int AssignedCount);
public sealed record SupportCreatedDto(SupportPatientConversation Conversation, List<SupportPatientMessage> Messages);
public sealed record SupportSettingsDto(bool Enabled, string SupportHours, string Hotline, int MaxMessageLength);
public sealed class SupportChatOptions
{
    public bool Enabled { get; set; }
    public string SupportHours { get; set; } = "Giờ hành chính hằng ngày, gồm Thứ Bảy và Chủ Nhật";
    public string Hotline { get; set; } = "0389479884";
    public int MaxMessageLength { get; set; } = 2000;
    public int ReconciliationIntervalSeconds { get; set; } = 30;
    public int MessagesPerMinute { get; set; } = 30;
    public int CreatesPerHour { get; set; } = 10;
}
public interface ISupportChatNotifier
{
    Task ChangedAsync(long conversationId, int patientUserId, int? previousAssignee, int? currentAssignee);
}
