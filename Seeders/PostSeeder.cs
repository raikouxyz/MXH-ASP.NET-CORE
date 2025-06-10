using MXH_ASP.NET_CORE.Models;
using System;
using System.Collections.Generic;

namespace MXH_ASP.NET_CORE.Seeders
{
    public static class PostSeeder
    {
        public static List<Post> Seed(List<User> users)
        {
            var posts = new List<Post>();
            var rand = new Random();
            for (int i = 0; i < 50; i++)
            {
                // Gán ngẫu nhiên user cho mỗi post
                var user = users[rand.Next(users.Count)];
                posts.Add(new Post
                {
                    Content = $"Đây là bài viết số {i + 1} của {user.FullName}",
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-rand.Next(1000))
                });
            }
            return posts;
        }
    }
}