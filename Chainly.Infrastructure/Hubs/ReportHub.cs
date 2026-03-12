using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Chainly.Infrastructure.Hubs;

[Authorize]
public class ReportHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinProductionLineGroup(int productionLineId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"ProductionLine_{productionLineId}");
    }

    public async Task LeaveProductionLineGroup(int productionLineId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ProductionLine_{productionLineId}");
    }
}
