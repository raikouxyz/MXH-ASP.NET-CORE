using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MXH_ASP.NET_CORE.Data;
using MXH_ASP.NET_CORE.Helpers;
using MXH_ASP.NET_CORE.Models;
using MXH_ASP.NET_CORE.Services;
using MXH_ASP.NET_CORE.ViewModels;
using System.Security.Claims;

namespace MXH_ASP.NET_CORE.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ISmsSender _smsSender;
        private readonly ILogger<AccountController> _logger;

        public AccountController(ApplicationDbContext context, ISmsSender smsSender, ILogger<AccountController> logger)
        {
            _context = context;
            _smsSender = smsSender;
            _logger = logger;
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra email đã tồn tại chưa
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng");
                    return View(model);
                }

                // Kiểm tra username đã tồn tại chưa
                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập này đã được sử dụng");
                    return View(model);
                }

                // Kiểm tra số điện thoại đã tồn tại chưa
                if (await _context.Users.AnyAsync(u => u.PhoneNumber == model.PhoneNumber))
                {
                    ModelState.AddModelError("PhoneNumber", "Số điện thoại này đã được sử dụng");
                    return View(model);
                }

                // Tạo user mới
                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password, BCrypt.Net.BCrypt.GenerateSalt()),
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    ProfilePicture = "/uploads/avatars/default-avatar.svg",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Tự động đăng nhập sau khi đăng ký
                await SignInUser(user);

                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users
                    .AsNoTracking()  // Sử dụng AsNoTracking để không cache
                    .FirstOrDefaultAsync(u => u.Username == model.Username || u.Email == model.Username);

                if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    // Lấy lại đầy đủ thông tin người dùng
                    user = await _context.Users.FindAsync(user.Id);
                    
                    // Cập nhật thời gian đăng nhập cuối
                    user.LastLoginAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    // Đăng nhập
                    await SignInUser(user, model.RememberMe);

                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng");
            }

            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/ForgotPassword
        /// <summary>
        /// Hiển thị form quên mật khẩu
        /// </summary>
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        /// <summary>
        /// Xử lý yêu cầu quên mật khẩu
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Tìm user theo số điện thoại
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == model.PhoneNumber);

                if (user == null)
                {
                    // Không tìm thấy user với số điện thoại này
                    ModelState.AddModelError(string.Empty, "Không tìm thấy tài khoản với số điện thoại này");
                    return View(model);
                }

                // Tạo mã OTP
                string otpCode = OtpHelper.GenerateOtp();
                
                // Lưu mã OTP
                OtpHelper.SaveOtp(model.PhoneNumber, otpCode);
                
                // Gửi mã OTP qua SMS
                string message = $"Mã xác thực đặt lại mật khẩu của bạn là: {otpCode}. Mã có hiệu lực trong 5 phút.";
                await _smsSender.SendSmsAsync(model.PhoneNumber, message);

                // Chuyển hướng đến trang xác thực OTP
                return RedirectToAction("VerifyOtp", new { phoneNumber = model.PhoneNumber });
            }

            return View(model);
        }

        // GET: /Account/VerifyOtp
        /// <summary>
        /// Hiển thị form xác thực OTP
        /// </summary>
        public IActionResult VerifyOtp(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
            {
                return RedirectToAction("ForgotPassword");
            }

            var model = new VerifyOtpViewModel
            {
                PhoneNumber = phoneNumber
            };
            
            return View(model);
        }

        // POST: /Account/VerifyOtp
        /// <summary>
        /// Xử lý xác thực OTP
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyOtp(VerifyOtpViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra mã OTP
                bool isValid = OtpHelper.VerifyOtp(model.PhoneNumber, model.OtpCode);
                
                if (isValid)
                {
                    // Chuyển hướng đến trang đặt lại mật khẩu
                    return RedirectToAction("ResetPassword", new { phoneNumber = model.PhoneNumber });
                }
                
                ModelState.AddModelError(string.Empty, "Mã xác thực không hợp lệ hoặc đã hết hạn");
            }
            
            return View(model);
        }

        // POST: /Account/ResendOtp
        /// <summary>
        /// Gửi lại mã OTP
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendOtp(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
            {
                return RedirectToAction("ForgotPassword");
            }

            // Tạo mã OTP mới
            string otpCode = OtpHelper.GenerateOtp();
            
            // Lưu mã OTP
            OtpHelper.SaveOtp(phoneNumber, otpCode);
            
            // Gửi mã OTP qua SMS
            string message = $"Mã xác thực đặt lại mật khẩu của bạn là: {otpCode}. Mã có hiệu lực trong 5 phút.";
            await _smsSender.SendSmsAsync(phoneNumber, message);

            // Thông báo
            TempData["SuccessMessage"] = "Mã xác thực mới đã được gửi lại";
            
            // Chuyển hướng đến trang xác thực OTP
            return RedirectToAction("VerifyOtp", new { phoneNumber });
        }

        // GET: /Account/ResetPassword
        /// <summary>
        /// Hiển thị form đặt lại mật khẩu
        /// </summary>
        public async Task<IActionResult> ResetPassword(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
            {
                return RedirectToAction("ForgotPassword");
            }

            // Kiểm tra số điện thoại có tồn tại trong hệ thống không
            var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
            if (user == null)
            {
                return RedirectToAction("ForgotPassword");
            }

            var model = new ResetPasswordViewModel
            {
                PhoneNumber = phoneNumber
            };
            
            return View(model);
        }

        // POST: /Account/ResetPassword
        /// <summary>
        /// Xử lý yêu cầu đặt lại mật khẩu
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Tìm user theo số điện thoại
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == model.PhoneNumber);

                if (user == null)
                {
                    // Không tìm thấy user với số điện thoại này
                    ModelState.AddModelError(string.Empty, "Không tìm thấy tài khoản với số điện thoại này");
                    return View(model);
                }

                // Cập nhật mật khẩu mới
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword, BCrypt.Net.BCrypt.GenerateSalt());
                await _context.SaveChangesAsync();

                // Thông báo thành công
                TempData["SuccessMessage"] = "Mật khẩu đã được đặt lại thành công. Vui lòng đăng nhập với mật khẩu mới.";
                return RedirectToAction("Login");
            }

            return View(model);
        }

        // GET: /Account/ChangePassword
        /// <summary>
        /// Hiển thị form đổi mật khẩu
        /// </summary>
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: /Account/ChangePassword
        /// <summary>
        /// Xử lý yêu cầu đổi mật khẩu
        /// </summary>
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Lấy thông tin người dùng hiện tại
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    // Đăng xuất nếu không tìm thấy user
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return RedirectToAction("Login");
                }

                // Kiểm tra mật khẩu hiện tại
                if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
                {
                    ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng");
                    return View(model);
                }

                // Cập nhật mật khẩu mới
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword, BCrypt.Net.BCrypt.GenerateSalt());
                await _context.SaveChangesAsync();

                // Thông báo thành công
                TempData["SuccessMessage"] = "Mật khẩu đã được thay đổi thành công";
                return RedirectToAction("Index", "Profile");
            }
            catch (Exception ex)
            {
                // Log lỗi
                _logger.LogError(ex, "Lỗi khi đổi mật khẩu");
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi đổi mật khẩu. Vui lòng thử lại sau.");
                return View(model);
            }
        }

        #region Private Methods

        private async Task SignInUser(User user, bool isPersistent = false)
        {
            // Ghi logs để debug
            Console.WriteLine($"Logging in user: {user.Id}, Username: {user.Username}, ProfilePicture: {user.ProfilePicture ?? "null"}");
            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("FullName", user.FullName ?? string.Empty),
                new Claim("ProfilePicture", user.ProfilePicture ?? string.Empty),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }

        #endregion
    }
} 