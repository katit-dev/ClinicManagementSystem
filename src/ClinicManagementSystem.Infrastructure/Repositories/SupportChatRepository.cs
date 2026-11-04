using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Infrastructure.Repositories;

public sealed record SupportInboxRow(SupportConversation Conversation, string PatientName, string PatientCode,
    string? AssignedName, int UnreadCount);
public interface ISupportChatRepository : IRepositoryBase<SupportConversation>
{
    Task<User?> ActorAsync(int userId, string role);
    Task LockActorAsync(int userId);
    Task<Patient?> PatientAsync(int userId);
    Task<SupportConversation?> LockAsync(long id);
    Task<SupportConversation?> OpenAsync(int patientId);
    Task<SupportMessage?> RetryAsync(int userId, Guid clientId);
    Task<(List<SupportInboxRow> Items,int Total)> ListAsync(int userId, string side, string scope, byte? status,
        int? assignee, string? search, DateTime? from, DateTime? to, int page, int size);
    Task<SupportInboxRow> SummaryAsync(SupportConversation conversation, bool patient);
    Task<List<SupportMessage>> MessagesAsync(long id, long? before, long? after, int take);
    Task<List<SupportConversationEvent>> EventsAsync(long id, long? before, long? after, int take);
    Task<int> UnreadTotalAsync(int userId);
    Task<List<(int Id,string Name,int Count)>> ReceptionistsAsync();
    Task<Dictionary<int,string>> NamesAsync(IEnumerable<int> ids);
    void AddMessage(SupportMessage message);
    void AddEvent(SupportConversationEvent item);
}

public class SupportChatRepository : RepositoryBase<SupportConversation>, ISupportChatRepository
{
    private readonly ClinicManagementDbContext db;
    public SupportChatRepository(ClinicManagementDbContext db) : base(db) { this.db=db; }
    public Task<User?> ActorAsync(int id, string role) => db.Users.AsNoTracking().FirstOrDefaultAsync(u =>
        u.Id==id && u.IsActive && !u.IsDeleted && (u.LockoutEnd==null || u.LockoutEnd<=DateTime.UtcNow)
        && u.UserRoleUsers.Any(ur=>ur.Role.Name==role));
    public async Task LockActorAsync(int id) => _ = await db.Users.FromSqlInterpolated(
        $"SELECT * FROM auth.users WITH (UPDLOCK,HOLDLOCK) WHERE id={id}").Select(u=>u.Id).ToListAsync();
    public Task<Patient?> PatientAsync(int id) => db.Patients.AsNoTracking().FirstOrDefaultAsync(p=>p.UserId==id && p.IsActive);
    public Task<SupportConversation?> LockAsync(long id) => db.SupportConversations.FromSqlInterpolated(
        $"SELECT * FROM support.support_conversations WITH (UPDLOCK,HOLDLOCK) WHERE id={id}").SingleOrDefaultAsync();
    public Task<SupportConversation?> OpenAsync(int id) => db.SupportConversations.SingleOrDefaultAsync(c=>c.PatientId==id && c.Status<2);
    public Task<SupportMessage?> RetryAsync(int id,Guid clientId) => db.SupportMessages.AsNoTracking()
        .SingleOrDefaultAsync(m=>m.SenderUserId==id && m.ClientMessageId==clientId);

