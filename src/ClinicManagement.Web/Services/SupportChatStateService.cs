using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Support;
using Microsoft.AspNetCore.SignalR.Client;

namespace ClinicManagementSystem.Web.Services;

// Scoped to a Blazor circuit; never share user tokens or chat bodies across circuits.
public sealed class SupportChatStateService(UserStateService user,IHttpClientFactory clients,IConfiguration configuration,ILogger<SupportChatStateService>? logger=null) : IAsyncDisposable
{
    public sealed class Conversation
    {
        public long Id { get; set; }
        public string Subject { get; set; }="";
        public byte Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public long LastMessageSequence { get; set; }
        public long LastReadSequence { get; set; }
        public int UnreadCount { get; set; }
        public string PatientName { get; set; }="";
        public string PatientCode { get; set; }="";
        public int? AssignedUserId { get; set; }
        public string? AssignedName { get; set; }
        public string Version { get; set; }="";
    }
    private readonly SemaphoreSlim _requests=new(1,1),_lifecycle=new(1,1),_messageLoads=new(1,1);
    private int _selectionVersion,_inboxRequest;
    private HubConnection? _hub;
    private CancellationTokenSource? _lifetime;
    private int? _owner;
    private int _generation;
    private bool _subscribed;
    private bool _disposed;
    public SupportSettingsDto? Settings { get; private set; }
    public SupportPage<Conversation> Inbox { get; private set; }=new([],0,1,20);
    public Conversation? Selected { get; private set; }
    public List<SupportStaffMessage> Messages { get; private set; }=[];
    public List<SupportEventDto> Events { get; private set; }=[];
    public List<SupportReceptionistDto> Receptionists { get; private set; }=[];
    public bool LoadingMessages { get; private set; }
    public bool MessagesLoaded { get; private set; }
    public string MessageError { get; private set; }="";
    public bool MoreMessages { get; private set; }
    public bool MoreEvents { get; private set; }
    public int UnreadCount { get; private set; }
    public string ConnectionStatus { get; private set; }="Chưa kết nối";
    public string Error { get; private set; }="";
    public string Side { get; private set; }="me";
    public SupportListQuery Query { get; set; }=new();
    public event Action? Invalidated;
    public event Action? MessagesChanged;
    private TimeSpan RequestTimeout=>TimeSpan.FromSeconds(Math.Clamp(configuration.GetValue("SupportChat:RequestTimeoutSeconds",15),1,60));
    public event Action? SessionCleared;
    private void Invalidate()=>Invalidated?.Invoke();
    public static string StatusLabel(byte status)=>status switch {0=>"Chờ tiếp nhận",1=>"Đang hỗ trợ",_=>"Đã kết thúc"};
    public static string ClinicTime(DateTime utc)=>DateTime.SpecifyKind(utc,DateTimeKind.Utc).AddHours(7).ToString("dd/MM/yyyy HH:mm");

