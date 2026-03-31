using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Serilog;

namespace StockFlow.Web.Hubs
{
    [Authorize]
    public class StockFlowHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            try
            {
                var user = Context.User;
                if (user == null) return;

                var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var role = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

                if (!string.IsNullOrEmpty(userId))
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");

                if (!string.IsNullOrEmpty(role))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, role);

                    if (role == "Admin")
                        await Groups.AddToGroupAsync(Context.ConnectionId, "Managers");

                    if (role is "Admin" or "Manager")
                        await Groups.AddToGroupAsync(Context.ConnectionId, "Managers");

                    await Groups.AddToGroupAsync(Context.ConnectionId, "Staff");
                }

                Log.Information("SignalR client connected: {ConnectionId}, User: {UserId}, Role: {Role}",
                    Context.ConnectionId, userId, role);

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error during SignalR OnConnectedAsync for connection {ConnectionId}", Context.ConnectionId);
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                Log.Information("SignalR client disconnected: {ConnectionId}", Context.ConnectionId);

                if (exception != null)
                    Log.Warning(exception, "SignalR disconnection with error for {ConnectionId}", Context.ConnectionId);

                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error during SignalR OnDisconnectedAsync for connection {ConnectionId}", Context.ConnectionId);
            }
        }
    }
}