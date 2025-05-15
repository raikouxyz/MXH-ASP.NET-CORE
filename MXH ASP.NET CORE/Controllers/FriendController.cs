using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MXH_ASP.NET_CORE.Data;
using MXH_ASP.NET_CORE.Models;
using MXH_ASP.NET_CORE.ViewModels;
using System.Security.Claims;

namespace MXH_ASP.NET_CORE.Controllers
{
    [Authorize] // Yêu cầu đăng nhập để truy cập trang bạn bè
    public class FriendController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FriendController> _logger;

        public FriendController(ApplicationDbContext context, ILogger<FriendController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Hiển thị trang bạn bè chính với các tab: Bạn bè, Lời mời, Đã gửi, Gợi ý
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                // Lấy ID của người dùng hiện tại
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return RedirectToAction("Login", "Account");
                }

                var userId = int.Parse(userIdClaim);
                _logger.LogInformation($"User {userId} accessing friends page");

                // Chuẩn bị ViewModel tổng hợp
                var viewModel = new FriendListViewModel();

                // 1. Lấy danh sách bạn bè (những người đã chấp nhận kết bạn)
                var acceptedFriendships = await _context.Friendships
                    .Where(f => (f.RequesterId == userId || f.AddresseeId == userId) && f.Status == FriendshipStatus.Accepted)
                    .Include(f => f.Requester)
                    .Include(f => f.Addressee)
                    .ToListAsync();

                foreach (var friendship in acceptedFriendships)
                {
                    // Xác định người bạn (người còn lại trong quan hệ)
                    var friend = friendship.RequesterId == userId ? friendship.Addressee : friendship.Requester;

                    viewModel.Friends.Add(new FriendViewModel
                    {
                        UserId = friend.Id,
                        Username = friend.Username,
                        FullName = friend.FullName,
                        ProfilePicture = friend.ProfilePicture,
                        FriendsSince = friendship.UpdatedAt ?? friendship.CreatedAt,
                        CanUnfriend = true
                    });
                }

                // 2. Lấy các lời mời kết bạn đã nhận
                var receivedRequests = await _context.Friendships
                    .Where(f => f.AddresseeId == userId && f.Status == FriendshipStatus.Pending)
                    .Include(f => f.Requester)
                    .ToListAsync();

                foreach (var request in receivedRequests)
                {
                    viewModel.ReceivedRequests.Add(new FriendRequestViewModel
                    {
                        FriendshipId = request.Id,
                        UserId = request.RequesterId,
                        Username = request.Requester.Username,
                        FullName = request.Requester.FullName,
                        ProfilePicture = request.Requester.ProfilePicture,
                        RequestDate = request.CreatedAt,
                        Status = request.Status
                    });
                }

                // 3. Lấy các lời mời kết bạn đã gửi
                var sentRequests = await _context.Friendships
                    .Where(f => f.RequesterId == userId && f.Status == FriendshipStatus.Pending)
                    .Include(f => f.Addressee)
                    .ToListAsync();

                foreach (var request in sentRequests)
                {
                    viewModel.SentRequests.Add(new FriendRequestViewModel
                    {
                        FriendshipId = request.Id,
                        UserId = request.AddresseeId,
                        Username = request.Addressee.Username,
                        FullName = request.Addressee.FullName,
                        ProfilePicture = request.Addressee.ProfilePicture,
                        RequestDate = request.CreatedAt,
                        Status = request.Status
                    });
                }

                // 4. Lấy gợi ý kết bạn (những người chưa là bạn và chưa có lời mời)
                // Lấy danh sách tất cả người dùng đã có tương tác qua friendship
                var existingFriendUserIds = acceptedFriendships
                    .SelectMany(f => new[] { f.RequesterId, f.AddresseeId })
                    .Concat(receivedRequests.Select(r => r.RequesterId))
                    .Concat(sentRequests.Select(r => r.AddresseeId))
                    .Distinct()
                    .ToList();

                // Thêm user Id hiện tại vào danh sách loại trừ
                existingFriendUserIds.Add(userId);

                // Lấy 5 người dùng ngẫu nhiên chưa có tương tác
                var suggestedUsers = await _context.Users
                    .Where(u => !existingFriendUserIds.Contains(u.Id))
                    .OrderBy(u => Guid.NewGuid()) // Sắp xếp ngẫu nhiên
                    .Take(5)
                    .Select(u => new UserViewModel
                    {
                        UserId = u.Id,
                        Username = u.Username,
                        FullName = u.FullName,
                        ProfilePicture = u.ProfilePicture
                    })
                    .ToListAsync();