    public async Task<bool> StartAsync(string side)
    {
        await _lifecycle.WaitAsync();
        try {
            if(_disposed) return false;
            if(!_subscribed) { user.OnChange+=AccountChanged;_subscribed=true; }
            var uid=user.CurrentUser?.Id;
            if(uid==null || string.IsNullOrEmpty(user.AccessToken)) return false;
            if(_owner!=uid || Side!=side) await ClearCoreAsync();
            _owner=uid;Side=side;
            using var http=clients.CreateClient("ClinicApi");
            Settings=await http.GetFromJsonAsync<SupportSettingsDto>("/api/support/settings");
            if(Settings is not { Enabled:true }) { ConnectionStatus="Hỗ trợ trực tuyến chưa được bật";return false; }
            _lifetime??=new();
            if(_hub==null) {
                _hub=new HubConnectionBuilder().WithUrl(new Uri(new Uri(configuration["ApiSettings:BaseUrl"]!),"/hubs/support-chat"),o=> {
                    o.AccessTokenProvider=TokenAsync;
                }).WithAutomaticReconnect([TimeSpan.Zero,TimeSpan.FromSeconds(2),TimeSpan.FromSeconds(5),TimeSpan.FromSeconds(10)]).Build();
                _hub.On<JsonElement>("SupportConversationChanged",_=>Invalidate());
                _hub.On("SupportInboxChanged",()=>Invalidate());
                _hub.On("SupportDispatchChanged",()=>Invalidate());
                _hub.Reconnecting+=_=>{ConnectionStatus="Đang kết nối lại…";Invalidate();return Task.CompletedTask;};
                _hub.Reconnected+=_=>{ConnectionStatus="Đã kết nối";Invalidate();return Task.CompletedTask;};
                _hub.Closed+=_=>{ConnectionStatus="Mất kết nối, đang thử lại…";Invalidate();return Task.CompletedTask;};
                _=ReconcileAsync(_lifetime.Token);
            }
            if(_hub.State==HubConnectionState.Disconnected) {
                try { await _hub.StartAsync(_lifetime.Token);ConnectionStatus="Đã kết nối"; }
                catch { ConnectionStatus="Chưa nối được realtime, đang thử lại…"; }
            }
            return true;
        }
        catch { Error="Không thể kết nối hỗ trợ trực tuyến. Vui lòng thử lại.";return false; }
        finally { _lifecycle.Release(); }
    }
    private async Task<string?> TokenAsync() {
        if(_owner!=user.CurrentUser?.Id) return null;
        var token=user.AccessToken;
        try {
            var part=token.Split('.')[1].Replace('-','+').Replace('_','/');part=part.PadRight((part.Length+3)/4*4,'=');
            using var payload=JsonDocument.Parse(Convert.FromBase64String(part));
            if(payload.RootElement.GetProperty("exp").GetInt64()<=DateTimeOffset.UtcNow.AddSeconds(30).ToUnixTimeSeconds()) {
                if(!await user.RefreshTokenAsync()) return null;
                token=user.AccessToken;
            }
        } catch { return null; }
        return _owner==user.CurrentUser?.Id?token:null;
    }
    private async Task ReconcileAsync(CancellationToken ct) {
        try {
            using var timer=new PeriodicTimer(TimeSpan.FromSeconds(Math.Clamp(configuration.GetValue("SupportChat:ReconciliationIntervalSeconds",30),10,120)));
            while(await timer.WaitForNextTickAsync(ct)) {
                if(_owner!=user.CurrentUser?.Id) { await StopAsync();return; }
                if(_hub?.State==HubConnectionState.Disconnected) {
                    try { await _hub.StartAsync(ct);ConnectionStatus="Đã kết nối"; } catch { }
                }
                Invalidate();
            }
        } catch(OperationCanceledException) { }
    }
    private void AccountChanged() { if(_owner.HasValue && _owner!=user.CurrentUser?.Id) _=StopAsync(); }
    private string Prefix=>"/api/support/"+Side;
    private async Task<T?> Request<T>(string path,object? body=null,bool quiet=false,int? selectionVersion=null,CancellationToken cancellationToken=default) where T:class {
        var generation=_generation;var uid=_owner;
        using var budget=CancellationTokenSource.CreateLinkedTokenSource(_lifetime?.Token??CancellationToken.None,cancellationToken);
        budget.CancelAfter(RequestTimeout);var entered=false;var stage="queue";
        try {
            await _requests.WaitAsync(budget.Token);entered=true;
            if(uid==null || uid!=user.CurrentUser?.Id || generation!=_generation || selectionVersion.HasValue && selectionVersion!=_selectionVersion) return null;
            if(!quiet) Error="";
            for(var attempt=0;attempt<2;attempt++) {
                using var http=clients.CreateClient("ClinicAuthorizedApi");
                using var request=new HttpRequestMessage(body==null?HttpMethod.Get:HttpMethod.Post,path);
                request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",user.AccessToken);
                if(body!=null) request.Content=JsonContent.Create(body);
                stage="response";
                using var response=await http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,budget.Token).WaitAsync(budget.Token);
                if(generation!=_generation || uid!=user.CurrentUser?.Id || selectionVersion.HasValue && selectionVersion!=_selectionVersion) return null;
                stage="authentication";
                if(response.StatusCode==HttpStatusCode.Unauthorized && attempt==0 && await user.RefreshTokenAsync().WaitAsync(budget.Token)) continue;
                if(response.StatusCode==HttpStatusCode.Unauthorized) {
                    await StopAsync().WaitAsync(budget.Token);Error="Phiên đăng nhập đã hết hạn hoặc quyền truy cập đã thay đổi. Vui lòng đăng nhập lại.";return null;
                }
                if(response.StatusCode==HttpStatusCode.TooManyRequests) { Error="Bạn gửi quá nhanh. Vui lòng chờ rồi thử lại.";return null; }
                HttpResponseData<T>? data=null;
                stage="body";
                try { data=await response.Content.ReadFromJsonAsync<HttpResponseData<T>>(cancellationToken:budget.Token); } catch(JsonException) { }
                if(!response.IsSuccessStatusCode || data?.Content==null) {
                    if(response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound && Selected!=null) { NewConversation(); }
                    Error=data?.Message??"Không thể xử lý yêu cầu. Vui lòng tải lại.";return null;
                }
                return generation==_generation && uid==user.CurrentUser?.Id?data.Content:null;
            }
        } catch(OperationCanceledException) {
            if(generation==_generation && uid==user.CurrentUser?.Id && (!selectionVersion.HasValue || selectionVersion==_selectionVersion))
            {
                logger?.LogWarning("Support request timed out; Stage {Stage}; Route {Route}; User {UserId}",stage,path.Split('?')[0],uid);
                Error=body==null?"Quá thời gian tải dữ liệu. Vui lòng thử lại và kiểm tra API đang chạy.":"Chưa xác nhận thao tác thành công do quá thời gian chờ. Bạn có thể thử lại.";
            }
        } catch { if(generation!=_generation || uid!=user.CurrentUser?.Id || selectionVersion.HasValue && selectionVersion!=_selectionVersion)return null;Error=body==null?"Không thể tải dữ liệu. Vui lòng thử lại.":"Chưa xác nhận gửi thành công. Bạn có thể thử lại; hệ thống sẽ tránh lưu trùng."; }
        finally { if(entered)_requests.Release(); }
        return null;
    }
    public async Task<bool> LoadInboxAsync() {
        var ticket=++_inboxRequest;
        var q=Query;var path=$"{Prefix}/conversations?page={q.Page}&pageSize={q.PageSize}&scope={q.Scope}";
        if(q.Status.HasValue) path+="&status="+q.Status;
        if(q.AssignedUserId.HasValue) path+="&assignedUserId="+q.AssignedUserId;
        if(q.From.HasValue) path+="&from="+q.From.Value.ToString("yyyy-MM-dd");
        if(q.To.HasValue) path+="&to="+q.To.Value.ToString("yyyy-MM-dd");
        if(!string.IsNullOrWhiteSpace(q.Search)) path+="&search="+Uri.EscapeDataString(q.Search);
        var data=await Request<SupportPage<Conversation>>(path);
        if(data!=null && ticket==_inboxRequest) {
            Inbox=data;
            if(Selected!=null) {
                var fresh=data.Items.FirstOrDefault(c=>c.Id==Selected.Id);
                if(fresh!=null) {
                    if(Side=="me") {fresh.LastReadSequence=Math.Max(fresh.LastReadSequence,Selected.LastReadSequence);if(fresh.LastReadSequence>=fresh.LastMessageSequence)fresh.UnreadCount=0;}
                    Selected=fresh;
                }
                else if(Side!="me") { NewConversation(); }
            }
            return true;
        }
        return false;
    }
    public async Task LoadUnreadAsync() {
        var data=await Request<UnreadSummary>("/api/support/me/unread-summary",quiet:true);
        if(data!=null) UnreadCount=data.UnreadCount;
    }
    public sealed class UnreadSummary { public int UnreadCount { get;set; } }
    public void Select(Conversation c) {ResetMessages();Selected=c;Events=[];MoreEvents=false;}
    private void ResetMessages() {++_selectionVersion;Messages=[];MoreMessages=false;MessagesLoaded=LoadingMessages=false;MessageError="";}
    public async Task LoadMessagesAsync(bool older=false,bool reset=false) {
        if(Selected==null || Side=="admin") return;
        if(LoadingMessages && !reset)return;
        var id=Selected.Id;var selection=_selectionVersion;
        LoadingMessages=true;MessageError="";MessagesChanged?.Invoke();
        using var budget=CancellationTokenSource.CreateLinkedTokenSource(_lifetime?.Token??CancellationToken.None);
        budget.CancelAfter(RequestTimeout);var entered=false;
        try {
            await _messageLoads.WaitAsync(budget.Token);entered=true;
            if(Selected?.Id!=id || selection!=_selectionVersion) return;
            var latest=reset || Messages.Count==0;
            var path=$"{Prefix}/conversations/{id}/messages?pageSize=30";
            if(!latest)path+=older?"&beforeSequence="+Messages[0].Sequence:"&afterSequence="+Messages[^1].Sequence;
            var page=await Request<SupportCursorPage<SupportStaffMessage>>(path,selectionVersion:selection,cancellationToken:budget.Token);
            while(true) {
                if(Selected?.Id!=id || selection!=_selectionVersion)return;
                if(page==null){MessageError=Error.Length>0?Error:"Không thể tải tin nhắn. Vui lòng thử lại.";return;}
                if(latest && page.Items.Count==0 && Selected.LastMessageSequence>0){
                    MessageError="Chưa tải được nội dung hội thoại. Vui lòng bấm tải lại.";return;
                }
                Messages=(latest?page.Items:Messages.Concat(page.Items)).DistinctBy(m=>m.Id).OrderBy(m=>m.Sequence).ToList();
                MessagesLoaded=true;
                if(latest || older)MoreMessages=page.HasMore;
                // HasMore on a latest/older page means older history, not an after cursor.
                if(latest || older || !page.HasMore || page.Items.Count==0)break;
                page=await Request<SupportCursorPage<SupportStaffMessage>>($"{Prefix}/conversations/{id}/messages?afterSequence={Messages[^1].Sequence}&pageSize=100",selectionVersion:selection,cancellationToken:budget.Token);
            }
        } catch(OperationCanceledException) {
            if(selection==_selectionVersion && Selected?.Id==id)MessageError="Quá thời gian tải tin nhắn. Vui lòng bấm tải lại.";
        } finally {
            if(selection==_selectionVersion){LoadingMessages=false;MessagesChanged?.Invoke();}
            if(entered)_messageLoads.Release();
        }
    }
    public async Task LoadEventsAsync(bool older=false) {
        if(Selected==null || Side=="me") return;
        var id=Selected.Id;var path=$"{Prefix}/conversations/{id}/events?pageSize=30";
        if(Events.Count>0) path+=older?"&beforeSequence="+Events[0].Sequence:"&afterSequence="+Events[^1].Sequence;
        var page=await Request<SupportCursorPage<SupportEventDto>>(path);
        if(page==null || Selected?.Id!=id) return;
        Events=Events.Concat(page.Items).DistinctBy(e=>e.Id).OrderBy(e=>e.Sequence).ToList();
        if(older || Events.Count==page.Items.Count) MoreEvents=page.HasMore;
    }
    public async Task<bool> SendAsync(string content,Guid clientId,string? subject=null) {
        if(Selected==null) {
            var generation=_generation;
            var created=await Request<SupportCreatedDto>(Prefix+"/conversations",new SupportCreateRequest {Subject=subject,Content=content,ClientMessageId=clientId});
            if(created==null) return false;
            // A successful send is already durable, even if the following inbox refresh fails.
            var c=created.Conversation;
            Selected=new(){Id=c.Id,Subject=c.Subject,Status=c.Status,CreatedAt=c.CreatedAt,UpdatedAt=c.UpdatedAt,
                LastMessageSequence=c.LastMessageSequence,LastReadSequence=c.LastReadSequence,UnreadCount=c.UnreadCount};
            MessagesLoaded=true;
            Messages=created.Messages.Select(m=>new SupportStaffMessage(m.Id,m.Sequence,m.SenderSide,m.MessageKind,m.SenderLabel,m.Content,m.SentAt,m.ClientMessageId,null)).ToList();
            await LoadInboxAsync();await LoadMessagesAsync();
            return generation==_generation && Selected?.Id==c.Id;
        }
        var id=Selected.Id;
        var sent=await Request<SupportStaffMessage>($"{Prefix}/conversations/{id}/messages",new SupportMessageRequest {Content=content,ClientMessageId=clientId});
        if(sent==null) return false;
        if(Selected?.Id==id) Messages=Messages.Append(sent).DistinctBy(m=>m.Id).OrderBy(m=>m.Sequence).ToList();
        return true;
    }
    public async Task<bool> UpdateSubjectAsync(string? subject,string expectedSubject) {
        var c=Selected;if(Side!="me" || c==null || c.Status>=2) return false;
        var updated=await Request<Conversation>($"/api/support/me/conversations/{c.Id}/subject",
            new SupportSubjectRequest {Subject=subject,ExpectedSubject=expectedSubject});
        if(updated==null || Selected?.Id!=c.Id) return false;
        Selected=updated;
        var row=Inbox.Items.FirstOrDefault(x=>x.Id==c.Id);
        if(row!=null) {row.Subject=updated.Subject;row.UpdatedAt=updated.UpdatedAt;}
        return true;
    }
    public async Task<bool> ActAsync(string action,string? reason=null,int? target=null,Conversation? row=null) {
        var c=row??Selected;if(c==null) return false;
        var updated=await Request<Conversation>($"{Prefix}/conversations/{c.Id}/{action}",new SupportActionRequest {Reason=reason,ReceptionistUserId=target,ExpectedVersion=c.Version});
        if(updated==null) return false;
        await LoadInboxAsync();
        if(action is "claim" or "assign" or "transfer") {Select(updated);if(Side!="admin") await LoadMessagesAsync(reset:true);await LoadEventsAsync();}
        else if(Selected?.Id==c.Id) {Selected=updated;if(action=="release") {Selected=null;Messages=[];Events=[];}}
        return true;
    }
    public async Task MarkReadAsync(long? visibleSequence=null,long? conversationId=null) {
        var c=Selected;var selection=_selectionVersion;if(c==null || Messages.Count==0 || Side=="admin") return;
        if(conversationId.HasValue && conversationId!=c.Id)return;
        var seq=visibleSequence??Messages[^1].Sequence;if(seq<=c.LastReadSequence || !Messages.Any(m=>m.Sequence==seq)) return;
        var result=await Request<JsonRead>($"{Prefix}/conversations/{c.Id}/read",new SupportReadRequest {Sequence=seq,ExpectedVersion=c.Version},true,selection);
        if(result!=null && Selected?.Id==c.Id && selection==_selectionVersion) {Selected.LastReadSequence=Math.Max(Selected.LastReadSequence,result.Sequence);Selected.UnreadCount=0;var row=Inbox.Items.FirstOrDefault(x=>x.Id==c.Id);if(row!=null){row.LastReadSequence=Selected.LastReadSequence;row.UnreadCount=0;}if(Side=="me") await LoadUnreadAsync();}
    }
    public sealed class JsonRead { public long Sequence { get;set; } }
    public async Task LoadReceptionistsAsync() {var rows=await Request<List<SupportReceptionistDto>>("/api/support/admin/receptionists");if(rows!=null) Receptionists=rows;}
    public void NewConversation() {ResetMessages();Selected=null;Events=[];Error="";}
    private async Task ClearCoreAsync() {
        ++_generation;_lifetime?.Cancel();_lifetime?.Dispose();_lifetime=null;
        var hub=_hub;_hub=null;if(hub!=null) await hub.DisposeAsync();
        _owner=null;NewConversation();Receptionists=[];Inbox=new([],0,1,20);UnreadCount=0;
        Error="";Settings=null;ConnectionStatus="Chưa kết nối";SessionCleared?.Invoke();
    }
    public async Task StopAsync() {await _lifecycle.WaitAsync();try {await ClearCoreAsync();} finally {_lifecycle.Release();}}
    public async ValueTask DisposeAsync() {if(_disposed)return;_disposed=true;if(_subscribed) user.OnChange-=AccountChanged;await StopAsync();}
}
