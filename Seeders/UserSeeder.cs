using MXH_ASP.NET_CORE.Models;
using System.Collections.Generic;

namespace MXH_ASP.NET_CORE.Seeders
{
    public static class UserSeeder
    {
        public static List<User> Seed()
        {
            var users = new List<User>();
            // Tạo 1 admin
            users.Add(new User
            {
                Username = "admin",
                Email = "admin@gmail.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("8865512b"),
                FullName = "Quản trị viên",
                Role = UserRole.Admin,
                IsActive = true,
                PhoneNumber = "0999999999"
            });

            // Tạo 49 user thường
            for (int i = 1; i < 50; i++)
            {
                users.Add(new User
                {
                    Username = $"user{i}",
                    Email = $"user{i}@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("user123"),
                    FullName = $"Người dùng {i}",
                    Role = UserRole.User,
                    IsActive = true,
                    PhoneNumber = $"01234567{i:D2}"
                });
            }
            return users;
        }
    }
}
