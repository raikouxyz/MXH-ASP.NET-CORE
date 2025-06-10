using MXH_ASP.NET_CORE.Models;
using System;
using System.Collections.Generic;

namespace MXH_ASP.NET_CORE.Seeders
{
    public static class FavoritePostSeeder
    {
        public static List<FavoritePost> Seed(List<User> users, List<Post> posts)
        {
            var favorites = new List<FavoritePost>();
            var rand = new Random();
            for (int i = 0; i < 50; i++)
            {
                var user = users[rand.Next(users.Count)];
                var post = posts[rand.Next(posts.Count)];
                favorites.Add(new FavoritePost
                {
                    UserId = user.Id,
                    PostId = post.Id,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-rand.Next(1000))
                });
            }
            return favorites;
        }
    }
}
