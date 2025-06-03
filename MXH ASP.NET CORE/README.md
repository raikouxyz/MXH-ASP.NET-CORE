# MXH ASP.NET CORE

## Giới thiệu

Đây là dự án xây dựng một nền tảng Mạng Xã Hội (MXH) sử dụng ASP.NET Core 8.0. Ứng dụng cho phép người dùng đăng ký, đăng nhập, đăng bài viết, bình luận, kết bạn, nhắn tin, thích bài viết, tìm kiếm, quản lý hồ sơ cá nhân và nhiều tính năng khác giống như các mạng xã hội phổ biến hiện nay.

## Tính năng chính

- Đăng ký, đăng nhập, xác thực người dùng (Cookie Authentication)
- Quản lý hồ sơ cá nhân (Profile)
- Đăng bài viết, hình ảnh
- Bình luận, thích (like) bài viết
- Kết bạn, chấp nhận hoặc từ chối lời mời kết bạn
- Nhắn tin trực tiếp giữa các người dùng (Chat/Message)
- Tìm kiếm người dùng, bài viết
- Quản trị viên: Quản lý người dùng, bài viết
- Lưu bài viết yêu thích
- Seed dữ liệu mẫu cho phát triển nhanh

## Công nghệ sử dụng

- **Backend:** ASP.NET Core 8.0, Entity Framework Core 9
- **Frontend:** Razor Pages (Views)
- **Cơ sở dữ liệu:** SQL Server
- **Realtime:** AJAX (cho chat)
- **Xác thực:** Cookie Authentication, Identity
- **Các package:** BCrypt.Net, Microsoft.AspNetCore.Identity, EntityFrameworkCore

## Cấu trúc thư mục

```plaintext
├── Controllers/         // Xử lý các request API (Account, Admin, Post, Comment, Message, Friend, Profile, Like, FavoritePost, Search, Home)
├── Models/              // Định nghĩa các thực thể dữ liệu (User, Post, Comment, Message, Like, Friendship, ...)
├── Data/                // ApplicationDbContext, quản lý kết nối và migration database
├── Services/            // Các service nghiệp vụ (FavoritePost, SmsSender, ...)
├── Seeders/             // Tạo dữ liệu mẫu cho database (UserSeeder, PostSeeder, ...)
├── ViewModels/          // Các lớp truyền dữ liệu giữa backend và frontend
├── Helpers/             // Các hàm tiện ích (ví dụ: OtpHelper)
├── Views/               // Giao diện Razor Pages (Account, Admin, Post, Message, Profile, ...)
├── wwwroot/             // Tài nguyên tĩnh (ảnh, css, js)
├── Areas/Identity/      // Quản lý xác thực, đăng ký, đăng nhập, quên mật khẩu
├── Program.cs           // Điểm khởi động ứng dụng, cấu hình middleware
├── appsettings.json     // Cấu hình ứng dụng (connection string, SMS, logging)
├── MXH ASP.NET CORE.csproj // Cấu hình project, các package sử dụng
└── README.md            // Tài liệu mô tả dự án
```

## Hướng dẫn cài đặt & chạy dự án

1. **Clone dự án:**
   ```bash
   git clone <link-repo>
   ```

2. **Cài đặt các package cần thiết:**
   ```bash
   dotnet restore
   ```

3. **Cấu hình chuỗi kết nối database trong `appsettings.json`:**
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Database=MXH_ASP_NET_CORE;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
   }
   ```

4. **Chạy migration để tạo database:**
   ```bash
   dotnet ef database update
   ```

5. **Chạy ứng dụng:**
   ```bash
   dotnet run
   ```
