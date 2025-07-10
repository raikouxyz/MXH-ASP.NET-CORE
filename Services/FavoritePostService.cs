using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MXH_ASP.NET_CORE.Data;
using MXH_ASP.NET_CORE.Models;

namespace MXH_ASP.NET_CORE.Services
{
    public class FavoritePostService
    {
        private readonly ApplicationDbContext _context;

        public FavoritePostService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> AddToFavoritesAsync(string userId, int postId)
        {
            var userIdInt = int.Parse(userId);
            var existingFavorite = await _context.FavoritePosts
                .FirstOrDefaultAsync(f => f.UserId == userIdInt && f.PostId == postId);

            if (existingFavorite != null)
                return false;

            var favoritePost = new FavoritePost
            {
                UserId = userIdInt,
                PostId = postId,
                CreatedAt = DateTime.Now
            };

            _context.FavoritePosts.Add(favoritePost);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveFromFavoritesAsync(string userId, int postId)
        {
            var userIdInt = int.Parse(userId);
            var favoritePost = await _context.FavoritePosts
                .FirstOrDefaultAsync(f => f.UserId == userIdInt && f.PostId == postId);

            if (favoritePost == null)
                return false;

            _context.FavoritePosts.Remove(favoritePost);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsFavoriteAsync(string userId, int postId)
        {
            var userIdInt = int.Parse(userId);
            return await _context.FavoritePosts
                .AnyAsync(f => f.UserId == userIdInt && f.PostId == postId);
        }

        public async Task<IEnumerable<Post>> GetFavoritePostsAsync(string userId)
        {
            var userIdInt = int.Parse(userId);
            return await _context.FavoritePosts
                .Where(f => f.UserId == userIdInt)
                .Include(f => f.Post)
                    .ThenInclude(p => p.User)
                .Include(f => f.Post)
                    .ThenInclude(p => p.Images)
                .Include(f => f.Post)
                    .ThenInclude(p => p.Likes)
                .Include(f => f.Post)
                    .ThenInclude(p => p.Comments)
                .Select(f => f.Post)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
    }
} 