                viewModel.SuggestedFriends = suggestedUsers;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Friend/Index");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải trang bạn bè.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Gửi lời mời kết bạn đến một người dùng khác
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendRequest(int userId)
        {
            try
            {
                // Lấy ID của người dùng hiện tại
                var currentUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(currentUserIdClaim))
                {
                    return RedirectToAction("Login", "Account");
                }

                var currentUserId = int.Parse(currentUserIdClaim);

                // Kiểm tra người dùng muốn kết bạn có tồn tại không
                var addressee = await _context.Users.FindAsync(userId);
                if (addressee == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                    return RedirectToAction("Index");
                }

                // Kiểm tra đã có mối quan hệ bạn bè nào tồn tại chưa
                var existingFriendship = await _context.Friendships
                    .FirstOrDefaultAsync(f => 
                        (f.RequesterId == currentUserId && f.AddresseeId == userId) ||
                        (f.RequesterId == userId && f.AddresseeId == currentUserId));

                if (existingFriendship != null)
                {
                    _logger.LogInformation($"Friendship already exists between {currentUserId} and {userId} with status {existingFriendship.Status}");
                    
                    if (existingFriendship.Status == FriendshipStatus.Accepted)
                    {
                        TempData["ErrorMessage"] = "Người này đã là bạn bè của bạn.";
                        return RedirectToAction("Index");
                    }
                    else if (existingFriendship.Status == FriendshipStatus.Pending)
                    {
                        if (existingFriendship.RequesterId == currentUserId)
                        {
                            TempData["ErrorMessage"] = "Bạn đã gửi lời mời kết bạn cho người này.";
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Người này đã gửi lời mời kết bạn cho bạn. Hãy chấp nhận lời mời.";
                        }
                        return RedirectToAction("Index");
                    }
                    else if (existingFriendship.Status == FriendshipStatus.Blocked)
                    {
                        TempData["ErrorMessage"] = "Không thể kết bạn với người này.";
                        return RedirectToAction("Index");
                    }
                }

                // Tạo lời mời kết bạn mới
                var friendship = new Friendship
                {
                    RequesterId = currentUserId,
                    AddresseeId = userId,
                    Status = FriendshipStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Friendships.Add(friendship);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Friend request sent from {currentUserId} to {userId}");
                TempData["SuccessMessage"] = "Đã gửi lời mời kết bạn thành công.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending friend request to user {userId}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi gửi lời mời kết bạn.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Chấp nhận lời mời kết bạn
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptRequest(int friendshipId)
        {
            try
            {
                // Lấy ID của người dùng hiện tại
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return RedirectToAction("Login", "Account");
                }

                var userId = int.Parse(userIdClaim);

                // Kiểm tra lời mời kết bạn có tồn tại không
                var friendship = await _context.Friendships
                    .Include(f => f.Requester)
                    .FirstOrDefaultAsync(f => f.Id == friendshipId && f.AddresseeId == userId);

                if (friendship == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy lời mời kết bạn.";
                    return RedirectToAction("Index");
                }

                // Chấp nhận lời mời
                friendship.Status = FriendshipStatus.Accepted;
                friendship.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Friend request from {friendship.RequesterId} to {userId} accepted");
                TempData["SuccessMessage"] = $"Bạn đã trở thành bạn bè với {friendship.Requester.FullName}.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error accepting friend request {friendshipId}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi chấp nhận lời mời kết bạn.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Từ chối lời mời kết bạn
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRequest(int friendshipId)
        {
            try
            {
                // Lấy ID của người dùng hiện tại
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return RedirectToAction("Login", "Account");
                }

                var userId = int.Parse(userIdClaim);

                // Kiểm tra lời mời kết bạn có tồn tại không
                var friendship = await _context.Friendships
                    .FirstOrDefaultAsync(f => f.Id == friendshipId && f.AddresseeId == userId);

                if (friendship == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy lời mời kết bạn.";
                    return RedirectToAction("Index");
                }

                // Từ chối lời mời
                _context.Friendships.Remove(friendship);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Friend request from {friendship.RequesterId} to {userId} rejected");
                TempData["SuccessMessage"] = "Đã từ chối lời mời kết bạn.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rejecting friend request {friendshipId}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi từ chối lời mời kết bạn.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Hủy lời mời kết bạn đã gửi
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRequest(int friendshipId)
        {
            try
            {
                // Lấy ID của người dùng hiện tại
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return RedirectToAction("Login", "Account");
                }

                var userId = int.Parse(userIdClaim);

                // Kiểm tra lời mời kết bạn có tồn tại không
                var friendship = await _context.Friendships
                    .FirstOrDefaultAsync(f => f.Id == friendshipId && f.RequesterId == userId);

                if (friendship == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy lời mời kết bạn.";
                    return RedirectToAction("Index");
                }

                // Hủy lời mời
                _context.Friendships.Remove(friendship);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Friend request from {userId} to {friendship.AddresseeId} canceled");
                TempData["SuccessMessage"] = "Đã hủy lời mời kết bạn.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error canceling friend request {friendshipId}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi hủy lời mời kết bạn.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Hủy kết bạn với một người
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unfriend(int userId)
        {
            try
            {
                // Lấy ID của người dùng hiện tại
                var currentUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(currentUserIdClaim))
                {
                    return RedirectToAction("Login", "Account");
                }

                var currentUserId = int.Parse(currentUserIdClaim);

                // Tìm mối quan hệ bạn bè
                var friendship = await _context.Friendships
                    .FirstOrDefaultAsync(f => 
                        ((f.RequesterId == currentUserId && f.AddresseeId == userId) || 
                         (f.RequesterId == userId && f.AddresseeId == currentUserId)) && 
                        f.Status == FriendshipStatus.Accepted);

                if (friendship == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy mối quan hệ bạn bè.";
                    return RedirectToAction("Index");
                }

                // Xóa mối quan hệ bạn bè
                _context.Friendships.Remove(friendship);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Friendship between {currentUserId} and {userId} removed");
                TempData["SuccessMessage"] = "Đã hủy kết bạn thành công.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error unfriending user {userId}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi hủy kết bạn.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Tìm kiếm người dùng để kết bạn
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return RedirectToAction("Index");
            }

            try
            {
                // Lấy ID của người dùng hiện tại
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return RedirectToAction("Login", "Account");
                }

                var userId = int.Parse(userIdClaim);

                // Tìm kiếm người dùng theo username hoặc họ tên (không bao gồm bản thân)
                var searchResults = await _context.Users
                    .Where(u => u.Id != userId && 
                           (u.Username.Contains(query) || u.FullName.Contains(query)))
                    .Take(20)
                    .ToListAsync();

                // Lấy danh sách các mối quan hệ bạn bè hiện có của người dùng hiện tại
                var friendships = await _context.Friendships
                    .Where(f => f.RequesterId == userId || f.AddresseeId == userId)
                    .ToListAsync();

                // Chuyển đổi kết quả thành ViewModel với trạng thái bạn bè
                var viewModel = new List<SearchResultViewModel>();
                
                foreach (var user in searchResults)
                {
                    var friendship = friendships.FirstOrDefault(f => 
                        (f.RequesterId == userId && f.AddresseeId == user.Id) || 
                        (f.RequesterId == user.Id && f.AddresseeId == userId));

                    var result = new SearchResultViewModel
                    {
                        UserId = user.Id,
                        Username = user.Username,
                        FullName = user.FullName,
                        ProfilePicture = user.ProfilePicture
                    };

                    if (friendship != null)
                    {
                        result.FriendshipStatus = friendship.Status;
                        result.FriendshipId = friendship.Id;
                        result.IsRequester = friendship.RequesterId == userId;
                    }

                    viewModel.Add(result);
                }

                ViewData["SearchQuery"] = query;
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching for users with query: {query}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tìm kiếm người dùng.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// API cung cấp gợi ý kết bạn cho trang chủ
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSuggestions()
        {
            try
            {
                // Lấy ID của người dùng hiện tại
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để xem gợi ý kết bạn." });
                }

                var userId = int.Parse(userIdClaim);

                // Lấy danh sách tất cả mối quan hệ bạn bè hiện có
                var existingRelationships = await _context.Friendships
                    .Where(f => f.RequesterId == userId || f.AddresseeId == userId)
                    .ToListAsync();

                // Lấy danh sách ID người dùng đã có mối quan hệ
                var existingUserIds = existingRelationships
                    .SelectMany(f => new[] { f.RequesterId, f.AddresseeId })
                    .Distinct()
                    .ToList();

                // Thêm user Id hiện tại vào danh sách loại trừ
                existingUserIds.Add(userId);

                // Lấy 5 người dùng ngẫu nhiên chưa có mối quan hệ
                var suggestedUsers = await _context.Users
                    .Where(u => !existingUserIds.Contains(u.Id))
                    .OrderBy(u => Guid.NewGuid()) // Sắp xếp ngẫu nhiên
                    .Take(5)
                    .Select(u => new
                    {
                        userId = u.Id,
                        username = u.Username,
                        fullName = u.FullName,
                        profilePicture = u.ProfilePicture
                    })
                    .ToListAsync();

                return Json(new { success = true, suggestions = suggestedUsers });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting friend suggestions");
                return Json(new { success = false, message = "Đã xảy ra lỗi khi tải gợi ý kết bạn." });
            }
        }
    }

    /// <summary>
    /// ViewModel hiển thị kết quả tìm kiếm bạn bè
    /// </summary>
    public class SearchResultViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string ProfilePicture { get; set; }
        public FriendshipStatus? FriendshipStatus { get; set; }
        public int? FriendshipId { get; set; }
        public bool IsRequester { get; set; }
    }
} 