namespace ClinicManagementSystem.Infrastructure.Models;

public class SupportConversation
{
    public long Id { get; set; }
    public int PatientId { get; set; }
    public int PatientUserId { get; set; }
    public string Subject { get; set; } = "";
    public byte Status { get; set; }
    public int? AssignedReceptionistUserId { get; set; }
    public long LastMessageSequence { get; set; }
    public long LastEventSequence { get; set; }
    public long PatientLastReadSequence { get; set; }
    public long AssignedLastReadSequence { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int? ClosedByUserId { get; set; }
    public byte? CloseReason { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
