MXH ASP.NET CORE

Giới thiệu:
Dự án xây dựng nền tảng mạng xã hội bằng ASP.NET Core 8.0. Cho phép đăng ký, đăng nhập, đăng bài, bình luận, kết bạn, nhắn tin, thích bài viết, tìm kiếm, quản lý hồ sơ cá nhân, quản trị viên quản lý người dùng và bài viết.

Tính năng chính:
- Đăng ký, đăng nhập, xác thực người dùng
- Quản lý hồ sơ cá nhân
- Đăng bài viết, hình ảnh
- Bình luận, thích bài viết
- Kết bạn, chấp nhận/từ chối lời mời
- Nhắn tin trực tiếp
- Tìm kiếm người dùng, bài viết
- Quản trị viên quản lý người dùng, bài viết
- Lưu bài viết yêu thích
- Seed dữ liệu mẫu

Công nghệ sử dụng:
- Backend: ASP.NET Core 8.0, Entity Framework Core 9
- Frontend: Razor Pages (Views)
- Cơ sở dữ liệu: SQL Server
- Realtime: SignalR (chat)
- Xác thực: Cookie Authentication
- Các package: BCrypt.Net, Microsoft.AspNetCore.Identity, EntityFrameworkCore

Hướng dẫn cài đặt và chạy dự án:
1. Clone dự án: git clone https://github.com/raikouxyz/MXH-ASP.NET-CORE.git
2. Cài đặt package: dotnet restore
3. Cấu hình chuỗi kết nối database trong appsettings.json
   "DefaultConnection": "Server=localhost;Database=MXH_ASP_NET_CORE;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
4. Chạy migration: dotnet ef database update
5. Chạy ứng dụng: dotnet run
