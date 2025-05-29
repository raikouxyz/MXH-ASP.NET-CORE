using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MXH_ASP.NET_CORE.Data;
using MXH_ASP.NET_CORE.Models;
using MXH_ASP.NET_CORE.ViewModels;
using System.Security.Claims;

namespace MXH_ASP.NET_CORE.Controllers
{
    [Authorize] // Yêu cầu đăng nhập để sử dụng các chức năng liên quan đến bài viết
    public class PostController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PostController> _logger;

        public PostController(ApplicationDbContext context, ILogger<PostController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// API tạo bài viết mới
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePostViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Lấy ID của người dùng hiện tại
                    var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (string.IsNullOrEmpty(userIdClaim))
                    {
                        ModelState.AddModelError(string.Empty, "Bạn cần đăng nhập để đăng bài viết");
                        return RedirectToAction("Login", "Account");
                    }
                    
                    var userId = int.Parse(userIdClaim);

                    // Tạo bài viết mới
                    var post = new Post
                    {
                        Content = model.Content,
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow
                    };

                    // Xử lý upload ảnh nếu có
                    if (model.ImageFile != null && model.ImageFile.Length > 0)
                    {
                        // Tạo thư mục nếu chưa tồn tại
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "posts");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        // Tạo tên file duy nhất
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImageFile.FileName;
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        // Lưu file
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.ImageFile.CopyToAsync(fileStream);
                        }

