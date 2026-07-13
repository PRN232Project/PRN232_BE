using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private static readonly Dictionary<string, string> _userConnections = new();
        private static readonly object _lock = new();

        public override Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (userId != null)
            {
                lock (_lock)
                {
                    _userConnections[userId] = Context.ConnectionId;
                }
            }
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            if (userId != null)
            {
                lock (_lock)
                {
                    _userConnections.Remove(userId);
                }
            }
            return base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Gửi thông báo tới user cụ thể (được gọi từ service/controller)
        /// </summary>
        public static async Task SendNotificationToUser(
            IHubContext<NotificationHub> hubContext,
            string userId,
            string type,
            string title,
            string message,
            object? data = null)
        {
            await hubContext.Clients.User(userId).SendAsync("ReceiveNotification", new
            {
                id = Guid.NewGuid(),
                type,
                title,
                message,
                data,
                createdAt = DateTime.UtcNow
            });
        }
    }
}
