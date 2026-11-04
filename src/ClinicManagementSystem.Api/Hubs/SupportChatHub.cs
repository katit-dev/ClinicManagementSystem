using ClinicManagementSystem.Application.DTOs.Support;
using ClinicManagementSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace ClinicManagementSystem.Api.Hubs;

[Authorize(Roles="Patient,Receptionist,Admin")]
public sealed class SupportChatHub(IAccountAccessValidationService accounts,IOptions<SupportChatOptions> options) : Hub
{
    // No client-controlled groups and no send/write methods bypassing the REST service.
    public override async Task OnConnectedAsync()
    {
        if(!options.Value.Enabled || Context.User==null || !await accounts.IsValidAsync(Context.User)) { Context.Abort();return; }
        await Groups.AddToGroupAsync(Context.ConnectionId,"support:user:"+Context.UserIdentifier);
        if(Context.User.IsInRole("Receptionist")) await Groups.AddToGroupAsync(Context.ConnectionId,"support:reception");
        if(Context.User.IsInRole("Admin")) await Groups.AddToGroupAsync(Context.ConnectionId,"support:admin");
        await base.OnConnectedAsync();
    }
}
public sealed class SupportChatNotifier(IHubContext<SupportChatHub> hub) : ISupportChatNotifier
{
    public async Task ChangedAsync(long conversationId,int patientUserId,int? previousAssignee,int? currentAssignee)
    {
        var users=new[]{patientUserId,previousAssignee??0,currentAssignee??0}.Where(x=>x>0).Distinct()
            .Select(x=>"support:user:"+x).ToArray();
        // IDs only: stale role/group membership never exposes bodies or patient metadata.
        await Task.WhenAll(
            hub.Clients.Groups(users).SendAsync("SupportConversationChanged",new { ConversationId=conversationId }),
            hub.Clients.Group("support:reception").SendAsync("SupportInboxChanged"),
            hub.Clients.Group("support:admin").SendAsync("SupportDispatchChanged"));
    }
}
