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
    public class MessageController : BaseController
    {
        private readonly ILogger<MessageController> _logger;

        public MessageController(ApplicationDbContext context, ILogger<MessageController> logger) 
            : base(context)
        {
            _logger = logger;
        }

        // GET: /Message/Chat/{userId}
        /// <summary>
        /// Hiển thị trang chat với người dùng cụ thể
        /// </summary>
        public async Task<IActionResult> Chat(int userId)
        {
            await SetCurrentUserInfo(); // Gọi method từ base class

            try
            {
                // Lấy ID người dùng hiện tại
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

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
                        m.ImageUrl,
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
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

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
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

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
                        m.ImageUrl,
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
            await SetCurrentUserInfo(); // Gọi method từ base class

            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

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
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

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

        // Thêm action mới để xử lý upload hình ảnh
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendImage(int receiverId, IFormFile image)
        {
            try
            {
                if (image == null || image.Length == 0)
                {
                    return Json(new { success = false, message = "Vui lòng chọn hình ảnh" });
                }

                // Kiểm tra định dạng file
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(image.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return Json(new { success = false, message = "Định dạng file không được hỗ trợ" });
                }

                // Kiểm tra kích thước file (giới hạn 5MB)
                if (image.Length > 5 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "Kích thước file không được vượt quá 5MB" });
                }

                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                // Kiểm tra tình trạng bạn bè
                var friendship = await _context.Friendships
                    .FirstOrDefaultAsync(f => 
                        (f.RequesterId == currentUserId && f.AddresseeId == receiverId) ||
                        (f.RequesterId == receiverId && f.AddresseeId == currentUserId));

                if (friendship == null || friendship.Status != FriendshipStatus.Accepted)
                {
                    return Json(new { success = false, message = "Bạn không thể nhắn tin với người này" });
                }

                // Tạo thư mục nếu chưa tồn tại
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "messages");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Tạo tên file duy nhất
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Lưu file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }

                // Tạo tin nhắn mới với hình ảnh
                var message = new Message
                {
                    SenderId = currentUserId,
                    ReceiverId = receiverId,
                    Content = "", // Để trống vì đây là tin nhắn hình ảnh
                    ImageUrl = "/uploads/messages/" + uniqueFileName,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = new {
                        id = message.Id,
                        content = message.Content,
                        imageUrl = message.ImageUrl,
                        createdAt = message.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                        isSender = true
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi hình ảnh");
                return Json(new { success = false, message = "Có lỗi xảy ra khi gửi hình ảnh" });
            }
        }
    }
} 