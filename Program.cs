using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MXH_ASP.NET_CORE.Data;
using MXH_ASP.NET_CORE.Models;
using MXH_ASP.NET_CORE.Services;
using MXH_ASP.NET_CORE.Seeders;
using MXH_ASP.NET_CORE.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllersWithViews();

// Thêm SignalR service để hỗ trợ real-time messaging
builder.Services.AddSignalR();

// Cấu hình xác thực Cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "VibeNet.Auth";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

// Đăng ký các dịch vụ
builder.Services.AddScoped<FavoritePostService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Thêm middleware xác thực và phân quyền
app.UseAuthentication();
app.UseAuthorization();

// Cấu hình routing mặc định
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map SignalR ChatHub để xử lý real-time messaging
app.MapHub<ChatHub>("/chatHub");

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
    MXH_ASP.NET_CORE.Seeders.DatabaseSeeder.Seed(context);
}

app.Run();
