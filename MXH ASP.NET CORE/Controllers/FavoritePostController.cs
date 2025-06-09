using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MXH_ASP.NET_CORE.Data;
using MXH_ASP.NET_CORE.Services;

namespace MXH_ASP.NET_CORE.Controllers
{
    [Authorize]
    public class FavoritePostController : BaseController
    {
        private readonly IFavoritePostService _favoritePostService;

        public FavoritePostController(ApplicationDbContext context, IFavoritePostService favoritePostService) : base(context)
        {
            _favoritePostService = favoritePostService;
        }

        [HttpPost]
        public async Task<IActionResult> ToggleFavorite(int postId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var isFavorite = await _favoritePostService.IsFavoriteAsync(userId, postId);
            if (isFavorite)
            {
                await _favoritePostService.RemoveFromFavoritesAsync(userId, postId);
                return Json(new { success = true, action = "removed" });
            }
            else
            {
                await _favoritePostService.AddToFavoritesAsync(userId, postId);
                return Json(new { success = true, action = "added" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> MyFavorites()
        {
            await SetCurrentUserInfo(); // Gọi method từ base class

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var favoritePosts = await _favoritePostService.GetFavoritePostsAsync(userId);
            return View(favoritePosts);
        }

        [HttpGet]
        public async Task<IActionResult> IsFavorite(int postId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var isFavorite = await _favoritePostService.IsFavoriteAsync(userId, postId);
            return Json(new { isFavorite });
        }
    }
} 