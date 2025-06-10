using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MXH_ASP.NET_CORE.Data;
using MXH_ASP.NET_CORE.Models;
using System.Security.Claims;

namespace MXH_ASP.NET_CORE.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class LikeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LikeController> _logger;

        public LikeController(ApplicationDbContext context, ILogger<LikeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Thực hiện thích hoặc bỏ thích bài viết
        /// </summary>
        [HttpPost("{postId}")]
        public async Task<IActionResult> ToggleLike(int postId)
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

                // Kiểm tra bài viết tồn tại không
                var post = await _context.Posts.FindAsync(postId);
                if (post == null)
                {
                    _logger.LogWarning($"Post {postId} not found when attempting to like");
                    return NotFound(new { success = false, message = "Bài viết không tồn tại" });
                }

                // Kiểm tra xem người dùng đã thích bài viết chưa
                var existingLike = await _context.Likes
                    .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

                // Nếu đã thích thì bỏ thích, nếu chưa thích thì thích
                if (existingLike != null)
                {
                    // Bỏ thích
                    _context.Likes.Remove(existingLike);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"User {userId} unliked post {postId}");
                    
                    // Lấy số lượt thích hiện tại
                    var likeCount = await _context.Likes.CountAsync(l => l.PostId == postId);
                    
                    return Ok(new { success = true, liked = false, likeCount });
                }
                else
                {
                    // Thích
                    var like = new Like
                    {
                        PostId = postId,
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Likes.Add(like);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"User {userId} liked post {postId}");
                    
                    // Lấy số lượt thích hiện tại
                    var likeCount = await _context.Likes.CountAsync(l => l.PostId == postId);
                    
                    return Ok(new { success = true, liked = true, likeCount });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error toggling like for post {postId}");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi xử lý yêu cầu" });
            }
        }
    }
}