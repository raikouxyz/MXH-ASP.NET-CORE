using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MXH_ASP.NET_CORE.Data;
using System.Security.Claims;

namespace MXH_ASP.NET_CORE.Controllers
{
    public class BaseController : Controller
    {
        protected readonly ApplicationDbContext _context;

        public BaseController(ApplicationDbContext context)
        {
            _context = context;
        }

        protected async Task SetCurrentUserInfo()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    var user = await _context.Users.FindAsync(int.Parse(userId));
                    if (user != null)
                    {
                        ViewBag.CurrentUserAvatar = user.ProfilePicture ?? "/uploads/avatars/default-avatar.svg";
                        ViewBag.CurrentUserName = user.FullName ?? user.Username;
                        return;
                    }
                }
            }
            
            ViewBag.CurrentUserAvatar = "/uploads/avatars/default-avatar.svg";
            ViewBag.CurrentUserName = "";
        }
    }
} 