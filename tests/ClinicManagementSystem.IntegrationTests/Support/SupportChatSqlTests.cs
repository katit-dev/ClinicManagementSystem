using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.DTOs.Support;
using ClinicManagementSystem.Application.Helpers;
using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.MsSql;

namespace ClinicManagementSystem.IntegrationTests.Support;

public class SupportChatSqlTests : IAsyncLifetime
{
    private readonly string _database="ClinicSupportChatTests_"+Guid.NewGuid().ToString("N");
    private string _connection="";
    private MsSqlContainer? _container;
    private ChatFactory _factory=null!;
    private const string Key="SupportChat-Isolated-Test-Key-2026-At-Least32Characters";
    private sealed class Baseline(DbContextOptions<ClinicManagementDbContext> options):ClinicManagementDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder builder) {
            base.OnModelCreating(builder);builder.Ignore<SupportMessage>();builder.Ignore<SupportConversationEvent>();builder.Ignore<SupportConversation>();
        }
    }
    private sealed class ChatFactory(string connection):WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:ClinicDb",connection);
            builder.UseSetting("Jwt:Key",Key);builder.UseSetting("Jwt:Issuer","SupportTests");builder.UseSetting("Jwt:Audience","SupportTests");
            builder.ConfigureTestServices(s=>s.Configure<SupportChatOptions>(o=>{o.Enabled=true;o.SupportHours="Giờ hành chính hằng ngày";}));
        }
    }
    private ClinicManagementDbContext Db()=>new(new DbContextOptionsBuilder<ClinicManagementDbContext>().UseSqlServer(_connection).Options);
    public async Task InitializeAsync() {
        if(OperatingSystem.IsWindows()) _connection=$"Server=(localdb)\\MSSQLLocalDB;Database={_database};Integrated Security=true;TrustServerCertificate=true";
        else { _container=new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();await _container.StartAsync();
            var cs=new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_container.GetConnectionString()){InitialCatalog=_database};_connection=cs.ConnectionString; }
        await using(var baseline=new Baseline(new DbContextOptionsBuilder<ClinicManagementDbContext>().UseSqlServer(_connection).Options)) await baseline.Database.EnsureCreatedAsync();
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null && !File.Exists(Path.Combine(root.FullName,"database","007_AddSupportChat.sql"))) root=root.Parent;
        Assert.NotNull(root);
        await using(var db=Db()) {
            await db.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(Path.Combine(root!.FullName,"database","007_AddSupportChat.sql")));
            db.Roles.AddRange(new Role{Name="Patient"},new Role{Name="Receptionist"},new Role{Name="Admin"});
            db.Patients.Add(new Patient{FullName="Unlinked fixture",PatientCode="FIXTURE-UNLINKED",IsActive=true});
            await db.SaveChangesAsync();
        }
        _factory=new ChatFactory(_connection);
    }
    public async Task DisposeAsync() {
        if(_factory!=null) await _factory.DisposeAsync();
        // Only the randomly named database created by this fixture may be removed.
        if(!_database.StartsWith("ClinicSupportChatTests_",StringComparison.Ordinal)) throw new InvalidOperationException();
        await using(var db=Db()) await db.Database.EnsureDeletedAsync();
        if(_container!=null) await _container.DisposeAsync();
    }
    private async Task<(HttpClient Client,int UserId,string Token)> Actor(string role) {
        await using var db=Db();
        var user=new User{Username="support-"+Guid.NewGuid().ToString("N"),PasswordHash="isolated-test-only",FullName=role+" fixture",IsActive=true,CreatedAt=DateTime.UtcNow};
        db.Users.Add(user);await db.SaveChangesAsync();
        db.UserRoles.Add(new UserRole{UserId=user.Id,RoleId=await db.Roles.Where(r=>r.Name==role).Select(r=>r.Id).SingleAsync(),AssignedAt=DateTime.UtcNow});
        if(role=="Patient") db.Patients.Add(new Patient{UserId=user.Id,FullName="Patient fixture",PatientCode="PAT-"+Guid.NewGuid().ToString("N")[..12],IsActive=true,CreatedAt=DateTime.UtcNow});
        await db.SaveChangesAsync();
        var claims=new[]{new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),new Claim(ClaimTypes.Role,role),new Claim(AccountAccessStamp.ClaimType,AccountAccessStamp.Create(user,[role]))};
        var token=new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("SupportTests","SupportTests",claims,expires:DateTime.UtcNow.AddMinutes(20),
            signingCredentials:new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)),SecurityAlgorithms.HmacSha256)));
        var client=_factory.CreateClient(new WebApplicationFactoryClientOptions{BaseAddress=new Uri("https://localhost"),AllowAutoRedirect=false});
        client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",token);
        return(client,user.Id,token);
    }
    private static async Task<T> Body<T>(HttpResponseMessage response) {
        var raw=await response.Content.ReadAsStringAsync();Assert.True(response.IsSuccessStatusCode,$"{response.StatusCode}: {raw}");
        var dto=JsonSerializer.Deserialize<HttpResponseData<T>>(raw,new JsonSerializerOptions(JsonSerializerDefaults.Web));Assert.NotNull(dto);Assert.NotNull(dto.Content);return dto.Content!;
    }
    private static async Task<SupportStaffConversation> Summary(HttpClient client,long id) {
        var page=await Body<SupportPage<SupportStaffConversation>>(await client.GetAsync("/api/support/admin/conversations?pageSize=100"));return page.Items.Single(c=>c.Id==id);
    }
    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData("   ")]
    public async Task OptionalSubject_DefaultsWithoutRejectingFirstMessage(string? subject)
    {
        var (p,_,_)=await Actor("Patient");
        var created=await Body<SupportCreatedDto>(await p.PostAsJsonAsync("/api/support/me/conversations",
            new SupportCreateRequest{Subject=subject,Content="Cần hỗ trợ",ClientMessageId=Guid.NewGuid()}));
        Assert.Equal("Hỗ trợ chung",created.Conversation.Subject);Assert.Equal(2,created.Messages.Count);
    }
    [Fact]
    public async Task SubjectChange_IsOwnerOnlyAtomicAndDoesNotAlterMessagesAssignmentOrReadPositions()
    {
        var (p,_,_)=await Actor("Patient");var (other,_,_o)=await Actor("Patient");
        var (staff,sid,_s)=await Actor("Receptionist");var (admin,_,_a)=await Actor("Admin");
        var original=new SupportCreateRequest{Subject="Đặt lịch khám",Content="Tin đầu",ClientMessageId=Guid.NewGuid()};
        var created=await Body<SupportCreatedDto>(await p.PostAsJsonAsync("/api/support/me/conversations",original));var id=created.Conversation.Id;
        await Body<SupportStaffConversation>(await staff.PostAsJsonAsync($"/api/support/reception/conversations/{id}/claim",new SupportActionRequest()));
        await Body<JsonElement>(await p.PostAsJsonAsync($"/api/support/me/conversations/{id}/read",new SupportReadRequest{Sequence=2}));
        var endpoint=$"/api/support/me/conversations/{id}/subject";
        var change=new SupportSubjectRequest{Subject="  Dịch vụ & chi phí  ",ExpectedSubject="Đặt lịch khám"};
        Assert.Equal(HttpStatusCode.NotFound,(await other.PostAsJsonAsync(endpoint,change)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await staff.PostAsJsonAsync(endpoint,change)).StatusCode);
        var renamed=await Body<SupportPatientConversation>(await p.PostAsJsonAsync(endpoint,change));
        Assert.Equal("Dịch vụ & chi phí",renamed.Subject);Assert.Equal(2,renamed.LastReadSequence);Assert.Equal(2,renamed.LastMessageSequence);
        await Body<SupportPatientConversation>(await p.PostAsJsonAsync(endpoint,change)); // Response lost: retry is harmless.
        Assert.Equal(HttpStatusCode.Conflict,(await p.PostAsJsonAsync(endpoint,new SupportSubjectRequest{Subject="Khác",ExpectedSubject="Đặt lịch khám"})).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await p.PostAsJsonAsync(endpoint,new SupportSubjectRequest{Subject=new string('x',151),ExpectedSubject=renamed.Subject})).StatusCode);
        var retried=await Body<SupportCreatedDto>(await p.PostAsJsonAsync("/api/support/me/conversations",original));
        Assert.Equal(id,retried.Conversation.Id);Assert.Equal(renamed.Subject,retried.Conversation.Subject);
        var summary=await Summary(admin,id);Assert.Equal(sid,summary.AssignedUserId);Assert.Equal((byte)1,summary.Status);
        await using(var db=Db()) {
            Assert.Equal(2,await db.SupportMessages.CountAsync(m=>m.ConversationId==id));
            Assert.Equal(1,await db.SupportMessages.CountAsync(m=>m.ConversationId==id && m.MessageKind==1));
            Assert.Equal(2,await db.SupportConversationEvents.CountAsync(e=>e.ConversationId==id));
        }
        await Body<SupportPatientConversation>(await p.PostAsJsonAsync($"/api/support/me/conversations/{id}/close",new SupportActionRequest()));
        Assert.Equal(HttpStatusCode.Conflict,(await p.PostAsJsonAsync(endpoint,new SupportSubjectRequest{Subject="Đổi sau đóng",ExpectedSubject=renamed.Subject})).StatusCode);
    }
    [Fact]
    public async Task ConcurrentSubjectEdits_OnlyOneDifferentTitleWins()
    {
        var (p,_,_)=await Actor("Patient");
        var created=await Body<SupportCreatedDto>(await p.PostAsJsonAsync("/api/support/me/conversations",new SupportCreateRequest{Content="Câu hỏi",ClientMessageId=Guid.NewGuid()}));
        var endpoint=$"/api/support/me/conversations/{created.Conversation.Id}/subject";
        var responses=await Task.WhenAll(p.PostAsJsonAsync(endpoint,new SupportSubjectRequest{Subject="Đặt lịch khám",ExpectedSubject="Hỗ trợ chung"}),
            p.PostAsJsonAsync(endpoint,new SupportSubjectRequest{Subject="Thủ tục khám",ExpectedSubject="Hỗ trợ chung"}));
        Assert.Single(responses,r=>r.StatusCode==HttpStatusCode.OK);Assert.Single(responses,r=>r.StatusCode==HttpStatusCode.Conflict);
    }
    [Fact]
    public async Task EndToEnd_DurableHistoryPrivacyAssignmentRetryReadAndClose()
    {
        var (p,pid,_)=await Actor("Patient");var (a,aid,_a)=await Actor("Receptionist");var (b,bid,_b)=await Actor("Receptionist");var (admin,_,_d)=await Actor("Admin");var (other,_,_o)=await Actor("Patient");
        var request=new SupportCreateRequest{Subject="Hỏi lịch khám",Content="Tin đầu",ClientMessageId=Guid.NewGuid()};
        var created=await Body<SupportCreatedDto>(await p.PostAsJsonAsync("/api/support/me/conversations",request));var id=created.Conversation.Id;
        Assert.Equal(2,created.Messages.Count);Assert.Equal((byte)2,created.Messages[1].SenderSide);Assert.Equal((byte)1,created.Messages[1].MessageKind);
        Assert.Equal(id,(await Body<SupportCreatedDto>(await p.PostAsJsonAsync("/api/support/me/conversations",request))).Conversation.Id);
        await using(var db=Db()) {
            var row=await db.SupportConversations.FindAsync(id);Assert.NotEqual(pid,row!.PatientId);
            Assert.Equal(1,await db.SupportMessages.CountAsync(m=>m.ConversationId==id && m.MessageKind==1));
        }
        Assert.Equal(HttpStatusCode.NotFound,(await other.GetAsync($"/api/support/me/conversations/{id}/messages")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await a.GetAsync($"/api/support/reception/conversations/{id}/messages")).StatusCode);
        var waiting=await a.GetStringAsync("/api/support/reception/conversations?scope=waiting");Assert.DoesNotContain("Tin đầu",waiting);
        var claimed=await Body<SupportStaffConversation>(await a.PostAsJsonAsync($"/api/support/reception/conversations/{id}/claim",new SupportActionRequest()));Assert.Equal(aid,claimed.AssignedUserId);
        Assert.Equal(HttpStatusCode.Conflict,(await b.PostAsJsonAsync($"/api/support/reception/conversations/{id}/claim",new SupportActionRequest())).StatusCode);
        var message=new SupportMessageRequest{Content="Nhân viên trả lời",ClientMessageId=Guid.NewGuid()};
        var saved=await Body<SupportStaffMessage>(await a.PostAsJsonAsync($"/api/support/reception/conversations/{id}/messages",message));
        Assert.Equal(saved.Id,(await Body<SupportStaffMessage>(await a.PostAsJsonAsync($"/api/support/reception/conversations/{id}/messages",message))).Id);
        var patientText=await p.GetStringAsync($"/api/support/me/conversations/{id}/messages");
        Assert.DoesNotContain("senderUserId",patientText);Assert.DoesNotContain("Receptionist fixture",patientText);
        Assert.Equal(HttpStatusCode.NotFound,(await admin.GetAsync($"/api/support/admin/conversations/{id}/messages")).StatusCode);
        await Body<JsonElement>(await p.PostAsJsonAsync($"/api/support/me/conversations/{id}/read",new SupportReadRequest{Sequence=3}));
        await Body<SupportPatientMessage>(await p.PostAsJsonAsync($"/api/support/me/conversations/{id}/messages",new SupportMessageRequest{Content="Tin bệnh nhân 4",ClientMessageId=Guid.NewGuid()}));
        await Body<SupportStaffMessage>(await a.PostAsJsonAsync($"/api/support/reception/conversations/{id}/messages",new SupportMessageRequest{Content="Tin nhân viên 5",ClientMessageId=Guid.NewGuid()}));
        var unread=await p.GetStringAsync("/api/support/me/unread-summary");Assert.Contains("\"unreadCount\":1",unread);
        Assert.Equal(HttpStatusCode.BadRequest,(await p.PostAsJsonAsync($"/api/support/me/conversations/{id}/read",new SupportReadRequest{Sequence=999})).StatusCode);
        var summary=await Summary(admin,id);
        var transferred=await Body<SupportStaffConversation>(await admin.PostAsJsonAsync($"/api/support/admin/conversations/{id}/transfer",new SupportActionRequest{ReceptionistUserId=bid,Reason="Đổi ca",ExpectedVersion=summary.Version}));
        Assert.Equal(0,transferred.LastReadSequence);
        Assert.Equal(HttpStatusCode.NotFound,(await a.PostAsJsonAsync($"/api/support/reception/conversations/{id}/messages",new SupportMessageRequest{Content="Không được lưu",ClientMessageId=Guid.NewGuid()})).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await a.GetAsync($"/api/support/reception/conversations/{id}/messages")).StatusCode);
        await Body<SupportCursorPage<SupportStaffMessage>>(await b.GetAsync($"/api/support/reception/conversations/{id}/messages"));
        var events=await Body<SupportCursorPage<SupportEventDto>>(await admin.GetAsync($"/api/support/admin/conversations/{id}/events"));
        Assert.Equal(5,events.Items.Single(e=>e.EventType==3).MessageSequenceAtEvent);
        await Body<SupportStaffConversation>(await b.PostAsJsonAsync($"/api/support/reception/conversations/{id}/close",new SupportActionRequest()));
        Assert.Equal(HttpStatusCode.Conflict,(await p.PostAsJsonAsync($"/api/support/me/conversations/{id}/messages",new SupportMessageRequest{Content="Không lưu sau đóng",ClientMessageId=Guid.NewGuid()})).StatusCode);
        Assert.NotEmpty((await Body<SupportCursorPage<SupportPatientMessage>>(await p.GetAsync($"/api/support/me/conversations/{id}/messages"))).Items);
        Assert.Equal(HttpStatusCode.NotFound,(await a.GetAsync($"/api/support/reception/conversations/{id}/messages")).StatusCode);
    }
    [Fact]
    public async Task ConcurrentClaims_OnlyOneWinner_AndConcurrentCreateHasOneAcknowledgement()
    {
        var (p,_,_p)=await Actor("Patient");var (a,_,_a)=await Actor("Receptionist");var (b,_,_b)=await Actor("Receptionist");
        var req=new SupportCreateRequest{Content="Concurrent",ClientMessageId=Guid.NewGuid()};
        var creates=await Task.WhenAll(p.PostAsJsonAsync("/api/support/me/conversations",req),p.PostAsJsonAsync("/api/support/me/conversations",req));
        var first=await Body<SupportCreatedDto>(creates[0]);Assert.Equal(first.Conversation.Id,(await Body<SupportCreatedDto>(creates[1])).Conversation.Id);
        var id=first.Conversation.Id;
        var claims=await Task.WhenAll(a.PostAsJsonAsync($"/api/support/reception/conversations/{id}/claim",new SupportActionRequest()),b.PostAsJsonAsync($"/api/support/reception/conversations/{id}/claim",new SupportActionRequest()));
        Assert.Single(claims,x=>x.StatusCode==HttpStatusCode.OK);Assert.Single(claims,x=>x.StatusCode==HttpStatusCode.Conflict);
        await using var db=Db();Assert.Equal(2,await db.SupportMessages.CountAsync(m=>m.ConversationId==id));Assert.Equal(1,await db.SupportConversationEvents.CountAsync(e=>e.ConversationId==id && e.EventType==1));
    }
    [Fact]
    public async Task SendAndCloseRace_NeverPersistsBeyondClosingBoundary()
    {
        var (p,_,_p)=await Actor("Patient");var (a,_,_a)=await Actor("Receptionist");
        var created=await Body<SupportCreatedDto>(await p.PostAsJsonAsync("/api/support/me/conversations",new SupportCreateRequest{Content="Race",ClientMessageId=Guid.NewGuid()}));
        var id=created.Conversation.Id;
        await Body<SupportStaffConversation>(await a.PostAsJsonAsync($"/api/support/reception/conversations/{id}/claim",new SupportActionRequest()));
        var results=await Task.WhenAll(
            a.PostAsJsonAsync($"/api/support/reception/conversations/{id}/messages",new SupportMessageRequest{Content="Concurrent send",ClientMessageId=Guid.NewGuid()}),
            p.PostAsJsonAsync($"/api/support/me/conversations/{id}/close",new SupportActionRequest()));
        Assert.True(results[0].StatusCode is HttpStatusCode.OK or HttpStatusCode.Conflict);
        Assert.Equal(HttpStatusCode.OK,results[1].StatusCode);
        await using var db=Db();var c=await db.SupportConversations.FindAsync(id);
        Assert.Equal((byte)2,c!.Status);
        Assert.Equal(c.LastMessageSequence,(await db.SupportConversationEvents.SingleAsync(e=>e.ConversationId==id && e.EventType==4)).MessageSequenceAtEvent);
        Assert.Equal(c.LastMessageSequence,await db.SupportMessages.Where(m=>m.ConversationId==id).MaxAsync(m=>m.MessageSequence));
    }
    [Fact]
    public async Task DatabaseConstraintsAndAppendOnlyProtection_RejectInvalidSystemMessages()
    {
        var (p,uid,_p)=await Actor("Patient");
        var created=await Body<SupportCreatedDto>(await p.PostAsJsonAsync("/api/support/me/conversations",new SupportCreateRequest{Content="Constraints",ClientMessageId=Guid.NewGuid()}));
        var id=created.Conversation.Id;
        await using var db=Db();
        var duplicate=new SupportMessage{ConversationId=id,MessageSequence=3,SenderSide=2,MessageKind=1,SenderNameSnapshot="System",Content="Duplicate",ClientMessageId=Guid.NewGuid(),SentAt=DateTime.UtcNow};
        db.SupportMessages.Add(duplicate);await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());db.ChangeTracker.Clear();
        db.SupportMessages.Add(new(){ConversationId=id,MessageSequence=3,SenderSide=0,MessageKind=0,SenderUserId=null,SenderNameSnapshot="Patient",Content="Missing account",ClientMessageId=Guid.NewGuid(),SentAt=DateTime.UtcNow});
        await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());db.ChangeTracker.Clear();
        var original=await db.SupportMessages.FirstAsync(m=>m.ConversationId==id);original.Content="Must not overwrite history";
        await Assert.ThrowsAsync<InvalidOperationException>(()=>db.SaveChangesAsync());
    }
    [Fact]
    public async Task ReadCursorIsSharedAndMonotonic_SystemAndOwnMessagesAreNotUnread()
    {
        var (p,_,token)=await Actor("Patient");var (a,_,_a)=await Actor("Receptionist");
        var c=await Body<SupportCreatedDto>(await p.PostAsJsonAsync("/api/support/me/conversations",new SupportCreateRequest{Content="Unread",ClientMessageId=Guid.NewGuid()}));
        var id=c.Conversation.Id;Assert.Contains("\"unreadCount\":0",await p.GetStringAsync("/api/support/me/unread-summary"));
        await Body<SupportStaffConversation>(await a.PostAsJsonAsync($"/api/support/reception/conversations/{id}/claim",new SupportActionRequest()));
        await Body<SupportStaffMessage>(await a.PostAsJsonAsync($"/api/support/reception/conversations/{id}/messages",new SupportMessageRequest{Content="Reply",ClientMessageId=Guid.NewGuid()}));
        await Body<JsonElement>(await p.PostAsJsonAsync($"/api/support/me/conversations/{id}/read",new SupportReadRequest{Sequence=3}));
        await Body<JsonElement>(await p.PostAsJsonAsync($"/api/support/me/conversations/{id}/read",new SupportReadRequest{Sequence=1}));
        using var second=_factory.CreateClient(new WebApplicationFactoryClientOptions{BaseAddress=new("https://localhost")});
        second.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",token);
        var page=await Body<SupportPage<SupportPatientConversation>>(await second.GetAsync("/api/support/me/conversations"));
        Assert.Equal(3,Assert.Single(page.Items).LastReadSequence);Assert.Equal(0,Assert.Single(page.Items).UnreadCount);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RealSignalRConnection_ReceivesInvalidationAfterCommit(bool webSockets)
    {
        var (p,_,_p)=await Actor("Patient");var (_,_,token)=await Actor("Receptionist");
        await using var hub=new HubConnectionBuilder().WithUrl("https://localhost/hubs/support-chat",o=>{
            o.AccessTokenProvider=()=>Task.FromResult<string?>(token);o.Transports=webSockets?HttpTransportType.WebSockets:HttpTransportType.LongPolling;
            o.HttpMessageHandlerFactory=_=>_factory.Server.CreateHandler();
            if(webSockets) o.WebSocketFactory=(context,ct)=>{
                var client=_factory.Server.CreateWebSocketClient();
                client.ConfigureRequest=request=>request.Headers.Authorization="Bearer "+token;
                return new ValueTask<System.Net.WebSockets.WebSocket>(client.ConnectAsync(context.Uri,ct));
            };
        }).Build();
        var received=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        hub.On("SupportInboxChanged",()=>received.TrySetResult());await hub.StartAsync();
        var created=await Body<SupportCreatedDto>(await p.PostAsJsonAsync("/api/support/me/conversations",new SupportCreateRequest{Content="Realtime",ClientMessageId=Guid.NewGuid()}));
        await received.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await using var db=Db();Assert.Equal(2,await db.SupportMessages.CountAsync(m=>m.ConversationId==created.Conversation.Id));
    }
}
