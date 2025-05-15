using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MXH_ASP.NET_CORE.Data;
using MXH_ASP.NET_CORE.Models;
using MXH_ASP.NET_CORE.ViewModels;
using System.Security.Claims;

namespace MXH_ASP.NET_CORE.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CommentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CommentController> _logger;

        public CommentController(ApplicationDbContext context, ILogger<CommentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách bình luận của bài viết
        /// </summary>
        [HttpGet("post/{postId}")]
        public async Task<IActionResult> GetComments(int postId)
        {
            try
            {
                // Kiểm tra bài viết tồn tại không
                var post = await _context.Posts.FindAsync(postId);
                if (post == null)
                {
                    _logger.LogWarning($"Post {postId} not found when getting comments");
                    return NotFound(new { success = false, message = "Bài viết không tồn tại" });
                }

                // Lấy ID người dùng hiện tại (nếu đã đăng nhập)
                int? currentUserId = null;
                if (User.Identity.IsAuthenticated)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.IsNullOrEmpty(userId))
                    {
                        currentUserId = int.Parse(userId);
                    }
                }

                // Lấy danh sách bình luận
                var comments = await _context.Comments
                    .Where(c => c.PostId == postId)
                    .Include(c => c.User)
                    .OrderBy(c => c.CreatedAt)
                    .Select(c => new
                    {
                        Id = c.Id,
                        Content = c.Content,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt,
                        UserId = c.UserId,
                        Username = c.User.Username,
                        UserFullName = c.User.FullName,
                        ProfilePicture = c.User.ProfilePicture,
                        CanEdit = currentUserId.HasValue && c.UserId == currentUserId.Value,
                        CanDelete = currentUserId.HasValue && (c.UserId == currentUserId.Value || post.UserId == currentUserId.Value)
                    })
                    .ToListAsync();

                return Ok(new { success = true, comments });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting comments for post {postId}");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lấy bình luận" });
            }
        }

        /// <summary>
        /// Thêm bình luận mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddComment([FromBody] CommentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });
            }

            try
            {
                // Lấy ID người dùng hiện tại
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new { success = false, message = "Người dùng chưa đăng nhập" });
                }
                
                var userId = int.Parse(userIdClaim);

                // Kiểm tra bài viết tồn tại không
                var post = await _context.Posts.FindAsync(model.PostId);
                if (post == null)
                {
                    _logger.LogWarning($"Post {model.PostId} not found when adding comment");
                    return NotFound(new { success = false, message = "Bài viết không tồn tại" });
                }

                // Tạo bình luận mới
                var comment = new Comment
                {
                    Content = model.Content,
                    PostId = model.PostId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                // Lấy thông tin người dùng
                var user = await _context.Users.FindAsync(userId);

                _logger.LogInformation($"User {userId} added comment {comment.Id} to post {model.PostId}");

                // Trả về thông tin bình luận
                return Ok(new
                {
                    success = true,
                    comment = new
                    {
                        Id = comment.Id,
                        Content = comment.Content,
                        CreatedAt = comment.CreatedAt,
                        UpdatedAt = comment.UpdatedAt,
                        UserId = comment.UserId,
                        Username = user.Username,
                        UserFullName = user.FullName,
                        ProfilePicture = user.ProfilePicture,
                        CanEdit = true,
                        CanDelete = true
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding comment to post {model.PostId}");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi thêm bình luận" });
            }
        }

        /// <summary>
        /// Cập nhật bình luận
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateComment(int id, [FromBody] CommentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });
            }

            try
            {
                // Lấy ID người dùng hiện tại
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new { success = false, message = "Người dùng chưa đăng nhập" });
                }
                
                var userId = int.Parse(userIdClaim);

                // Lấy bình luận cần cập nhật
                var comment = await _context.Comments.FindAsync(id);
                if (comment == null)
                {
                    _logger.LogWarning($"Comment {id} not found");
                    return NotFound(new { success = false, message = "Bình luận không tồn tại" });
                }

                // Kiểm tra quyền chỉnh sửa
                if (comment.UserId != userId)
                {
                    _logger.LogWarning($"User {userId} attempted to edit comment {id} without permission");
                    return Forbid();
                }

                // Cập nhật bình luận
                comment.Content = model.Content;
                comment.UpdatedAt = DateTime.UtcNow;

                _context.Update(comment);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {userId} updated comment {id}");

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating comment {id}");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi cập nhật bình luận" });
            }
        }

        /// <summary>
        /// Xóa bình luận
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            try
            {
                // Lấy ID người dùng hiện tại
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new { success = false, message = "Người dùng chưa đăng nhập" });
                }
                
                var userId = int.Parse(userIdClaim);

                // Lấy bình luận cần xóa
                var comment = await _context.Comments
                    .Include(c => c.Post)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (comment == null)
                {
                    _logger.LogWarning($"Comment {id} not found");
                    return NotFound(new { success = false, message = "Bình luận không tồn tại" });
                }

                // Kiểm tra quyền xóa (người viết bình luận hoặc chủ bài viết)
                if (comment.UserId != userId && comment.Post.UserId != userId)
                {
                    _logger.LogWarning($"User {userId} attempted to delete comment {id} without permission");
                    return Forbid();
                }

                // Xóa bình luận
                _context.Comments.Remove(comment);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {userId} deleted comment {id}");

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting comment {id}");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi xóa bình luận" });
            }
        }
    }
} 