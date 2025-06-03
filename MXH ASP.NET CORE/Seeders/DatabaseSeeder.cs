using MXH_ASP.NET_CORE.Data;
using MXH_ASP.NET_CORE.Models;
using System.Linq;

namespace MXH_ASP.NET_CORE.Seeders
{
    public static class DatabaseSeeder
    {
        public static void Seed(ApplicationDbContext context)
        {
            // Chỉ seed khi database chưa có dữ liệu
            if (!context.Users.Any())
            {
                // 1. Seed User
                var users = UserSeeder.Seed();
                context.Users.AddRange(users);
                context.SaveChanges();

                // Lấy lại danh sách user đã có Id
                users = context.Users.ToList();

                // 2. Seed Post
                var posts = PostSeeder.Seed(users);
                context.Posts.AddRange(posts);
                context.SaveChanges();

                // Lấy lại danh sách post đã có Id
                posts = context.Posts.ToList();

                // 3. Seed Comment
                var comments = CommentSeeder.Seed(users, posts);
                context.Comments.AddRange(comments);

                // 4. Seed Like
                var likes = LikeSeeder.Seed(users, posts);
                context.Likes.AddRange(likes);

                // 5. Seed Friendship
                var friendships = FriendshipSeeder.Seed(users);
                context.Friendships.AddRange(friendships);

                // 6. Seed Message
                var messages = MessageSeeder.Seed(users);
                context.Messages.AddRange(messages);

                // 7. Seed FavoritePost
                var favorites = FavoritePostSeeder.Seed(users, posts);
                context.FavoritePosts.AddRange(favorites);

                // 8. Seed PostImage
                var images = PostImageSeeder.Seed(posts);
                context.PostImages.AddRange(images);

                // Lưu tất cả thay đổi còn lại
                context.SaveChanges();
            }
        }
    }
}
