using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Auth;
using ClinicManagementSystem.Application.DTOs.Support;
using ClinicManagementSystem.Web.Services;
using ClinicManagementSystem.Web.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;

namespace ClinicManagementSystem.UnitTests.Web;

public class SupportChatWebTests
{
    private static void Set(object target,string property,object? value)=>target.GetType().GetProperty(property,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(target,value);
    private static (SupportChatStateService State,UserStateService User) Create(HttpMessageHandler? handler=null,IConfiguration? configuration=null)
    {
        var factory=new Mock<IHttpClientFactory>();factory.Setup(f=>f.CreateClient(It.IsAny<string>())).Returns(()=>new HttpClient(handler??new Handler(),false){BaseAddress=new("https://fixture.invalid")});
        var user=new UserStateService(null!,factory.Object,null!,new CustomAuthenticationStateProvider());
        Set(user,"CurrentUser",new AuthUserDTO{Id=11});Set(user,"AccessToken","fixture-token");
        var state=new SupportChatStateService(user,factory.Object,configuration??new ConfigurationBuilder().Build());
        typeof(SupportChatStateService).GetField("_owner",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(state,11);
        return(state,user);
    }
    private sealed class Handler:HttpMessageHandler
    {
        public Func<HttpRequestMessage,Task<HttpResponseMessage>> Respond {get;set;}=_=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=JsonContent.Create(new HttpResponseData<SupportPage<SupportChatStateService.Conversation>>{StatusCode=200,Content=new([],0,1,20)})});
        public List<string> Requests {get;}=[];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct){Requests.Add(request.RequestUri!.PathAndQuery);return Respond(request);}
    }
    [Fact]
    public async Task LateResponseAfterLogout_DoesNotRestorePreviousAccountHistory()
    {
        var pending=new TaskCompletionSource<HttpResponseMessage>();var handler=new Handler{Respond=_=>pending.Task};
        var (state,user)=Create(handler);var loading=state.LoadInboxAsync();
        Set(user,"CurrentUser",null);await state.StopAsync();
        pending.SetResult(new(HttpStatusCode.OK){Content=JsonContent.Create(new HttpResponseData<SupportPage<SupportChatStateService.Conversation>>{StatusCode=200,Content=new([new(){Id=1,Subject="Private previous account"}],1,1,20)})});
        await loading;Assert.Empty(state.Inbox.Items);Assert.Null(state.Selected);
    }
    [Fact]
    public async Task HungMessageRequest_LeavesLoadingAndAllowsSuccessfulRetry()
    {
        var pending=new TaskCompletionSource<HttpResponseMessage>();
        var handler=new Handler{Respond=_=>pending.Task};
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"SupportChat:RequestTimeoutSeconds","1"}}).Build();
        var(state,_)=Create(handler,config);Set(state,"Side","reception");state.Select(new(){Id=1,Status=1,LastMessageSequence=1});
        var loading=state.LoadMessagesAsync(reset:true);
        try {
            await loading.WaitAsync(TimeSpan.FromSeconds(3));
            Assert.False(state.LoadingMessages);Assert.NotEmpty(state.MessageError);
            handler.Respond=_=>Task.FromResult(PatientMessages(new SupportPatientMessage(1,1,0,0,"Bệnh nhân","Tải lại thành công",DateTime.UtcNow,Guid.NewGuid())));
            await state.LoadMessagesAsync(reset:true);
            Assert.True(state.MessagesLoaded);Assert.Empty(state.MessageError);Assert.Single(state.Messages);
        }finally{pending.TrySetResult(PatientMessages());await loading;}
    }
    [Fact]
    public async Task FailedSend_DoesNotOptimisticallyAppendMessage()
    {
        var handler=new Handler{Respond=_=>throw new HttpRequestException("fixture")};var (state,_)=Create(handler);
        state.Select(new(){Id=1,Status=1});Assert.False(await state.SendAsync("Pending",Guid.NewGuid()));
        Assert.Empty(state.Messages);Assert.Contains("Chưa xác nhận",state.Error);
    }
    private static HttpResponseMessage PatientMessages(params SupportPatientMessage[] messages)=>new(HttpStatusCode.OK){Content=JsonContent.Create(
        new HttpResponseData<SupportCursorPage<SupportPatientMessage>>{StatusCode=200,Content=new(messages.ToList(),false,null)})};
    [Fact]
    public async Task PatientApiMessageContract_LoadsWithoutInternalSenderIdentity()
    {
        var handler=new Handler{Respond=_=>Task.FromResult(PatientMessages(new SupportPatientMessage(1,1,1,0,"Hỗ trợ phòng khám","Tin trả lời",DateTime.UtcNow,Guid.NewGuid())))};
        var(state,_)=Create(handler);state.Select(new(){Id=1,Status=1,LastMessageSequence=1});
        await state.LoadMessagesAsync(reset:true);
        Assert.Single(state.Messages);Assert.Equal("Tin trả lời",state.Messages[0].Content);
    }
    [Fact]
    public async Task ReopenPatientWidget_RefreshesCachedConversationBeforeShowingLatestReply()
    {
        var c=new SupportChatStateService.Conversation{Id=1,Status=1,Subject="Đặt lịch khám",LastMessageSequence=3,UnreadCount=1};
        var handler=new Handler{Respond=r=>Task.FromResult(r.RequestUri!.AbsolutePath.EndsWith("/messages")?
            PatientMessages(new SupportPatientMessage(3,3,1,0,"Hỗ trợ phòng khám","Tin mới trong lúc đóng khung",DateTime.UtcNow,Guid.NewGuid())):
            new(HttpStatusCode.OK){Content=JsonContent.Create(new HttpResponseData<SupportPage<SupportChatStateService.Conversation>>{StatusCode=200,Content=new([c],1,1,20)})})};
        var(state,_)=Create(handler);state.Select(c);
        var widget=new PatientSupportWidget();Set(widget,"Chat",state);
        await (Task)typeof(PatientSupportWidget).GetMethod("ToggleAsync",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(widget,null)!;
        Assert.Contains(state.Messages,m=>m.Sequence==3 && m.Content=="Tin mới trong lúc đóng khung");
        Assert.DoesNotContain(handler.Requests,p=>p.EndsWith("/read"));
    }
    [Fact]
    public async Task OlderResetResponse_CannotReplaceTheSameConversationSelectedAgain()
    {
        var pending=new TaskCompletionSource<HttpResponseMessage>();var handler=new Handler{Respond=_=>pending.Task};
        var(state,_)=Create(handler);state.Select(new(){Id=1,Status=1});var oldLoad=state.LoadMessagesAsync(reset:true);
        state.NewConversation();state.Select(new(){Id=1,Status=1});
        Set(state,"Messages",new List<SupportStaffMessage>{new(9,9,1,0,"Hỗ trợ phòng khám","Đã tải ở lượt mở mới",DateTime.UtcNow,Guid.NewGuid(),null)});
        pending.SetResult(PatientMessages(new SupportPatientMessage(1,1,0,0,"Bạn","Dữ liệu của lượt mở cũ",DateTime.UtcNow,Guid.NewGuid())));
        await oldLoad;
        Assert.Single(state.Messages);Assert.Equal(9,state.Messages[0].Sequence);
    }
    [Fact]
    public async Task HistoryLoadFailure_StaysVisibleAfterSuccessfulInboxRefresh()
    {
        var handler=new Handler{Respond=r=>r.RequestUri!.AbsolutePath.EndsWith("/messages")?throw new HttpRequestException("offline"):
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=JsonContent.Create(new HttpResponseData<SupportPage<SupportChatStateService.Conversation>>{StatusCode=200,Content=new([new(){Id=1,Status=1}],1,1,20)})})};
        var(state,_)=Create(handler);state.Select(new(){Id=1,Status=1});
        var pending=state.LoadMessagesAsync(reset:true);await pending;
        Assert.NotEmpty(state.MessageError);Assert.False(state.MessagesLoaded);Assert.False(state.LoadingMessages);
        await state.LoadInboxAsync();Assert.NotEmpty(state.MessageError);
    }
    [Fact]
    public async Task LatestPageHasOlderHistory_DoesNotMistakeItForForwardCatchup()
    {
        var handler=new Handler{Respond=_=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=JsonContent.Create(new HttpResponseData<SupportCursorPage<SupportPatientMessage>>{
            StatusCode=200,Content=new([new(31,31,1,0,"Hỗ trợ phòng khám","Tin cuối",DateTime.UtcNow,Guid.NewGuid())],true,31)})})};
        var(state,_)=Create(handler);state.Select(new(){Id=1,Status=1,LastMessageSequence=31});
        await state.LoadMessagesAsync(reset:true);
        Assert.Single(handler.Requests);Assert.True(state.MoreMessages);Assert.True(state.MessagesLoaded);Assert.Single(state.Messages);
    }
    [Fact]
    public async Task KnownConversationWithEmptyResponse_ShowsRetryInsteadOfFalseEmptyHistory()
    {
        var handler=new Handler{Respond=_=>Task.FromResult(PatientMessages())};var(state,_)=Create(handler);
        state.Select(new(){Id=1,Status=1,LastMessageSequence=5});await state.LoadMessagesAsync(reset:true);
        Assert.NotEmpty(state.MessageError);Assert.False(state.MessagesLoaded);
    }
    [Fact]
    public async Task UnreadNotice_OpensClosedConversationOnLaterHistoryPage()
    {
        var handler=new Handler{Respond=r=>Task.FromResult(r.RequestUri!.AbsolutePath.EndsWith("/messages")?
            PatientMessages(new SupportPatientMessage(5,5,1,0,"Hỗ trợ phòng khám","Tin chưa đọc",DateTime.UtcNow,Guid.NewGuid())):
            new(HttpStatusCode.OK){Content=JsonContent.Create(new HttpResponseData<SupportPage<SupportChatStateService.Conversation>>{
                StatusCode=200,Content=r.RequestUri.Query.Contains("page=2&")?
                    new([new(){Id=1,Status=2,UnreadCount=1,LastMessageSequence=5}],21,2,20):
                    new(Enumerable.Range(2,20).Select(i=>new SupportChatStateService.Conversation{Id=i,Status=2}).ToList(),21,1,20)})})};
        var(state,_)=Create(handler);var widget=new PatientSupportWidget();Set(widget,"Chat",state);
        await (Task)typeof(PatientSupportWidget).GetMethod("OpenUnreadAsync",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(widget,null)!;
        Assert.Equal(1,state.Selected!.Id);Assert.Equal((byte)2,state.Selected.Status);Assert.Single(state.Messages);
        Assert.DoesNotContain(handler.Requests,p=>p.EndsWith("/read"));
    }
    [Theory] [InlineData(1L,true)] [InlineData(2L,false)]
    public async Task ReadPosition_UsesVisibleSnapshotAndCannotAdvanceAnotherConversation(long visibleConversation,bool expectedRequest)
    {
        long? posted=null;
        var handler=new Handler{Respond=async r=>{
            if(r.Method==HttpMethod.Post){posted=(await r.Content!.ReadFromJsonAsync<SupportReadRequest>())!.Sequence;
                return new(HttpStatusCode.OK){Content=JsonContent.Create(new HttpResponseData<SupportChatStateService.JsonRead>{StatusCode=200,Content=new(){Sequence=posted.Value}})};}
            return new(HttpStatusCode.OK){Content=JsonContent.Create(new HttpResponseData<SupportChatStateService.UnreadSummary>{StatusCode=200,Content=new(){UnreadCount=1}})};
        }};
        var(state,_)=Create(handler);state.Select(new(){Id=1,Status=1});
        Set(state,"Messages",new List<SupportStaffMessage>{new(2,2,2,1,"","Đã xem",DateTime.UtcNow,Guid.NewGuid(),null),new(3,3,1,0,"Hỗ trợ phòng khám","Tin đến sau khi kiểm tra hiển thị",DateTime.UtcNow,Guid.NewGuid(),null)});
        await state.MarkReadAsync(2,visibleConversation);
        Assert.Equal(expectedRequest,posted.HasValue);
        if(expectedRequest){Assert.Equal(2,posted);Assert.Equal(2,state.Selected!.LastReadSequence);Assert.Equal(1,state.UnreadCount);}
    }
    [Fact]
    public async Task ServerRevokesAccess_PrivateStateIsCleared()
    {
        var handler=new Handler{Respond=_=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound))};var (state,_)=Create(handler);
        state.Select(new(){Id=1,Status=1});Set(state,"Messages",new List<SupportStaffMessage>{new(1,1,0,0,"Patient","Private",DateTime.UtcNow,Guid.NewGuid(),11)});
        await state.LoadMessagesAsync();Assert.Null(state.Selected);Assert.Empty(state.Messages);
    }
    [Fact]
    public async Task NewConversationSelectionAndRealtimeNotice_DoNotMarkRead()
    {
        var handler=new Handler();var(state,_)=Create(handler);state.Select(new(){Id=1});
        await state.LoadInboxAsync();Assert.DoesNotContain(handler.Requests,p=>p.EndsWith("/read"));
    }
    [Fact]
    public async Task RenameSubject_KeepsLoadedMessagesAndDoesNotMarkReadOrCreateNewRequest()
    {
        var handler=new Handler{Respond=_=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=JsonContent.Create(new HttpResponseData<SupportChatStateService.Conversation>{StatusCode=200,Content=new(){Id=1,Subject="Đặt lịch khám",Status=1,LastReadSequence=3}})})};
        var(state,_)=Create(handler);state.Select(new(){Id=1,Subject="Hỗ trợ chung",Status=1});
        Set(state,"Messages",new List<SupportStaffMessage>{new(1,1,0,0,"Bạn","Giữ tin cũ",DateTime.UtcNow,Guid.NewGuid(),11)});
        Assert.True(await state.UpdateSubjectAsync("Đặt lịch khám","Hỗ trợ chung"));
        Assert.Equal("Đặt lịch khám",state.Selected!.Subject);Assert.Single(state.Messages);
        Assert.Equal(new[]{"/api/support/me/conversations/1/subject"},handler.Requests);
    }
    [Fact]
    public async Task SuccessfulFirstSend_RemainsVisibleWhenInboxRefreshFails()
    {
        var handler=new Handler{Respond=request=>{
            if(request.Method!=HttpMethod.Post)throw new HttpRequestException("Refresh unavailable");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=JsonContent.Create(new HttpResponseData<SupportCreatedDto>{StatusCode=200,
                Content=new(new(1,"Hỗ trợ chung",0,DateTime.UtcNow,DateTime.UtcNow,DateTime.UtcNow,2,0,0),[
                    new(1,1,0,0,"Bạn","Câu hỏi đã lưu",DateTime.UtcNow,Guid.NewGuid()),new(2,2,2,1,"","Đã nhận",DateTime.UtcNow,Guid.NewGuid())])})});
        }};
        var(state,_)=Create(handler);
        Assert.True(await state.SendAsync("Câu hỏi đã lưu",Guid.NewGuid()));
        Assert.Equal(1,state.Selected!.Id);Assert.Equal(2,state.Messages.Count);
        Assert.Single(handler.Requests,p=>p=="/api/support/me/conversations");
    }
    [Theory] [InlineData("me",(byte)2)] [InlineData("reception",(byte)1)]
    public async Task RenameSubject_ClosedOrStaffDoesNotSendRequest(string side,byte status)
    {
        var handler=new Handler();var(state,_)=Create(handler);Set(state,"Side",side);state.Select(new(){Id=1,Status=status});
        Assert.False(await state.UpdateSubjectAsync("Đặt lịch khám","Hỗ trợ chung"));Assert.Empty(handler.Requests);
    }
    [Theory] [InlineData("welcome")] [InlineData("other")] [InlineData("editor")] [InlineData("closed")] [InlineData("history")] [InlineData("info")] [InlineData("error")] [InlineData("confirm")]
    public async Task PatientRazor_SubjectChoicesAndReadOnlyHistory(string scenario)
    {
        var(state,user)=Create();Set(state,"Settings",new SupportSettingsDto(true,"Giờ hành chính hằng ngày, gồm Thứ Bảy và Chủ Nhật","0389479884",2000));
        if(scenario is "editor" or "closed" or "confirm")state.Select(new(){Id=101,Subject="Dịch vụ & chi phí",Status=(byte)(scenario=="closed"?2:1)});
        var fields=new Dictionary<string,object?>();
        if(scenario=="other"){fields["_topic"]=SupportTopics.Other;fields["_customSubject"]="Nội dung riêng";}
        if(scenario=="editor"){fields["_editingSubject"]=true;fields["_editTopic"]="Dịch vụ & chi phí";}
        if(scenario=="history")fields["_history"]=true;
        if(scenario=="info")fields["_showInfo"]=true;
        if(scenario=="error")Set(state,"Error","Chưa thể kết nối. Vui lòng thử gửi lại yêu cầu của bạn.");
        if(scenario=="confirm")fields["_confirmClose"]=true;
        using var services=new ServiceCollection().AddLogging().AddSingleton(state).AddSingleton(user).AddSingleton<IJSRuntime>(Mock.Of<IJSRuntime>()).AddSingleton<IComponentActivator>(new Activator(fields)).BuildServiceProvider();
        await using var renderer=new HtmlRenderer(services,services.GetRequiredService<ILoggerFactory>());
        var raw=await renderer.Dispatcher.InvokeAsync(async()=>(await renderer.RenderComponentAsync<PatientSupportWidget>()).ToHtmlString());
        var html=WebUtility.HtmlDecode(raw);
        Assert.DoesNotContain("support-floating",html);
        Assert.Contains(scenario=="info"?"aria-expanded=\"true\"":"aria-expanded=\"false\"",raw);
        if(scenario is "welcome" or "other" or "editor"){
            Assert.Equal(10,raw.Split("<option ").Length-1); // Nine topics plus optional placeholder.
            Assert.Contains("không bắt buộc",html);Assert.Contains("Tài khoản & sử dụng website",html);
            Assert.Equal(scenario=="other",html.Contains("maxlength=\"150\""));
        }
        if(scenario=="closed"){Assert.DoesNotContain("support-composer",html);Assert.DoesNotContain("Đổi chủ đề",html);}
        if(scenario=="editor"){Assert.Contains("Lưu chủ đề",html);Assert.DoesNotContain("support-composer",html);}
        var output=Environment.GetEnvironmentVariable("SUPPORT_CHAT_PREVIEW_DIR");
        if(!string.IsNullOrEmpty(output)){Directory.CreateDirectory(output);await File.WriteAllTextAsync(Path.Combine(output,scenario+".html"),raw);}
    }
    [Theory] [InlineData("patient")] [InlineData("reception")] [InlineData("admin")]
    public async Task ActualRazor_RendersActorSpecificInterfaceAndEscapesMessageContent(string actor)
    {
        var(state,user)=Create();Set(state,"Settings",new SupportSettingsDto(true,"Giờ hành chính hằng ngày, gồm Thứ Bảy và Chủ Nhật","0389479884",2000));
        Set(state,"Side",actor=="patient"?"me":actor);
        var row=new SupportChatStateService.Conversation{Id=101,Subject="Hỏi lịch khám tổng quát",PatientName="Nguyễn Minh Anh",PatientCode="BN-0001",Status=1,AssignedName="Lê Thu Hà",AssignedUserId=21,CreatedAt=new(2026,11,2,2,0,0),LastMessageSequence=5};
        Set(state,"Inbox",new SupportPage<SupportChatStateService.Conversation>([row],1,1,20));state.Select(row);
        Set(state,"Messages",new List<SupportStaffMessage>{
            new(1,1,0,0,actor=="patient"?"Bạn":"Nguyễn Minh Anh","Tôi muốn hỏi lịch khám sáng mai.",new(2026,11,2,2,0,0),Guid.NewGuid(),11),
            new(2,2,2,1,"", "Phòng khám đã nhận được yêu cầu của bạn. Giờ hỗ trợ: giờ hành chính hằng ngày, gồm Thứ Bảy và Chủ Nhật.",new(2026,11,2,2,0,1),Guid.NewGuid(),null),
            new(3,3,1,0,actor=="patient"?"Hỗ trợ phòng khám":"Lê Thu Hà","Phòng khám có lịch vào 09:00. Bạn muốn khám chuyên khoa nào?",new(2026,11,2,2,1,0),Guid.NewGuid(),21),
            new(4,4,0,0,actor=="patient"?"Bạn":"Nguyễn Minh Anh","<script>alert('fixture')</script>",new(2026,11,2,2,2,0),Guid.NewGuid(),11)});
        Set(state,"Events",new List<SupportEventDto>{new(1,1,0,"Nguyễn Minh Anh",null,null,null,2,new(2026,11,2,2,0,0)),new(2,2,1,"Lê Thu Hà",null,"Lê Thu Hà",null,2,new(2026,11,2,2,1,0))});
        var activator=new Activator();
        using var services=new ServiceCollection().AddLogging().AddSingleton(state).AddSingleton(user).AddSingleton<IJSRuntime>(Mock.Of<IJSRuntime>()).AddSingleton<IComponentActivator>(activator).BuildServiceProvider();
        await using var renderer=new HtmlRenderer(services,services.GetRequiredService<ILoggerFactory>());
        var raw=await renderer.Dispatcher.InvokeAsync(async()=>actor=="patient"?
            (await renderer.RenderComponentAsync<PatientSupportWidget>()).ToHtmlString():
            (await renderer.RenderComponentAsync<SupportWorkspace>(ParameterView.FromDictionary(new Dictionary<string,object?>{{"IsAdmin",actor=="admin"}}))).ToHtmlString());
        var html=WebUtility.HtmlDecode(raw);
        if(actor=="admin") {Assert.Contains("Điều phối hỗ trợ",html);Assert.DoesNotContain("Tôi muốn hỏi lịch",html);Assert.DoesNotContain("support-composer",html);}
        else {Assert.DoesNotContain("<script>",raw);Assert.Contains("&lt;script&gt;",raw);Assert.Contains("support-composer",html);}
        if(actor=="patient") {Assert.Contains("Lịch sử",html);Assert.DoesNotContain("Lê Thu Hà",html);Assert.DoesNotContain("Tin nhắn tự động",html);Assert.Contains("tel:0389479884",html);}
        var output=Environment.GetEnvironmentVariable("SUPPORT_CHAT_PREVIEW_DIR");
        if(!string.IsNullOrEmpty(output)){Directory.CreateDirectory(output);await File.WriteAllTextAsync(Path.Combine(output,actor+".html"),raw);}
    }
    private sealed class Activator(Dictionary<string,object?>? fields=null):IComponentActivator
    {
        public IComponent CreateInstance(Type type){
            var component=(IComponent)System.Activator.CreateInstance(type)!;
            if(type==typeof(PatientSupportWidget) || type==typeof(SupportWorkspace)){
                type.GetField("_ready",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(component,true);
                if(type==typeof(PatientSupportWidget))type.GetField("_open",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(component,true);
                if(type==typeof(PatientSupportWidget) && fields!=null)foreach(var field in fields)type.GetField(field.Key,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(component,field.Value);
            }
            return component;
        }
    }
}
