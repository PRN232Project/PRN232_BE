using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OnlineLearningPlatformApi.Domain;
using OnlineLearningPlatformApi.Domain.Entities;

namespace API.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly AppDbContext _db;
        // Track connected users: userId -> connectionId
        private static readonly Dictionary<string, string> _userConnections = new();
        private static readonly object _lock = new();

        public ChatHub(AppDbContext db)
        {
            _db = db;
        }

        public override Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (userId != null)
            {
                lock (_lock)
                {
                    _userConnections[userId] = Context.ConnectionId;
                }
                // Notify others this user is online
                Clients.Others.SendAsync("UserOnline", userId);
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
                Clients.Others.SendAsync("UserOffline", userId);
            }
            return base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Gửi tin nhắn realtime tới người nhận, đồng thời lưu vào DB
        /// </summary>
        public async Task SendMessage(string receiverId, string content)
        {
            var senderIdStr = Context.UserIdentifier;
            if (string.IsNullOrEmpty(senderIdStr)) return;

            if (!Guid.TryParse(senderIdStr, out var senderGuid)) return;
            if (!Guid.TryParse(receiverId, out var receiverGuid)) return;

            var sender = await _db.Users.FirstOrDefaultAsync(u => u.UserId == senderGuid);
            if (sender == null) return;

            // Lưu tin nhắn vào DB
            var message = new Message
            {
                MessageId = Guid.NewGuid(),
                SenderId = senderGuid,
                ReceiverId = receiverGuid,
                Content = content,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _db.Messages.Add(message);
            await _db.SaveChangesAsync();

            var payload = new
            {
                messageId = message.MessageId,
                senderId = message.SenderId,
                receiverId = message.ReceiverId,
                content = message.Content,
                sentAt = message.SentAt,
                isRead = message.IsRead,
                senderName = sender.FullName ?? sender.Email
            };

            // Gửi tới người nhận nếu đang online
            string? receiverConnectionId;
            lock (_lock)
            {
                _userConnections.TryGetValue(receiverId, out receiverConnectionId);
            }

            if (receiverConnectionId != null)
            {
                await Clients.Client(receiverConnectionId).SendAsync("ReceiveMessage", payload);
            }

            // Gửi lại cho chính người gửi để confirm
            await Clients.Caller.SendAsync("MessageSent", payload);
        }

        /// <summary>
        /// Đánh dấu đã đọc tin nhắn
        /// </summary>
        public async Task MarkRead(string senderId)
        {
            var currentUserIdStr = Context.UserIdentifier;
            if (string.IsNullOrEmpty(currentUserIdStr)) return;

            if (!Guid.TryParse(currentUserIdStr, out var currentGuid)) return;
            if (!Guid.TryParse(senderId, out var senderGuid)) return;

            var unread = await _db.Messages
                .Where(m => m.SenderId == senderGuid && m.ReceiverId == currentGuid && !m.IsRead)
                .ToListAsync();

            foreach (var msg in unread)
            {
                msg.IsRead = true;
            }

            if (unread.Any())
            {
                await _db.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Lấy danh sách user đang online
        /// </summary>
        public Task<List<string>> GetOnlineUsers()
        {
            lock (_lock)
            {
                return Task.FromResult(_userConnections.Keys.ToList());
            }
        }
    }
}