                        // Lưu đường dẫn tương đối vào database
                        post.ImageUrl = "/uploads/posts/" + uniqueFileName;
                    }

                    _context.Posts.Add(post);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"User {userId} created post {post.Id}");

                    // Trả về thông báo thành công
                    TempData["SuccessMessage"] = "Đã đăng bài viết thành công!";
                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating post");
                    ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi đăng bài viết");
                }
            }

            // Nếu có lỗi, chuyển hướng về trang chủ với thông báo lỗi
            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Hiển thị form chỉnh sửa bài viết
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            // Lấy ID của người dùng hiện tại
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }
            
            var userId = int.Parse(userIdClaim);

            // Lấy bài viết cần chỉnh sửa
            var post = await _context.Posts.FindAsync(id);

            // Kiểm tra bài viết tồn tại không
            if (post == null)
            {
                _logger.LogWarning($"Post {id} not found");
                return NotFound();
            }

            // Kiểm tra người dùng có quyền chỉnh sửa không
            if (post.UserId != userId)
            {
                _logger.LogWarning($"User {userId} attempted to edit post {id} without permission");
                return Forbid();
            }

            // Tạo view model
            var viewModel = new EditPostViewModel
            {
                Id = post.Id,
                Content = post.Content
            };

            return View(viewModel);
        }

        /// <summary>
        /// Xử lý chỉnh sửa bài viết
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditPostViewModel model)
        {
            if (ModelState.IsValid)
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

                    // Lấy bài viết cần chỉnh sửa
                    var post = await _context.Posts.FindAsync(model.Id);

                    // Kiểm tra bài viết tồn tại không
                    if (post == null)
                    {
                        _logger.LogWarning($"Post {model.Id} not found");
                        return NotFound();
                    }

                    // Kiểm tra người dùng có quyền chỉnh sửa không
                    if (post.UserId != userId)
                    {
                        _logger.LogWarning($"User {userId} attempted to edit post {model.Id} without permission");
                        return Forbid();
                    }

                    // Cập nhật nội dung
                    post.Content = model.Content;
                    post.UpdatedAt = DateTime.UtcNow;

                    _context.Update(post);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"User {userId} updated post {post.Id}");

                    TempData["SuccessMessage"] = "Đã cập nhật bài viết thành công!";
                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error updating post {model.Id}");
                    ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi cập nhật bài viết");
                }
            }

            return View(model);
        }

        /// <summary>
        /// Xóa bài viết
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
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

                // Lấy bài viết cần xóa
                var post = await _context.Posts.FindAsync(id);

                // Kiểm tra bài viết tồn tại không
                if (post == null)
                {
                    _logger.LogWarning($"Post {id} not found for deletion");
                    return NotFound();
                }

                // Kiểm tra người dùng có quyền xóa không
                if (post.UserId != userId)
                {
                    _logger.LogWarning($"User {userId} attempted to delete post {id} without permission");
                    return Forbid();
                }

                // Xóa bài viết
                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {userId} deleted post {id}");

                TempData["SuccessMessage"] = "Đã xóa bài viết thành công!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting post {id}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa bài viết";
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// API lấy danh sách bài viết
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPosts(int page = 1, int pageSize = 10)
        {
            try
            {
                // Lấy ID của người dùng hiện tại (nếu đã đăng nhập)
                int? currentUserId = null;
                if (User.Identity.IsAuthenticated)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.IsNullOrEmpty(userId))
                    {
                        currentUserId = int.Parse(userId);
                    }
                }

                // Lấy danh sách bài viết
                var posts = await _context.Posts
                    .Include(p => p.User)
                    .Include(p => p.Comments)
                    .Include(p => p.Likes)
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new PostViewModel
                    {
                        Id = p.Id,
                        Content = p.Content,
                        ImageUrl = p.ImageUrl,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                        UserId = p.UserId,
                        Username = p.User.Username,
                        UserFullName = p.User.FullName,
                        ProfilePicture = p.User.ProfilePicture,
                        CommentCount = p.Comments.Count,
                        LikeCount = p.Likes.Count,
                        CanEdit = currentUserId.HasValue && p.UserId == currentUserId.Value,
                        CanDelete = currentUserId.HasValue && p.UserId == currentUserId.Value,
                        IsLiked = currentUserId.HasValue && p.Likes.Any(l => l.UserId == currentUserId.Value)
                    })
                    .ToListAsync();

                return Json(posts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading posts");
                return StatusCode(500, "Có lỗi xảy ra khi tải bài viết");
            }
        }

        /// <summary>
        /// API lấy danh sách bài viết của người dùng cụ thể
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUserPosts(int id)
        {
            try
            {
                // Lấy ID của người dùng hiện tại (nếu đã đăng nhập)
                int? currentUserId = null;
                if (User.Identity.IsAuthenticated)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.IsNullOrEmpty(userId))
                    {
                        currentUserId = int.Parse(userId);
                    }
                }

                // Lấy danh sách bài viết của người dùng
                var posts = await _context.Posts
                    .Include(p => p.User)
                    .Include(p => p.Comments)
                    .Include(p => p.Likes)
                    .Where(p => p.UserId == id)
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new PostViewModel
                    {
                        Id = p.Id,
                        Content = p.Content,
                        ImageUrl = p.ImageUrl,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                        UserId = p.UserId,
                        Username = p.User.Username,
                        UserFullName = p.User.FullName,
                        ProfilePicture = p.User.ProfilePicture,
                        CommentCount = p.Comments.Count,
                        LikeCount = p.Likes.Count,
                        CanEdit = currentUserId.HasValue && p.UserId == currentUserId.Value,
                        CanDelete = currentUserId.HasValue && p.UserId == currentUserId.Value,
                        IsLiked = currentUserId.HasValue && p.Likes.Any(l => l.UserId == currentUserId.Value)
                    })
                    .ToListAsync();

                return Json(posts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading posts for user {id}");
                return StatusCode(500, "Có lỗi xảy ra khi tải bài viết");
            }
        }

        /// <summary>
        /// Hiển thị trang chi tiết bài viết
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            try
            {
                // Lấy ID người dùng hiện tại
                int? currentUserId = null;
                if (User.Identity.IsAuthenticated)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.IsNullOrEmpty(userId))
                    {
                        currentUserId = int.Parse(userId);
                    }
                }

                // Lấy chi tiết bài viết
                var post = await _context.Posts
                    .Include(p => p.User)
                    .Include(p => p.Comments)
                    .Include(p => p.Likes)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (post == null)
                {
                    return NotFound();
                }

                var postViewModel = new PostViewModel
                {
                    Id = post.Id,
                    Content = post.Content,
                    ImageUrl = post.ImageUrl,
                    CreatedAt = post.CreatedAt,
                    UpdatedAt = post.UpdatedAt,
                    UserId = post.UserId,
                    Username = post.User.Username,
                    UserFullName = post.User.FullName,
                    ProfilePicture = post.User.ProfilePicture,
                    CommentCount = post.Comments.Count,
                    LikeCount = post.Likes.Count,
                    CanEdit = currentUserId.HasValue && post.UserId == currentUserId.Value,
                    CanDelete = currentUserId.HasValue && post.UserId == currentUserId.Value,
                    IsLiked = currentUserId.HasValue && post.Likes.Any(l => l.UserId == currentUserId.Value)
                };

                return View(postViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting post detail {id}");
                return StatusCode(500, "Có lỗi xảy ra khi tải chi tiết bài viết");
            }
        }
    }
} 