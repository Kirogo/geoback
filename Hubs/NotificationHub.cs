using Microsoft.AspNetCore.SignalR;

namespace geoback.Hubs
{
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
            _logger.LogInformation("Client connected: {UserId} with ConnectionId: {ConnectionId}", userId, Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
            _logger.LogInformation("Client disconnected: {UserId} with ConnectionId: {ConnectionId}", userId, Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendNotification(string message)
        {
            try
            {
                await Clients.All.SendAsync("ReceiveNotification", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification");
            }
        }

        public async Task SendPrivateNotification(string userId, string message)
        {
            try
            {
                await Clients.User(userId).SendAsync("ReceiveNotification", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending private notification to user: {UserId}", userId);
            }
        }

        public async Task BroadcastReportUpdate(string message)
        {
            try
            {
                await Clients.All.SendAsync("ReportUpdated", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting report update");
            }
        }
    }
}
