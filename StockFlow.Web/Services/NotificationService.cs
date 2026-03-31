using Microsoft.AspNetCore.SignalR;
using Serilog;
using StockFlow.Web.Exceptions;
using StockFlow.Web.Hubs;
using StockFlow.Web.Services.Interfaces;

namespace StockFlow.Web.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IHubContext<StockFlowHub> _hub;

        public NotificationService(IHubContext<StockFlowHub> hub)
        {
            _hub = hub;
        }

        public async Task NotifyProcessingCompleteAsync(int shipmentId, int processedItemId, CancellationToken ct = default)
        {
            try
            {
                await _hub.Clients.Group("Managers").SendAsync("ProcessingComplete", new
                {
                    shipmentId,
                    processedItemId,
                    message = $"Shipment {shipmentId} has been processed.",
                    timestamp = DateTime.UtcNow
                }, ct);

                Log.Information("Notified Managers of processing complete for shipment {ShipmentId}", shipmentId);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to send ProcessingComplete notification for shipment {ShipmentId}", shipmentId);
            }
        }

        public async Task NotifyApprovalRequiredAsync(int processedItemId, CancellationToken ct = default)
        {
            try
            {
                await _hub.Clients.Group("Managers").SendAsync("ApprovalRequired", new
                {
                    processedItemId,
                    message = $"Processed item {processedItemId} is awaiting approval.",
                    timestamp = DateTime.UtcNow
                }, ct);

                Log.Information("Notified Managers of approval required for processed item {ProcessedItemId}", processedItemId);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to send ApprovalRequired notification for processed item {ProcessedItemId}", processedItemId);
            }
        }

        public async Task NotifyApprovalDecisionAsync(int processedItemId, string decision, CancellationToken ct = default)
        {
            try
            {
                await _hub.Clients.Group("Staff").SendAsync("ApprovalDecision", new
                {
                    processedItemId,
                    decision,
                    message = $"Processed item {processedItemId} has been {decision.ToLower()}.",
                    timestamp = DateTime.UtcNow
                }, ct);

                Log.Information("Notified Staff of {Decision} for processed item {ProcessedItemId}", decision, processedItemId);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to send ApprovalDecision notification for processed item {ProcessedItemId}", processedItemId);
            }
        }

        public async Task NotifyStaleShipmentAsync(int shipmentId, CancellationToken ct = default)
        {
            try
            {
                await _hub.Clients.Group("Managers").SendAsync("StaleShipment", new
                {
                    shipmentId,
                    message = $"Shipment {shipmentId} has been pending for too long.",
                    timestamp = DateTime.UtcNow
                }, ct);

                Log.Information("Notified Managers of stale shipment {ShipmentId}", shipmentId);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to send StaleShipment notification for shipment {ShipmentId}", shipmentId);
            }
        }

        public async Task NotifyUserAsync(int userId, string message, CancellationToken ct = default)
        {
            try
            {
                await _hub.Clients.Group($"User_{userId}").SendAsync("UserNotification", new
                {
                    message,
                    timestamp = DateTime.UtcNow
                }, ct);

                Log.Information("Notified user {UserId} with message: {Message}", userId, message);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to send notification to user {UserId}", userId);
            }
        }
    }
}