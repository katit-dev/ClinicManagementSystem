using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Support;
using ClinicManagementSystem.Infrastructure.Models;
using ClinicManagementSystem.Infrastructure.Repositories;
using ClinicManagementSystem.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClinicManagementSystem.Application.Services;

public interface ISupportChatService
{
    SupportSettingsDto Settings();
    Task<HttpResponseData<object>> ListAsync(int uid,string side,SupportListQuery query);
    Task<HttpResponseData<object>> CreateAsync(int uid,SupportCreateRequest request);
    Task<HttpResponseData<object>> UpdateSubjectAsync(int uid,long id,SupportSubjectRequest request);
    Task<HttpResponseData<object>> MessagesAsync(int uid,string side,long id,SupportCursorQuery query);
    Task<HttpResponseData<object>> SendAsync(int uid,string side,long id,SupportMessageRequest request);
    Task<HttpResponseData<object>> ActAsync(int uid,string side,long id,string action,SupportActionRequest request);
    Task<HttpResponseData<object>> ReadAsync(int uid,string side,long id,SupportReadRequest request);
    Task<HttpResponseData<object>> EventsAsync(int uid,string side,long id,SupportCursorQuery query);
    Task<HttpResponseData<object>> ReceptionistsAsync(int uid);
    Task<HttpResponseData<object>> UnreadAsync(int uid);
}

public static class SupportChatRules
{
    public static string NormalizeSubject(string? subject) => string.IsNullOrWhiteSpace(subject) ? SupportTopics.Default : subject.Trim();
    public static bool CanRead(SupportConversation c,int uid,string side) => side switch {
        "me"=>c.PatientUserId==uid,
        "reception"=>c.AssignedReceptionistUserId==uid && c.Status!=0,
        _=>false
    };
    public static long AdvanceRead(long current,long requested,long last) {
        if(requested<0 || requested>last) throw new ArgumentOutOfRangeException(nameof(requested));
        return Math.Max(current,requested);
    }
    public static string Acknowledgement(string hours)=>$"Phòng khám đã nhận được yêu cầu của bạn. Giờ hỗ trợ: {hours}. Nếu bạn nhắn trong khung giờ này, lễ tân sẽ phản hồi sớm nhất có thể. Nếu nhắn ngoài giờ hỗ trợ, yêu cầu của bạn sẽ được phản hồi trong giờ làm việc tiếp theo.";
    public static SupportPatientMessage PatientMessage(SupportMessage m)=>new(m.Id,m.MessageSequence,m.SenderSide,m.MessageKind,
        m.SenderSide==0?"Bạn":m.SenderSide==1?"Hỗ trợ phòng khám":"",m.Content,m.SentAt,m.ClientMessageId);
    public static SupportStaffMessage StaffMessage(SupportMessage m)=>new(m.Id,m.MessageSequence,m.SenderSide,m.MessageKind,
        m.SenderSide==2?"Hệ thống":m.SenderNameSnapshot,m.Content,m.SentAt,m.ClientMessageId,m.SenderUserId);
}

