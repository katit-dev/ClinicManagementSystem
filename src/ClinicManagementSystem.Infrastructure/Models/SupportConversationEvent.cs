namespace ClinicManagementSystem.Infrastructure.Models;

public class SupportConversationEvent
{
    public long Id { get; set; }
    public long ConversationId { get; set; }
    public long EventSequence { get; set; }
    public byte EventType { get; set; }
    public int ActorUserId { get; set; }
    public int? FromAssigneeUserId { get; set; }
    public int? ToAssigneeUserId { get; set; }
    public long MessageSequenceAtEvent { get; set; }
    public string? Reason { get; set; }
    public DateTime OccurredAt { get; set; }
}
