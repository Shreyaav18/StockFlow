using Serilog;
using StockFlow.Web.Services.Interfaces;

namespace StockFlow.Web.BackgroundJobs
{
    public class ShipmentAlertJob
    {
        private readonly IShipmentService _shipmentService;
        private readonly INotificationService _notificationService;
        private readonly IConfiguration _config;

        public ShipmentAlertJob(
            IShipmentService shipmentService,
            INotificationService notificationService,
            IConfiguration config)
        {
            _shipmentService = shipmentService;
            _notificationService = notificationService;
            _config = config;
        }

        public async Task RunAsync()
        {
            try
            {
                var hours = _config.GetValue<int>("App:UnprocessedAlertHours", 24);
                var stale = await _shipmentService.GetStaleAsync(hours);
                var shipmentList = stale.ToList();

                if (!shipmentList.Any())
                {
                    Log.Information("ShipmentAlertJob: No stale shipments found");
                    return;
                }

                Log.Warning("ShipmentAlertJob: Found {Count} stale shipments", shipmentList.Count);

                foreach (var shipment in shipmentList)
                {
                    await _notificationService.NotifyStaleShipmentAsync(shipment.ShipmentId);
                    Log.Warning("Stale shipment alert sent for ShipmentId: {ShipmentId}, received at {ReceivedAt}",
                        shipment.ShipmentId, shipment.ReceivedAt);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ShipmentAlertJob failed during execution");
            }
        }
    }
}