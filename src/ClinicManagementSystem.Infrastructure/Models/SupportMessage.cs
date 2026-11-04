namespace ClinicManagementSystem.Infrastructure.Models;

public class SupportMessage
{
    public long Id { get; set; }
    public long ConversationId { get; set; }
    public long MessageSequence { get; set; }
    public int? SenderUserId { get; set; }
    public byte SenderSide { get; set; }
    public string SenderNameSnapshot { get; set; } = "";
    public byte MessageKind { get; set; }
    public string Content { get; set; } = "";
    public Guid ClientMessageId { get; set; }
    public DateTime SentAt { get; set; }
}
