using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MXH_ASP.NET_CORE.Data;
using MXH_ASP.NET_CORE.Models;
using System.Security.Claims;

namespace MXH_ASP.NET_CORE.Controllers
{
    /// <summary>
    /// ViewModel để hiển thị thông tin bạn bè cùng số tin nhắn chưa đọc.
    /// </summary>
    public class FriendWithUnreadCountViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string ProfilePicture { get; set; }
        public int UnreadCount { get; set; } // Số tin nhắn chưa đọc từ người bạn này
    }

    [Authorize]
    public class MessageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MessageController> _logger;

        public MessageController(ApplicationDbContext context, ILogger<MessageController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Message/Chat/{userId}
        /// <summary>
        /// Hiển thị trang chat với người dùng cụ thể
        /// </summary>
        public async Task<IActionResult> Chat(int userId)
        {
            try
            {
                // Lấy ID người dùng hiện tại
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                _logger.LogInformation($"Accessing chat with userId: {userId}. Current user ID: {currentUserId}");

                // Kiểm tra xem hai người dùng có phải là bạn bè không
                var friendship = await _context.Friendships
                    .FirstOrDefaultAsync(f => 
                        ((f.RequesterId == currentUserId && f.AddresseeId == userId) ||
                        (f.RequesterId == userId && f.AddresseeId == currentUserId)) &&
                        f.Status == FriendshipStatus.Accepted);

                if (friendship == null)
                {
                    TempData["ErrorMessage"] = "Bạn không thể nhắn tin với người này";
                    return RedirectToAction("Index", "Home");
                }

                // Lấy thông tin người nhận
                var receiver = await _context.Users
                    .Select(u => new { u.Id, u.Username, u.FullName, u.ProfilePicture })
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (receiver == null)
                {
                    return NotFound();
                }

                // Lấy lịch sử tin nhắn
                var messages = await _context.Messages
                    .Where(m => 
                        (m.SenderId == currentUserId && m.ReceiverId == userId) ||
                        (m.SenderId == userId && m.ReceiverId == currentUserId))
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new
                    {
                        m.Id,
                        m.Content,
                        m.CreatedAt,
                        m.IsRead,
                        IsSender = m.SenderId == currentUserId
                    })
                    .ToListAsync();

                // Đánh dấu tin nhắn chưa đọc là đã đọc
                var unreadMessages = await _context.Messages
                    .Where(m => m.ReceiverId == currentUserId && m.SenderId == userId && !m.IsRead)
                    .ToListAsync();

                foreach (var message in unreadMessages)
                {
                    message.IsRead = true;
                }
                await _context.SaveChangesAsync();

                ViewBag.Receiver = receiver;
                ViewBag.Messages = messages;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi hiển thị trang chat");
                return View("Error");
            }
        }

        // POST: /Message/Send
        /// <summary>
        /// Gửi tin nhắn mới
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(int receiverId, string content)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content))
                {
                    return Json(new { success = false, message = "Nội dung tin nhắn không được để trống" });
                }

                // Lấy ID người dùng hiện tại
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                // Kiểm tra xem hai người dùng có phải là bạn bè không
                var friendship = await _context.Friendships
                    .FirstOrDefaultAsync(f => 
                        (f.RequesterId == currentUserId && f.AddresseeId == receiverId) ||
                        (f.RequesterId == receiverId && f.AddresseeId == currentUserId));

                if (friendship == null || friendship.Status != FriendshipStatus.Accepted)
                {
                    return Json(new { success = false, message = "Bạn không thể nhắn tin với người này" });
                }

                // Tạo tin nhắn mới
                var message = new Message
                {
                    SenderId = currentUserId,
                    ReceiverId = receiverId,
                    Content = content,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = new {
                        id = message.Id,
                        content = message.Content,
                        createdAt = message.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                        isSender = true
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi tin nhắn");
                return Json(new { success = false, message = "Có lỗi xảy ra khi gửi tin nhắn" });
            }
        }

        // GET: /Message/GetNewMessages
        /// <summary>
        /// Lấy tin nhắn mới
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetNewMessages(int userId, int lastMessageId)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var messages = await _context.Messages
                    .Where(m => 
                        m.Id > lastMessageId &&
                        ((m.SenderId == currentUserId && m.ReceiverId == userId) ||
                         (m.SenderId == userId && m.ReceiverId == currentUserId)))
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new
                    {
                        m.Id,
                        m.Content,
                        m.CreatedAt,
                        m.IsRead,
                        IsSender = m.SenderId == currentUserId
                    })
                    .ToListAsync();

                // Đánh dấu tin nhắn chưa đọc là đã đọc
                var unreadMessages = await _context.Messages
                    .Where(m => m.ReceiverId == currentUserId && m.SenderId == userId && !m.IsRead)
                    .ToListAsync();

                foreach (var message in unreadMessages)
                {
                    message.IsRead = true;
                }
                await _context.SaveChangesAsync();

                return Json(new { success = true, messages });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy tin nhắn mới");
                return Json(new { success = false, message = "Có lỗi xảy ra khi lấy tin nhắn mới" });
            }
        }

        // GET: /Message/Index
        /// <summary>
        /// Hiển thị trang danh sách bạn bè để chọn người nhắn tin, kèm theo số tin nhắn chưa đọc từ mỗi người.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                // Lấy danh sách bạn bè đã kết bạn
                var friendships = await _context.Friendships
                    .Where(f => f.Status == FriendshipStatus.Accepted &&
                        (f.RequesterId == currentUserId || f.AddresseeId == currentUserId))
                    .Include(f => f.Requester)
                    .Include(f => f.Addressee)
                    .ToListAsync();

                // Tạo danh sách ViewModel chứa thông tin bạn bè và số tin nhắn chưa đọc
                var friendsWithCount = new List<FriendWithUnreadCountViewModel>();

                foreach (var friendship in friendships)
                {
                    var friend = friendship.RequesterId == currentUserId ? friendship.Addressee : friendship.Requester;

                    // Đếm số tin nhắn chưa đọc từ người bạn này
                    var unreadCount = await _context.Messages
                        .CountAsync(m => m.SenderId == friend.Id && m.ReceiverId == currentUserId && !m.IsRead);

                    friendsWithCount.Add(new FriendWithUnreadCountViewModel
                    {
                        UserId = friend.Id,
                        Username = friend.Username,
                        FullName = friend.FullName,
                        ProfilePicture = friend.ProfilePicture,
                        UnreadCount = unreadCount
                    });
                }

                // Truyền danh sách ViewModel sang View
                return View(friendsWithCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi truy cập trang Index Message (danh sách bạn bè)");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải danh sách bạn bè.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: /Message/CheckNewMessagesCount
        /// <summary>
        /// Kiểm tra số lượng tin nhắn chưa đọc của người dùng hiện tại và trả về thông tin người gửi.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CheckNewMessagesCount()
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                // Lấy danh sách tin nhắn chưa đọc, sắp xếp theo thời gian để lấy những tin mới nhất
                var unreadMessages = await _context.Messages
                    .Where(m => m.ReceiverId == currentUserId && !m.IsRead)
                    .Include(m => m.Sender) // Include thông tin người gửi
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(5) // Lấy tối đa 5 tin nhắn chưa đọc gần nhất để hiển thị thông báo
                    .Select(m => new
                    {
                        SenderId = m.SenderId,
                        SenderName = m.Sender.FullName ?? m.Sender.Username // Lấy tên đầy đủ hoặc username của người gửi
                    })
                    .ToListAsync();

                var unreadCount = unreadMessages.Count; // Đếm số lượng tin nhắn chưa đọc

                // Nhóm các tin nhắn chưa đọc theo người gửi để hiển thị danh sách người gửi
                var sendersInfo = unreadMessages
                    .GroupBy(m => m.SenderId)
                    .Select(g => new { SenderId = g.Key, SenderName = g.First().SenderName })
                    .ToList();

                return Json(new { success = true, count = unreadCount, senders = sendersInfo });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kiểm tra số lượng tin nhắn mới");
                return Json(new { success = false, message = "Có lỗi xảy ra khi kiểm tra tin nhắn mới" });
            }
        }
    }
} 