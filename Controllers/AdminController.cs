using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MXH_ASP.NET_CORE.Data;
using MXH_ASP.NET_CORE.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MXH_ASP.NET_CORE.Controllers
{
    [Authorize(Roles = "Admin")] // Chỉ cho phép Admin truy cập
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Trang chủ Admin
        public IActionResult Index()
        {
            return View();
        }

        // Quản lý người dùng
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.FullName,
                    u.Email,
                    u.PhoneNumber,
                    u.CreatedAt,
                    u.IsActive,
                    u.Role
                })
                .ToListAsync();

            return View(users);
        }

        // Thêm người dùng mới
        [HttpPost]
        public async Task<IActionResult> AddUser(User user)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra username đã tồn tại chưa
                if (await _context.Users.AnyAsync(u => u.Username == user.Username))
                {
                    return Json(new { success = false, message = "Tên đăng nhập đã tồn tại" });
                }

                // Kiểm tra email đã tồn tại chưa
                if (await _context.Users.AnyAsync(u => u.Email == user.Email))
                {
                    return Json(new { success = false, message = "Email đã tồn tại" });
                }

                // Mã hóa mật khẩu
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
                user.CreatedAt = DateTime.UtcNow;
                user.IsActive = true;

                // Chuyển đổi Role từ string sang enum
                if (Enum.TryParse<UserRole>(user.Role.ToString(), out var role))
                {
                    user.Role = role;
                }
                else
                {
                    user.Role = UserRole.User; // Mặc định là User nếu không parse được
                }

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Thêm người dùng thành công" });
            }

            return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
        }

        // Cập nhật thông tin người dùng
        [HttpPost]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserModel model)
        {
            try
            {
                var existingUser = await _context.Users.FindAsync(model.Id);
                if (existingUser == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });
                }

                // Cập nhật thông tin
                existingUser.FullName = model.FullName;
                existingUser.Email = model.Email;
                existingUser.PhoneNumber = model.PhoneNumber;
                existingUser.IsActive = model.IsActive;

                // Chuyển đổi Role từ string sang enum
                if (Enum.TryParse<UserRole>(model.Role, true, out var role))
                {
                    existingUser.Role = role;
                }

                // Nếu có thay đổi mật khẩu
                if (!string.IsNullOrEmpty(model.PasswordHash))
                {
                    existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.PasswordHash);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Cập nhật thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi cập nhật: " + ex.Message });
            }
        }

        // Model cho việc cập nhật user
        public class UpdateUserModel
        {
            public int Id { get; set; }
            public string Username { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public string PhoneNumber { get; set; }
            public string Role { get; set; }
            public bool IsActive { get; set; }
            public string PasswordHash { get; set; }
        }

        // Xóa người dùng
        [HttpPost]
        [Route("Admin/DeleteUser/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });
                }

                // Xóa tất cả dữ liệu liên quan trước khi xóa user

                // 1. Xóa tin nhắn (người gửi hoặc người nhận)
                var messages = _context.Messages.Where(m => m.SenderId == id || m.ReceiverId == id);
                _context.Messages.RemoveRange(messages);

                // 2. Xóa bài viết yêu thích
                var favoritePosts = _context.FavoritePosts.Where(fp => fp.UserId == id);
                _context.FavoritePosts.RemoveRange(favoritePosts);

                // 3. Xóa lượt thích của user
                var likes = _context.Likes.Where(l => l.UserId == id);
                _context.Likes.RemoveRange(likes);

                // 4. Xóa bình luận của user
                var comments = _context.Comments.Where(c => c.UserId == id);
                _context.Comments.RemoveRange(comments);

                // 5. Xóa quan hệ bạn bè (người yêu cầu hoặc người được yêu cầu)
                var friendships = _context.Friendships.Where(f => f.RequesterId == id || f.AddresseeId == id);
                _context.Friendships.RemoveRange(friendships);

                // 6. Xóa các bài viết của user (cùng với images, comments, likes của bài viết đó)
                var userPosts = _context.Posts.Where(p => p.UserId == id).ToList();
                foreach (var post in userPosts)
                {
                    // Xóa hình ảnh của bài viết
                    var postImages = _context.PostImages.Where(pi => pi.PostId == post.Id);
                    _context.PostImages.RemoveRange(postImages);

                    // Xóa bình luận của bài viết
                    var postComments = _context.Comments.Where(c => c.PostId == post.Id);
                    _context.Comments.RemoveRange(postComments);

                    // Xóa lượt thích của bài viết
                    var postLikes = _context.Likes.Where(l => l.PostId == post.Id);
                    _context.Likes.RemoveRange(postLikes);

                    // Xóa bài viết yêu thích liên quan đến bài viết này
                    var postFavorites = _context.FavoritePosts.Where(fp => fp.PostId == post.Id);
                    _context.FavoritePosts.RemoveRange(postFavorites);
                }

                // Xóa các bài viết của user
                _context.Posts.RemoveRange(userPosts);

                // 7. Cuối cùng xóa user
                _context.Users.Remove(user);
                
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa người dùng thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting user {id}");
                return Json(new { success = false, message = "Lỗi xóa người dùng: " + ex.Message });
            }
        }

        // Lấy thông tin chi tiết người dùng
        [HttpGet]
        public async Task<IActionResult> GetUserDetails(int id)
        {
            var user = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.FullName,
                    u.Email,
                    u.PhoneNumber,
                    u.CreatedAt,
                    u.IsActive,
                    u.Role
                })
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng" });
            }

            return Json(new { success = true, data = user });
        }

        // Quản lý bài viết
        public async Task<IActionResult> Posts()
        {
            var posts = await _context.Posts
                .Include(p => p.User)
                .Select(p => new
                {
                    p.Id,
                    p.Content,
                    p.CreatedAt,
                    p.UpdatedAt,
                    UserName = p.User.Username,
                    UserFullName = p.User.FullName,
                    LikeCount = p.Likes.Count,
                    CommentCount = p.Comments.Count
                })
                .ToListAsync();

            return View(posts);
        }

        // Khóa/Mở khóa người dùng
        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Users));
        }

        // ViewModel cho bài viết
        public class PostViewModel
        {
            public int Id { get; set; }
            public string Content { get; set; }
            public int UserId { get; set; }
        }

        // Lấy chi tiết bài viết
        [HttpGet]
        public async Task<IActionResult> GetPostDetails(int id)
        {
            var post = await _context.Posts
                .Include(p => p.User)
                .Select(p => new
                {
                    p.Id,
                    p.Content,
                    p.CreatedAt,
                    p.UpdatedAt,
                    UserId = p.User.Id,
                    UserName = p.User.Username,
                    UserFullName = p.User.FullName
                })
                .FirstOrDefaultAsync(p => p.Id == id);
            if (post == null)
                return Json(new { success = false, message = "Không tìm thấy bài viết" });
            return Json(new { success = true, data = post });
        }

        // Thêm bài viết mới
        [HttpPost]
        public async Task<IActionResult> AddPost([FromBody] PostViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Content))
                return Json(new { success = false, message = "Nội dung không được để trống" });
            var user = await _context.Users.FindAsync(model.UserId);
            if (user == null)
                return Json(new { success = false, message = "Không tìm thấy người đăng" });
            var post = new Post
            {
                Content = model.Content,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UserId = model.UserId
            };
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Thêm bài viết thành công" });
        }

        // Cập nhật bài viết
        [HttpPost]
        public async Task<IActionResult> UpdatePost([FromBody] PostViewModel model)
        {
            var post = await _context.Posts.FindAsync(model.Id);
            if (post == null)
                return Json(new { success = false, message = "Không tìm thấy bài viết" });
            if (string.IsNullOrWhiteSpace(model.Content))
                return Json(new { success = false, message = "Nội dung không được để trống" });
            post.Content = model.Content;
            post.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cập nhật bài viết thành công" });
        }

        // Xóa bài viết
        [HttpPost]
        [Route("Admin/DeletePost/{id}")]
        public async Task<IActionResult> DeletePost(int id)
        {
            try
            {
                var post = await _context.Posts.FindAsync(id);
                if (post == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy bài viết" });
                }

                // Xóa các bình luận liên quan trước
                var comments = _context.Comments.Where(c => c.PostId == id);
                _context.Comments.RemoveRange(comments);

                // Xóa các lượt thích liên quan
                var likes = _context.Likes.Where(l => l.PostId == id);
                _context.Likes.RemoveRange(likes);

                // Xóa bài viết
                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa bài viết thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting post {id}");
                return Json(new { success = false, message = "Lỗi xóa bài viết: " + ex.Message });
            }
        }
    }
} 