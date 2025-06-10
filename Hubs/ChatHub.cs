using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MXH_ASP.NET_CORE.Data;
using MXH_ASP.NET_CORE.Models;
using System.Security.Claims;

namespace MXH_ASP.NET_CORE.Hubs
{
    /// <summary>
    /// SignalR Hub xử lý real-time messaging giữa các người dùng
    /// Chỉ cho phép người dùng đã đăng nhập sử dụng
    /// </summary>
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ApplicationDbContext context, ILogger<ChatHub> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Khi người dùng kết nối tới Hub
        /// Thêm user vào group để nhận tin nhắn
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            try
            {
                // Lấy ID người dùng từ Claims
                var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    // Thêm connection vào group theo userId để có thể gửi tin nhắn riêng
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
                    _logger.LogInformation($"User {userId} connected to chat hub with connection {Context.ConnectionId}");
                }
                
                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnConnectedAsync");
            }
        }

        /// <summary>
        /// Khi người dùng ngắt kết nối
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    _logger.LogInformation($"User {userId} disconnected from chat hub");
                }
                
                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnDisconnectedAsync");
            }
        }

        /// <summary>
        /// Gửi tin nhắn text từ một người dùng tới người khác
        /// </summary>
        /// <param name="receiverId">ID người nhận</param>
        /// <param name="content">Nội dung tin nhắn</param>
        public async Task SendMessage(string receiverId, string content)
        {
            try
            {
                // Lấy ID người gửi
                var senderIdString = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(senderIdString) || !int.TryParse(senderIdString, out var senderId))
                {
                    await Clients.Caller.SendAsync("Error", "Không thể xác định người gửi");
                    return;
                }

                if (!int.TryParse(receiverId, out var receiverIdInt))
                {
                    await Clients.Caller.SendAsync("Error", "ID người nhận không hợp lệ");
                    return;
                }

                // Kiểm tra xem hai người có phải là bạn bè không
                var friendship = await _context.Friendships
                    .FirstOrDefaultAsync(f => 
                        ((f.RequesterId == senderId && f.AddresseeId == receiverIdInt) ||
                        (f.RequesterId == receiverIdInt && f.AddresseeId == senderId)) &&
                        f.Status == FriendshipStatus.Accepted);

                if (friendship == null)
                {
                    await Clients.Caller.SendAsync("Error", "Bạn không thể nhắn tin với người này");
                    return;
                }

                // Tạo và lưu tin nhắn vào database
                var message = new Message
                {
                    SenderId = senderId,
                    ReceiverId = receiverIdInt,
                    Content = content,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                // Lấy thông tin người gửi để hiển thị
                var sender = await _context.Users
                    .Select(u => new { u.Id, u.Username, u.FullName, u.ProfilePicture })
                    .FirstOrDefaultAsync(u => u.Id == senderId);

                // Tạo object tin nhắn để gửi tới client
                var messageData = new
                {
                    id = message.Id,
                    content = message.Content,
                    createdAt = message.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    senderId = senderId,
                    senderName = sender?.FullName ?? sender?.Username,
                    receiverId = receiverIdInt
                };

                // Gửi tin nhắn tới người nhận (nếu đang online)
                await Clients.Group($"User_{receiverId}").SendAsync("ReceiveMessage", messageData);

                // Gửi confirmation tới người gửi
                await Clients.Caller.SendAsync("MessageSent", messageData);

                _logger.LogInformation($"Message sent from user {senderId} to user {receiverId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                await Clients.Caller.SendAsync("Error", "Có lỗi xảy ra khi gửi tin nhắn");
            }
        }

        /// <summary>
        /// Gửi hình ảnh từ một người dùng tới người khác
        /// </summary>
        /// <param name="receiverId">ID người nhận</param>
        /// <param name="imageUrl">URL hình ảnh đã upload</param>
        public async Task SendImage(string receiverId, string imageUrl)
        {
            try
            {
                // Lấy ID người gửi
                var senderIdString = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(senderIdString) || !int.TryParse(senderIdString, out var senderId))
                {
                    await Clients.Caller.SendAsync("Error", "Không thể xác định người gửi");
                    return;
                }

                if (!int.TryParse(receiverId, out var receiverIdInt))
                {
                    await Clients.Caller.SendAsync("Error", "ID người nhận không hợp lệ");
                    return;
                }

                // Kiểm tra tình trạng bạn bè
                var friendship = await _context.Friendships
                    .FirstOrDefaultAsync(f => 
                        ((f.RequesterId == senderId && f.AddresseeId == receiverIdInt) ||
                        (f.RequesterId == receiverIdInt && f.AddresseeId == senderId)) &&
                        f.Status == FriendshipStatus.Accepted);

                if (friendship == null)
                {
                    await Clients.Caller.SendAsync("Error", "Bạn không thể nhắn tin với người này");
                    return;
                }

                // Tạo và lưu tin nhắn hình ảnh vào database
                var message = new Message
                {
                    SenderId = senderId,
                    ReceiverId = receiverIdInt,
                    Content = "", // Để trống vì đây là tin nhắn hình ảnh
                    ImageUrl = imageUrl,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                // Lấy thông tin người gửi
                var sender = await _context.Users
                    .Select(u => new { u.Id, u.Username, u.FullName, u.ProfilePicture })
                    .FirstOrDefaultAsync(u => u.Id == senderId);

                // Tạo object tin nhắn để gửi tới client
                var messageData = new
                {
                    id = message.Id,
                    content = message.Content,
                    imageUrl = message.ImageUrl,
                    createdAt = message.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    senderId = senderId,
                    senderName = sender?.FullName ?? sender?.Username,
                    receiverId = receiverIdInt
                };

                // Gửi tin nhắn tới người nhận (nếu đang online)
                await Clients.Group($"User_{receiverId}").SendAsync("ReceiveMessage", messageData);

                // Gửi confirmation tới người gửi
                await Clients.Caller.SendAsync("MessageSent", messageData);

                _logger.LogInformation($"Image message sent from user {senderId} to user {receiverId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending image message");
                await Clients.Caller.SendAsync("Error", "Có lỗi xảy ra khi gửi hình ảnh");
            }
        }

        /// <summary>
        /// Đánh dấu tin nhắn là đã đọc
        /// </summary>
        /// <param name="senderId">ID người gửi tin nhắn</param>
        public async Task MarkMessagesAsRead(string senderId)
        {
            try
            {
                // Lấy ID người đọc tin nhắn
                var readerIdString = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(readerIdString) || !int.TryParse(readerIdString, out var readerId))
                {
                    return;
                }

                if (!int.TryParse(senderId, out var senderIdInt))
                {
                    return;
                }

                // Đánh dấu tất cả tin nhắn chưa đọc từ sender tới reader là đã đọc
                var unreadMessages = await _context.Messages
                    .Where(m => m.SenderId == senderIdInt && m.ReceiverId == readerId && !m.IsRead)
                    .ToListAsync();

                foreach (var message in unreadMessages)
                {
                    message.IsRead = true;
                }

                if (unreadMessages.Count > 0)
                {
                    await _context.SaveChangesAsync();
                    
                    // Thông báo tới người gửi rằng tin nhắn đã được đọc
                    await Clients.Group($"User_{senderId}").SendAsync("MessagesRead", new { readerId = readerId });
                }

                _logger.LogInformation($"Marked {unreadMessages.Count} messages as read from user {senderIdInt} to user {readerId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking messages as read");
            }
        }

        /// <summary>
        /// Gửi thông báo khi người dùng đang typing
        /// </summary>
        /// <param name="receiverId">ID người nhận</param>
        public async Task SendTypingNotification(string receiverId)
        {
            try
            {
                var senderIdString = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(senderIdString))
                {
                    // Gửi thông báo typing tới người nhận
                    await Clients.Group($"User_{receiverId}").SendAsync("UserTyping", new { senderId = senderIdString });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending typing notification");
            }
        }

        /// <summary>
        /// Gửi thông báo khi người dùng ngừng typing
        /// </summary>
        /// <param name="receiverId">ID người nhận</param>
        public async Task SendStopTypingNotification(string receiverId)
        {
            try
            {
                var senderIdString = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(senderIdString))
                {
                    // Gửi thông báo stop typing tới người nhận
                    await Clients.Group($"User_{receiverId}").SendAsync("UserStoppedTyping", new { senderId = senderIdString });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending stop typing notification");
            }
        }
    }
} 