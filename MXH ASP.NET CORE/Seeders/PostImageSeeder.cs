using MXH_ASP.NET_CORE.Models;
using System.Collections.Generic;

namespace MXH_ASP.NET_CORE.Seeders
{
    public static class PostImageSeeder
    {
        public static List<PostImage> Seed(List<Post> posts)
        {
            var images = new List<PostImage>();
            for (int i = 0; i < 50; i++)
            {
                var post = posts[i % posts.Count];
                images.Add(new PostImage
                {
                    PostId = post.Id,
                    ImageUrl = $"/uploads/sample{i % 10 + 1}.jpg",
                    Order = 1
                });
            }
            return images;
        }
    }
}