    internal static IQueryable<SupportConversation> Visible(IQueryable<SupportConversation> rows,int userId,string side,string scope)
        => side switch {
            "me"=>rows.Where(c=>c.PatientUserId==userId),
            "reception" when scope=="waiting"=>rows.Where(c=>c.Status==0),
            "reception" when scope=="mine"=>rows.Where(c=>c.AssignedReceptionistUserId==userId && c.Status==1),
            "reception"=>rows.Where(c=>c.AssignedReceptionistUserId==userId && c.Status==2),
            _=>rows
        };
    private IQueryable<SupportInboxRow> Inbox(IQueryable<SupportConversation> rows,bool patient)
        => from c in rows
           join p in db.Patients on c.PatientId equals p.Id
           join u in db.Users on c.AssignedReceptionistUserId equals (int?)u.Id into assigned
           from u in assigned.DefaultIfEmpty()
           select new SupportInboxRow(c,p.FullName,p.PatientCode,u==null?null:u.FullName??u.Username,
               db.SupportMessages.Count(m=>m.ConversationId==c.Id
                   && m.SenderSide==(patient?1:0)
                   && m.MessageSequence>(patient?c.PatientLastReadSequence:c.AssignedLastReadSequence)));
    public async Task<(List<SupportInboxRow>,int)> ListAsync(int uid,string side,string scope,byte? status,
        int? assignee,string? search,DateTime? from,DateTime? to,int page,int size)
    {
        var rows=Visible(db.SupportConversations.AsNoTracking(),uid,side,scope);
        if(side=="me" && scope=="open") rows=rows.Where(c=>c.Status<2);
        if(status.HasValue) rows=rows.Where(c=>c.Status==status);
        if(assignee.HasValue) rows=rows.Where(c=>c.AssignedReceptionistUserId==assignee);
        if(from.HasValue) rows=rows.Where(c=>c.CreatedAt>=from);
        if(to.HasValue) rows=rows.Where(c=>c.CreatedAt<to);
        if(!string.IsNullOrWhiteSpace(search)) {
            var key=search.Trim();long.TryParse(key.TrimStart('#'),out var id);
            rows=rows.Where(c=>c.Id==id || db.Patients.Any(p=>p.Id==c.PatientId && (p.FullName.Contains(key) || p.PatientCode.Contains(key))));
        }
        var total=await rows.CountAsync();
        rows=side=="reception" && scope=="waiting" ? rows.OrderBy(c=>c.CreatedAt).ThenBy(c=>c.Id)
            : rows.OrderByDescending(c=>c.UpdatedAt).ThenByDescending(c=>c.Id);
        return (await Inbox(rows.Skip((page-1)*size).Take(size),side=="me").ToListAsync(),total);
    }
    public Task<SupportInboxRow> SummaryAsync(SupportConversation c,bool patient)
        => Inbox(db.SupportConversations.AsNoTracking().Where(x=>x.Id==c.Id),patient).SingleAsync();
    public async Task<List<SupportMessage>> MessagesAsync(long id,long? before,long? after,int take)
    {
        var q=db.SupportMessages.AsNoTracking().Where(m=>m.ConversationId==id);
        if(before.HasValue) q=q.Where(m=>m.MessageSequence<before);
        if(after.HasValue) return await q.Where(m=>m.MessageSequence>after).OrderBy(m=>m.MessageSequence).Take(take).ToListAsync();
        return await q.OrderByDescending(m=>m.MessageSequence).Take(take).ToListAsync();
    }
    public async Task<List<SupportConversationEvent>> EventsAsync(long id,long? before,long? after,int take)
    {
        var q=db.SupportConversationEvents.AsNoTracking().Where(m=>m.ConversationId==id);
        if(before.HasValue) q=q.Where(m=>m.EventSequence<before);
        if(after.HasValue) return await q.Where(m=>m.EventSequence>after).OrderBy(m=>m.EventSequence).Take(take).ToListAsync();
        return await q.OrderByDescending(m=>m.EventSequence).Take(take).ToListAsync();
    }
    public Task<int> UnreadTotalAsync(int uid) => db.SupportMessages.CountAsync(m=>m.SenderSide==1 &&
        db.SupportConversations.Any(c=>c.Id==m.ConversationId && c.PatientUserId==uid && m.MessageSequence>c.PatientLastReadSequence));
    public async Task<List<(int,string,int)>> ReceptionistsAsync() {
        var rows=await db.Users.AsNoTracking().Where(u=>u.IsActive && !u.IsDeleted
            && (u.LockoutEnd==null || u.LockoutEnd<=DateTime.UtcNow) && u.UserRoleUsers.Any(ur=>ur.Role.Name=="Receptionist"))
            .Select(u=>new { u.Id,Name=u.FullName??u.Username,Count=db.SupportConversations.Count(c=>c.AssignedReceptionistUserId==u.Id && c.Status==1) })
            .OrderBy(u=>u.Name).ToListAsync();
        return rows.Select(u=>(u.Id,u.Name,u.Count)).ToList();
    }
    public Task<Dictionary<int,string>> NamesAsync(IEnumerable<int> ids) => db.Users.AsNoTracking()
        .Where(u=>ids.Contains(u.Id)).ToDictionaryAsync(u=>u.Id,u=>u.FullName??u.Username);
    public void AddMessage(SupportMessage m)=>db.SupportMessages.Add(m);
    public void AddEvent(SupportConversationEvent e)=>db.SupportConversationEvents.Add(e);
}
