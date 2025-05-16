using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MXH_ASP.NET_CORE.Data;
using MXH_ASP.NET_CORE.Models;
using MXH_ASP.NET_CORE.ViewModels;
using System.Diagnostics;
using System.Security.Claims;

namespace MXH_ASP.NET_CORE.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Nếu người dùng đã đăng nhập, hiển thị bài viết, nếu không hiển thị trang welcome
            if (User.Identity.IsAuthenticated)
            {
                // Lấy avatar và tên từ database
                var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
                string profilePicture = "/uploads/avatars/default-avatar.svg";
                string userName = "";
                if (!string.IsNullOrEmpty(userId))
                {
                    var user = await _context.Users.FindAsync(int.Parse(userId));
                    if (user != null)
                    {
                        if (!string.IsNullOrEmpty(user.ProfilePicture))
                        {
                            profilePicture = user.ProfilePicture;
                        }
                        userName = user.FullName ?? user.Username;
                    }
                }
                ViewBag.CurrentUserAvatar = profilePicture;
                ViewBag.CurrentUserName = userName;
                // Khởi tạo view model trống để sử dụng trong form đăng bài viết
                var createPostViewModel = new CreatePostViewModel();
                return View("Feed", createPostViewModel);
            }
            else
            {
                return View("Welcome");
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
} 