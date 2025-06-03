using MXH_ASP.NET_CORE.Models;
using System;
using System.Collections.Generic;

namespace MXH_ASP.NET_CORE.Seeders
{
    public static class CommentSeeder
    {
        public static List<Comment> Seed(List<User> users, List<Post> posts)
        {
            var comments = new List<Comment>();
            var rand = new Random();
            for (int i = 0; i < 50; i++)
            {
                var user = users[rand.Next(users.Count)];
                var post = posts[rand.Next(posts.Count)];
                comments.Add(new Comment
                {
                    Content = $"Bình luận số {i + 1} của {user.FullName} cho bài viết {post.Id}",
                    UserId = user.Id,
                    PostId = post.Id,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-rand.Next(1000))
                });
            }
            return comments;
        }
    }
}
