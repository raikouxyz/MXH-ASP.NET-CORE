using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MXH_ASP.NET_CORE.Services;

namespace MXH_ASP.NET_CORE.Controllers
{
    [Authorize]
    public class FavoritePostController : Controller
    {
        private readonly IFavoritePostService _favoritePostService;

        public FavoritePostController(IFavoritePostService favoritePostService)
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