public sealed class SupportChatService(IUnitOfWork uow,IOptions<SupportChatOptions> options,
    ISupportChatNotifier notifier,ILogger<SupportChatService> logger) : ISupportChatService
{
    private ISupportChatRepository Repo=>uow.SupportChatRepository;
    private SupportChatOptions Options=>options.Value;
    private static string Role(string side)=>side switch { "me"=>"Patient", "reception"=>"Receptionist", "admin"=>"Admin", _=>throw new Failure(403,"Không có quyền truy cập.") };
    public SupportSettingsDto Settings()=>new(Options.Enabled,Options.SupportHours,Options.Hotline,Math.Clamp(Options.MaxMessageLength,1,2000));
    private sealed class Failure(int code,string message):Exception(message) { public int Code=>code; }
    private static void Require([DoesNotReturnIf(false)] bool condition,int code,string message) { if(!condition) throw new Failure(code,message); }
    private static void Validate(object value) {
        var errors=new List<ValidationResult>();
        Require(Validator.TryValidateObject(value,new(value),errors,true),400,errors.FirstOrDefault()?.ErrorMessage??"Dữ liệu không hợp lệ.");
    }
    private async Task<HttpResponseData<object>> Execute(int uid,string side,Func<User,Task<object>> operation,bool write=false)
    {
        if(!Options.Enabled) return Result(503,"Hỗ trợ trực tuyến chưa được bật.");
        if(uid<=0) return Result(401,"Vui lòng đăng nhập lại.");
        var started=false;
        try {
            await uow.BeginTransactionAsync();started=true;
            if(write) await Repo.LockActorAsync(uid);
            var actor=await Repo.ActorAsync(uid,Role(side));
            Require(actor!=null,403,"Tài khoản không có quyền hoặc đã ngưng hoạt động.");
            var data=await operation(actor!);
            await uow.SaveChangesAsync();
            await uow.CommitTransactionAsync();started=false;
            return Result(200,"Thành công.",data);
        }
        catch(Failure e) { if(started) await uow.RollbackTransactionAsync();return Result(e.Code,e.Message); }
        catch(Exception e) {
            if(started) await uow.RollbackTransactionAsync();
            // Never log content, JWT, SQL parameter values or exception messages.
            logger.LogError("Support operation failed for User {UserId}; Code {ErrorCode}",uid,e.GetType().Name);
            return Result(500,"Không thể xử lý yêu cầu. Vui lòng tải lại và thử lại.");
        }
    }
    private static HttpResponseData<object> Result(int code,string message,object? content=null)=>new(){StatusCode=code,Message=message,Content=content};
    private async Task<SupportConversation> Conversation(long id,int uid,string side,bool dispatch=false) {
        var c=await Repo.LockAsync(id);
        Require(c!=null && (dispatch && side=="admin" || c!=null && SupportChatRules.CanRead(c,uid,side)),404,"Không tìm thấy yêu cầu trong phạm vi truy cập.");
        return c!;
    }
    private string Content(SupportMessageRequest r) {
        Validate(r);var content=r.Content.Trim();
        Require(content.Length>0 && content.Length<=Math.Clamp(Options.MaxMessageLength,1,2000) && r.ClientMessageId!=Guid.Empty,400,"Nội dung hoặc mã gửi tin không hợp lệ.");
        return content;
    }
    private static SupportPatientConversation PatientSummary(SupportInboxRow row) {
        var c=row.Conversation;return new(c.Id,c.Subject,c.Status,c.CreatedAt,c.UpdatedAt,c.LastMessageAt,c.LastMessageSequence,c.PatientLastReadSequence,row.UnreadCount);
    }
    private static SupportStaffConversation StaffSummary(SupportInboxRow row) {
        var c=row.Conversation;return new(c.Id,c.Subject,c.Status,c.CreatedAt,c.UpdatedAt,c.LastMessageAt,c.LastMessageSequence,
            c.AssignedLastReadSequence,row.UnreadCount,c.PatientId,row.PatientName,row.PatientCode,c.AssignedReceptionistUserId,row.AssignedName,Convert.ToBase64String(c.RowVersion));
    }
    public Task<HttpResponseData<object>> ListAsync(int uid,string side,SupportListQuery q)=>Execute(uid,side,async actor=>{
        Validate(q);Require(side!="reception" || new[]{"waiting","mine","closed"}.Contains(q.Scope),400,"Tab không hợp lệ.");
        Require(!q.From.HasValue || !q.To.HasValue || q.From<=q.To,400,"Khoảng ngày không hợp lệ.");
        Require(q.To!=DateOnly.MaxValue,400,"Ngày kết thúc không hợp lệ.");
        Require(q.From!=DateOnly.MinValue,400,"Ngày bắt đầu không hợp lệ.");
        // Date filters represent clinic dates, converted once to UTC boundaries.
        var (items,total)=await Repo.ListAsync(uid,side,q.Scope,q.Status,q.AssignedUserId,q.Search,
            q.From?.ToDateTime(TimeOnly.MinValue).AddHours(-7),q.To?.AddDays(1).ToDateTime(TimeOnly.MinValue).AddHours(-7),q.Page,q.PageSize);
        if(side=="me") return new SupportPage<SupportPatientConversation>(items.Select(PatientSummary).ToList(),total,q.Page,q.PageSize);
        return new SupportPage<SupportStaffConversation>(items.Select(StaffSummary).ToList(),total,q.Page,q.PageSize);
    });
    private void AddMessage(SupportConversation c,User? actor,byte side,string content,Guid clientId,byte kind=0) {
        var now=DateTime.UtcNow;
        Repo.AddMessage(new(){ConversationId=c.Id,MessageSequence=++c.LastMessageSequence,SenderUserId=actor?.Id,
            SenderSide=side,SenderNameSnapshot=actor==null?"Hệ thống":(actor.FullName??actor.Username)[..Math.Min(150,(actor.FullName??actor.Username).Length)],
            Content=content,ClientMessageId=clientId,MessageKind=kind,SentAt=now});
        c.LastMessageAt=c.UpdatedAt=now;
    }
    private void Event(SupportConversation c,int actor,byte type,int? from,int? to,string? reason=null) {
        c.UpdatedAt=DateTime.UtcNow;
        Repo.AddEvent(new(){ConversationId=c.Id,EventSequence=++c.LastEventSequence,EventType=type,ActorUserId=actor,
            FromAssigneeUserId=from,ToAssigneeUserId=to,MessageSequenceAtEvent=c.LastMessageSequence,Reason=reason,OccurredAt=c.UpdatedAt});
    }
    private async Task Notify(HttpResponseData<object> result,SupportConversation? c,int? old=null) {
        if(result.StatusCode!=200 || c==null) return;
        try { await notifier.ChangedAsync(c.Id,c.PatientUserId,old,c.AssignedReceptionistUserId); }
        catch { logger.LogWarning("Support delivery deferred for Conversation {ConversationId}",c.Id); }
    }
    public async Task<HttpResponseData<object>> CreateAsync(int uid,SupportCreateRequest r) {
        SupportConversation? changed=null;
        var result=await Execute(uid,"me",async actor=>{
            var text=Content(r);
            var retry=await Repo.RetryAsync(uid,r.ClientMessageId);
            if(retry!=null) {
                var existing=await Conversation(retry.ConversationId,uid,"me");
                // Subject is editable metadata; a retry of the original message must not undo a later rename.
                Require(retry.MessageSequence==1 && retry.SenderSide==0 && retry.Content==text,409,"Mã gửi tin đã được sử dụng cho nội dung khác.");
                return new SupportCreatedDto(PatientSummary(await Repo.SummaryAsync(existing,true)),
                    (await Repo.MessagesAsync(existing.Id,null,0,2)).Select(SupportChatRules.PatientMessage).ToList());
            }
            var patient=await Repo.PatientAsync(uid);Require(patient!=null,403,"Chưa có hồ sơ bệnh nhân đang hoạt động.");
            Require(await Repo.OpenAsync(patient!.Id)==null,409,"Bạn đã có yêu cầu đang mở. Hãy tiếp tục yêu cầu đó.");
            var c=new SupportConversation {PatientId=patient.Id,PatientUserId=uid,Subject=SupportChatRules.NormalizeSubject(r.Subject),CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow};
            await Repo.AddAsync(c);await uow.SaveChangesAsync();
            AddMessage(c,actor,0,text,r.ClientMessageId);
            var acknowledgement=SupportChatRules.Acknowledgement(Options.SupportHours);
            Require(acknowledgement.Length<=2000,503,"Cấu hình giờ hỗ trợ chưa hợp lệ.");
            AddMessage(c,null,2,acknowledgement,Guid.NewGuid(),1);
            Event(c,uid,0,null,null);await uow.SaveChangesAsync();changed=c;
            return new SupportCreatedDto(PatientSummary(await Repo.SummaryAsync(c,true)),
                (await Repo.MessagesAsync(c.Id,null,0,2)).Select(SupportChatRules.PatientMessage).ToList());
        },true);
        await Notify(result,changed);return result;
    }
    public async Task<HttpResponseData<object>> UpdateSubjectAsync(int uid,long id,SupportSubjectRequest r) {
        SupportConversation? changed=null;
        var result=await Execute(uid,"me",async actor=>{
            Validate(r);
            var c=await Conversation(id,uid,"me");
            Require(c.Status<2,409,"Yêu cầu đã kết thúc, không thể đổi chủ đề.");
            var subject=SupportChatRules.NormalizeSubject(r.Subject);
            if(c.Subject!=subject) {
                Require(c.Subject==r.ExpectedSubject,409,"Chủ đề đã thay đổi trên phiên khác. Vui lòng tải lại trước khi sửa.");
                c.Subject=subject;c.UpdatedAt=DateTime.UtcNow;
                await uow.SaveChangesAsync();changed=c;
            }
            return PatientSummary(await Repo.SummaryAsync(c,true));
        },true);
        await Notify(result,changed);return result;
    }
    public Task<HttpResponseData<object>> MessagesAsync(int uid,string side,long id,SupportCursorQuery q)=>Execute(uid,side,async actor=>{
        Validate(q);_ = await Conversation(id,uid,side);
        var rows=await Repo.MessagesAsync(id,q.BeforeSequence,q.AfterSequence,q.PageSize+1);
        var more=rows.Count>q.PageSize;rows=rows.Take(q.PageSize).OrderBy(m=>m.MessageSequence).ToList();
        long? cursor=rows.Count==0?null:q.AfterSequence.HasValue?rows[^1].MessageSequence:rows[0].MessageSequence;
        if(side=="me") return new SupportCursorPage<SupportPatientMessage>(rows.Select(SupportChatRules.PatientMessage).ToList(),more,cursor);
        return new SupportCursorPage<SupportStaffMessage>(rows.Select(SupportChatRules.StaffMessage).ToList(),more,cursor);
    });
    public async Task<HttpResponseData<object>> SendAsync(int uid,string side,long id,SupportMessageRequest r) {
        SupportConversation? changed=null;
        var result=await Execute(uid,side,async actor=>{
            var text=Content(r);var c=await Conversation(id,uid,side);
            var retry=await Repo.RetryAsync(uid,r.ClientMessageId);
            if(retry!=null) {
                Require(retry.ConversationId==id && retry.SenderSide==(side=="me"?0:1) && retry.Content==text,409,"Mã gửi tin đã được sử dụng cho nội dung khác.");
                return side=="me"?(object)SupportChatRules.PatientMessage(retry):SupportChatRules.StaffMessage(retry);
            }
            Require(c.Status<2,409,"Yêu cầu đã kết thúc.");
            AddMessage(c,actor,(byte)(side=="me"?0:1),text,r.ClientMessageId);
            await uow.SaveChangesAsync();changed=c;
            var saved=await Repo.RetryAsync(uid,r.ClientMessageId);
            return side=="me"?(object)SupportChatRules.PatientMessage(saved!):SupportChatRules.StaffMessage(saved!);
        },true);
        await Notify(result,changed);return result;
    }
    public async Task<HttpResponseData<object>> ActAsync(int uid,string side,long id,string action,SupportActionRequest r) {
        SupportConversation? changed=null;int? old=null;
        var result=await Execute(uid,side,async actor=>{
            Validate(r);
            SupportConversation c;
            if(side=="reception" && action=="claim") {
                c=(await Repo.LockAsync(id))!;Require(c!=null,404,"Không tìm thấy yêu cầu.");
            } else c=await Conversation(id,uid,side,side=="admin");
            Require(c.Status<2,409,"Yêu cầu đã kết thúc.");
            if(side=="admin") Require(!string.IsNullOrEmpty(r.ExpectedVersion) && r.ExpectedVersion==Convert.ToBase64String(c.RowVersion),409,"Yêu cầu đã thay đổi. Vui lòng tải lại.");
            var reason=r.Reason?.Trim();old=c.AssignedReceptionistUserId;
            switch(action) {
                case "claim": case "assign": case "transfer":
                    Require(side=="admin" || side=="reception" && action=="claim",403,"Không có quyền phân công.");
                    Require(action=="transfer"?c.Status==1:c.Status==0,409,"Yêu cầu đã được nhận hoặc thay đổi trạng thái.");
                    var target=action=="claim"?uid:r.ReceptionistUserId??0;
                    Require(target!=old,409,"Người nhận đang phụ trách yêu cầu này.");
                    Require(await Repo.ActorAsync(target,"Receptionist")!=null,400,"Người nhận phải là lễ tân đang hoạt động.");
                    if(action=="transfer") Require(!string.IsNullOrEmpty(reason),400,"Vui lòng nhập lý do bàn giao.");
                    c.AssignedReceptionistUserId=target;c.AssignedAt=DateTime.UtcNow;c.AssignedLastReadSequence=0;c.Status=1;
                    Event(c,uid,(byte)(action=="transfer"?3:1),old,target,reason);break;
                case "release":
                    Require(side!="me" && c.Status==1,409,"Yêu cầu không ở trạng thái đang hỗ trợ.");
                    Require(!string.IsNullOrEmpty(reason),400,"Vui lòng nhập lý do trả về hàng chờ.");
                    c.Status=0;c.AssignedReceptionistUserId=null;c.AssignedAt=null;c.AssignedLastReadSequence=0;
                    Event(c,uid,2,old,null,reason);break;
                case "close":
                    if(side=="admin") Require(!string.IsNullOrEmpty(reason),400,"Vui lòng nhập lý do kết thúc.");
                    var wasWaiting=c.Status==0;c.Status=2;c.ClosedAt=DateTime.UtcNow;c.ClosedByUserId=uid;
                    c.CloseReason=(byte)(side=="admin"?3:side=="me"?(wasWaiting?2:1):0);
                    Event(c,uid,4,old,old,side=="me"?null:reason);break;
                default:throw new Failure(400,"Thao tác không hợp lệ.");
            }
            await uow.SaveChangesAsync();changed=c;
            var summary=await Repo.SummaryAsync(c,side=="me");
            return side=="me"?(object)PatientSummary(summary):StaffSummary(summary);
        },true);
        await Notify(result,changed,old);return result;
    }
    public async Task<HttpResponseData<object>> ReadAsync(int uid,string side,long id,SupportReadRequest r) {
        SupportConversation? changed=null;
        var result=await Execute(uid,side,async actor=>{
            Validate(r);var c=await Conversation(id,uid,side);
            Require(r.Sequence<=c.LastMessageSequence,400,"Vị trí đọc vượt quá tin đã tồn tại.");
            if(side=="me") c.PatientLastReadSequence=SupportChatRules.AdvanceRead(c.PatientLastReadSequence,r.Sequence,c.LastMessageSequence);
            else {
                Require(r.ExpectedVersion==Convert.ToBase64String(c.RowVersion),409,"Phiên phân công đã thay đổi. Vui lòng tải lại.");
                c.AssignedLastReadSequence=SupportChatRules.AdvanceRead(c.AssignedLastReadSequence,r.Sequence,c.LastMessageSequence);
            }
            await uow.SaveChangesAsync();changed=c;return new { Sequence=side=="me"?c.PatientLastReadSequence:c.AssignedLastReadSequence };
        },true);
        await Notify(result,changed);return result;
    }
    public Task<HttpResponseData<object>> UnreadAsync(int uid)=>Execute(uid,"me",async actor=>(object)new { UnreadCount=await Repo.UnreadTotalAsync(uid) });
    public Task<HttpResponseData<object>> ReceptionistsAsync(int uid)=>Execute(uid,"admin",async actor=>(await Repo.ReceptionistsAsync()).Select(x=>new SupportReceptionistDto(x.Id,x.Name,x.Count)).ToList());
    public Task<HttpResponseData<object>> EventsAsync(int uid,string side,long id,SupportCursorQuery q)=>Execute(uid,side,async actor=>{
        Validate(q);_ = await Conversation(id,uid,side,side=="admin");
        var rows=await Repo.EventsAsync(id,q.BeforeSequence,q.AfterSequence,q.PageSize+1);
        var more=rows.Count>q.PageSize;rows=rows.Take(q.PageSize).OrderBy(e=>e.EventSequence).ToList();
        var ids=rows.SelectMany(e=>new[]{e.ActorUserId,e.FromAssigneeUserId??0,e.ToAssigneeUserId??0}).Where(x=>x>0).Distinct();
        var names=await Repo.NamesAsync(ids);
        return new SupportCursorPage<SupportEventDto>(rows.Select(e=>new SupportEventDto(e.Id,e.EventSequence,e.EventType,names.GetValueOrDefault(e.ActorUserId,"Tài khoản"),
            e.FromAssigneeUserId.HasValue?names.GetValueOrDefault(e.FromAssigneeUserId.Value):null,e.ToAssigneeUserId.HasValue?names.GetValueOrDefault(e.ToAssigneeUserId.Value):null,
            e.Reason,e.MessageSequenceAtEvent,e.OccurredAt)).ToList(),more,rows.Count==0?null:q.AfterSequence.HasValue?rows[^1].EventSequence:rows[0].EventSequence);
    });
}
