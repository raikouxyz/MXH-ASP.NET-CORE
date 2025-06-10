using MXH_ASP.NET_CORE.Models;
using System;
using System.Collections.Generic;

namespace MXH_ASP.NET_CORE.Seeders
{
    public static class LikeSeeder
    {
        public static List<Like> Seed(List<User> users, List<Post> posts)
        {
            var likes = new List<Like>();
            var rand = new Random();
            for (int i = 0; i < 50; i++)
            {
                var user = users[rand.Next(users.Count)];
                var post = posts[rand.Next(posts.Count)];
                likes.Add(new Like
                {
                    UserId = user.Id,
                    PostId = post.Id,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-rand.Next(1000))
                });
            }
            return likes;
        }
    }
}
