using System.Collections.Generic;
using System.Threading.Tasks;
using MXH_ASP.NET_CORE.Models;

namespace MXH_ASP.NET_CORE.Services
{
    public interface IFavoritePostService
    {
        /// <summary>
        /// Thêm bài viết vào danh sách yêu thích
        /// </summary>
        Task<bool> AddToFavoritesAsync(string userId, int postId);

        /// <summary>
        /// Xóa bài viết khỏi danh sách yêu thích
        /// </summary>
        Task<bool> RemoveFromFavoritesAsync(string userId, int postId);

        /// <summary>
        /// Kiểm tra bài viết có trong danh sách yêu thích không
        /// </summary>
        Task<bool> IsFavoriteAsync(string userId, int postId);

        /// <summary>
        /// Lấy danh sách bài viết yêu thích của người dùng
        /// </summary>
        Task<IEnumerable<Post>> GetFavoritePostsAsync(string userId);
    }
} 