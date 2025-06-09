using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MXH_ASP.NET_CORE.Data;
using MXH_ASP.NET_CORE.Models;
using MXH_ASP.NET_CORE.ViewModels;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace MXH_ASP.NET_CORE.Controllers
{
    public class SearchController : BaseController
    {
        private readonly ILogger<SearchController> _logger;

        public SearchController(ApplicationDbContext context, ILogger<SearchController> logger) : base(context)
        {
            _logger = logger;
        }

        // Hiển thị trang tìm kiếm
        public async Task<IActionResult> Index()
        {
            await SetCurrentUserInfo(); // Gọi method từ base class
            return View();
        }

        // Chuyển đổi tiếng Việt có dấu thành không dấu
        private string RemoveDiacritics(string text)
        {
            string normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
            var stringBuilder = new System.Text.StringBuilder();

            foreach (char c in normalizedString)
            {
                System.Globalization.UnicodeCategory unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }

        // API tìm kiếm người dùng
        [HttpGet]
        public async Task<IActionResult> SearchUsers(string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return Json(new { success = false, message = "Vui lòng nhập từ khóa tìm kiếm" });
                }

                // Lấy ID người dùng hiện tại
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int? currentUserIdInt = currentUserId != null ? int.Parse(currentUserId) : null;

                // Chuyển đổi query thành chữ thường
                string lowerQuery = query.ToLower();
                // Chuyển đổi query thành không dấu
                string normalizedQuery = RemoveDiacritics(lowerQuery);

                // Lấy danh sách người dùng và thực hiện tìm kiếm trên bộ nhớ
                var users = await _context.Users
                    .Select(u => new
                    {
                        u.Id,
                        u.Username,
                        u.FullName,
                        u.ProfilePicture
                    })
                    .ToListAsync();

                // Lọc kết quả trên bộ nhớ - tìm kiếm cả có dấu và không dấu
                var filteredUsers = users
                    .Where(u => 
                        u.FullName.ToLower().Contains(lowerQuery) || // Tìm kiếm có dấu
                        RemoveDiacritics(u.FullName.ToLower()).Contains(normalizedQuery)) // Tìm kiếm không dấu
                    .Take(10)
                    .ToList();

                // Lấy thông tin về trạng thái kết bạn
                var friendships = new List<object>();
                if (currentUserIdInt.HasValue)
                {
                    friendships = await _context.Friendships
                        .Where(f => (f.RequesterId == currentUserIdInt.Value || f.AddresseeId == currentUserIdInt.Value) &&
                                   filteredUsers.Select(u => u.Id).Contains(f.RequesterId == currentUserIdInt.Value ? f.AddresseeId : f.RequesterId))
                        .Select(f => new
                        {
                            FriendId = f.RequesterId == currentUserIdInt.Value ? f.AddresseeId : f.RequesterId,
                            Status = f.Status,
                            IsRequester = f.RequesterId == currentUserIdInt.Value
                        })
                        .ToListAsync<object>();
                }

                return Json(new { 
                    success = true, 
                    users = filteredUsers,
                    friendships = friendships
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tìm kiếm người dùng");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tìm kiếm" });
            }
        }

        // API tìm kiếm bài viết
        [HttpGet]
        public async Task<IActionResult> SearchPosts(string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return Json(new { success = false, message = "Vui lòng nhập từ khóa tìm kiếm" });
                }

                // Chuyển đổi query thành chữ thường
                string lowerQuery = query.ToLower();
                // Chuyển đổi query thành không dấu
                string normalizedQuery = RemoveDiacritics(lowerQuery);

                // Lấy ID người dùng hiện tại
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Lấy danh sách bài viết và thực hiện tìm kiếm trên bộ nhớ
                var posts = await _context.Posts
                    .Include(p => p.User)
                    .Include(p => p.Images)
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new
                    {
                        p.Id,
                        p.Content,
                        ImageUrls = p.Images.OrderBy(i => i.Order).Select(i => i.ImageUrl).ToList(),
                        p.CreatedAt,
                        p.UpdatedAt,
                        UserId = p.User.Id,
                        UserFullName = p.User.FullName,
                        UserProfilePicture = p.User.ProfilePicture,
                        LikeCount = p.Likes.Count,
                        CommentCount = p.Comments.Count,
                        CanEdit = currentUserId != null && p.UserId == int.Parse(currentUserId),
                        CanDelete = currentUserId != null && p.UserId == int.Parse(currentUserId)
                    })
                    .ToListAsync();

                // Lọc kết quả trên bộ nhớ - tìm kiếm cả có dấu và không dấu
                var filteredPosts = posts
                    .Where(p => 
                        p.Content.ToLower().Contains(lowerQuery) || // Tìm kiếm có dấu
                        RemoveDiacritics(p.Content.ToLower()).Contains(normalizedQuery)) // Tìm kiếm không dấu
                    .Take(10)
                    .ToList();

                return Json(new { success = true, posts = filteredPosts });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tìm kiếm bài viết");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tìm kiếm" });
            }
        }
    }
} 