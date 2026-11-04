using ClinicManagementSystem.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
namespace ClinicManagementSystem.Infrastructure.Data;
public partial class ClinicManagementDbContext {
public virtual DbSet<SupportConversation> SupportConversations { get; set; }
public virtual DbSet<SupportMessage> SupportMessages { get; set; }
public virtual DbSet<SupportConversationEvent> SupportConversationEvents { get; set; }
partial void OnModelCreatingPartial(ModelBuilder modelBuilder) {
modelBuilder.Entity<SupportConversation>(e => {
e.ToTable("support_conversations", "support"); e.HasKey(x => x.Id);
e.Property(x => x.Id).HasColumnName("id");
e.Property(x => x.PatientId).HasColumnName("patient_id");
e.Property(x => x.PatientUserId).HasColumnName("patient_user_id");
e.Property(x => x.Subject).HasColumnName("subject").HasMaxLength(150);
e.Property(x => x.Status).HasColumnName("status");
e.Property(x => x.AssignedReceptionistUserId).HasColumnName("assigned_receptionist_user_id");
e.Property(x => x.LastMessageSequence).HasColumnName("last_message_sequence");
e.Property(x => x.LastEventSequence).HasColumnName("last_event_sequence");
e.Property(x => x.PatientLastReadSequence).HasColumnName("patient_last_read_sequence");
e.Property(x => x.AssignedLastReadSequence).HasColumnName("assigned_last_read_sequence");
e.Property(x => x.CreatedAt).HasColumnName("created_at");
e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
e.Property(x => x.LastMessageAt).HasColumnName("last_message_at");
e.Property(x => x.AssignedAt).HasColumnName("assigned_at");
e.Property(x => x.ClosedAt).HasColumnName("closed_at");
e.Property(x => x.ClosedByUserId).HasColumnName("closed_by_user_id");
e.Property(x => x.CloseReason).HasColumnName("close_reason");
e.Property(x => x.RowVersion).HasColumnName("row_version").IsRowVersion();
e.HasIndex(x => x.PatientId).IsUnique().HasFilter("[status] < 2");
e.HasIndex(x => new { x.PatientUserId, x.CreatedAt, x.Id });
e.HasIndex(x => new { x.Status, x.CreatedAt, x.Id });
e.HasIndex(x => new { x.AssignedReceptionistUserId, x.Status, x.Id });
e.HasOne<Patient>().WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
e.HasOne<User>().WithMany().HasForeignKey(x => x.PatientUserId).OnDelete(DeleteBehavior.Restrict);
e.HasOne<User>().WithMany().HasForeignKey(x => x.AssignedReceptionistUserId).OnDelete(DeleteBehavior.Restrict);
e.HasOne<User>().WithMany().HasForeignKey(x => x.ClosedByUserId).OnDelete(DeleteBehavior.Restrict);
});
modelBuilder.Entity<SupportMessage>(e => {
e.ToTable("support_messages", "support"); e.HasKey(x => x.Id);
e.Property(x => x.Id).HasColumnName("id");
e.Property(x => x.ConversationId).HasColumnName("conversation_id");
e.Property(x => x.MessageSequence).HasColumnName("message_sequence");
e.Property(x => x.SenderUserId).HasColumnName("sender_user_id");
e.Property(x => x.SenderSide).HasColumnName("sender_side");
e.Property(x => x.SenderNameSnapshot).HasColumnName("sender_name_snapshot").HasMaxLength(150);
e.Property(x => x.MessageKind).HasColumnName("message_kind");
e.Property(x => x.Content).HasColumnName("content").HasMaxLength(2000);
e.Property(x => x.ClientMessageId).HasColumnName("client_message_id");
e.Property(x => x.SentAt).HasColumnName("sent_at");
e.HasIndex(x => new { x.ConversationId, x.MessageSequence }).IsUnique();
e.HasIndex(x => new { x.SenderUserId, x.ClientMessageId }).IsUnique().HasFilter("[sender_user_id] IS NOT NULL");
e.HasIndex(x => x.ConversationId).IsUnique().HasFilter("[message_kind] = 1");
e.HasOne<SupportConversation>().WithMany().HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Restrict);
e.HasOne<User>().WithMany().HasForeignKey(x => x.SenderUserId).OnDelete(DeleteBehavior.Restrict);
});
modelBuilder.Entity<SupportConversationEvent>(e => {
e.ToTable("support_conversation_events", "support"); e.HasKey(x => x.Id);
e.Property(x => x.Id).HasColumnName("id");
e.Property(x => x.ConversationId).HasColumnName("conversation_id");
e.Property(x => x.EventSequence).HasColumnName("event_sequence");
e.Property(x => x.EventType).HasColumnName("event_type");
e.Property(x => x.ActorUserId).HasColumnName("actor_user_id");
e.Property(x => x.FromAssigneeUserId).HasColumnName("from_assignee_user_id");
e.Property(x => x.ToAssigneeUserId).HasColumnName("to_assignee_user_id");
e.Property(x => x.MessageSequenceAtEvent).HasColumnName("message_sequence_at_event");
e.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(500);
e.Property(x => x.OccurredAt).HasColumnName("occurred_at");
e.HasIndex(x => new { x.ConversationId, x.EventSequence }).IsUnique();
e.HasOne<SupportConversation>().WithMany().HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Restrict);
e.HasOne<User>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
e.HasOne<User>().WithMany().HasForeignKey(x => x.FromAssigneeUserId).OnDelete(DeleteBehavior.Restrict);
e.HasOne<User>().WithMany().HasForeignKey(x => x.ToAssigneeUserId).OnDelete(DeleteBehavior.Restrict);
});
}
}
