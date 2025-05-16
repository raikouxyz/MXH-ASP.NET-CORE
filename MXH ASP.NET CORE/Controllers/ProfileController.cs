using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MXH_ASP.NET_CORE.Data;
using MXH_ASP.NET_CORE.Models;
using MXH_ASP.NET_CORE.ViewModels;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace MXH_ASP.NET_CORE.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            ApplicationDbContext context, 
            IWebHostEnvironment webHostEnvironment,
            ILogger<ProfileController> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        // Hiển thị trang profile của người dùng hiện tại
        [HttpGet]
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Accessing Profile/Index");
                
                if (!User.Identity.IsAuthenticated)
                {
                    _logger.LogWarning("User is not authenticated");
                    return RedirectToAction("Login", "Account");
                }

                var username = User.Identity?.Name;
                _logger.LogInformation($"Current user: {username}");

                if (string.IsNullOrEmpty(username))
                {
                    _logger.LogWarning("User.Identity.Name is null or empty");
                    return RedirectToAction("Login", "Account");
                }

                // Tìm user theo username hoặc email
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username || u.Email == username);

                if (user == null)
                {
                    _logger.LogWarning($"User not found in database: {username}");
                    // Đăng xuất người dùng vì không tìm thấy trong database
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    TempData["ErrorMessage"] = "Tài khoản không tồn tại hoặc đã bị xóa. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Account");
                }

                _logger.LogInformation($"Found user: {user.Username}");

                var viewModel = new UserProfileViewModel
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    ProfilePicture = user.ProfilePicture,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    IsCurrentUser = true
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Profile/Index");
                return View("Error");
            }
        }

        // Hiển thị trang profile của người dùng khác
        [HttpGet]
        [Route("Index/{id}")]
        public async Task<IActionResult> Index(int id)
        {
            try
            {
                _logger.LogInformation($"Accessing Profile/Index/{id}");
                
                if (!User.Identity.IsAuthenticated)
                {
                    _logger.LogWarning("User is not authenticated");
                    return RedirectToAction("Login", "Account");
                }

                // Tìm user theo ID
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    _logger.LogWarning($"User not found with ID: {id}");
                    TempData["ErrorMessage"] = "Không tìm thấy người dùng này.";
                    return RedirectToAction("Index", "Home");
                }

                _logger.LogInformation($"Found user: {user.Username}");

                // Kiểm tra xem có phải profile của người dùng hiện tại không
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var isCurrentUser = currentUserId == id;

                var viewModel = new UserProfileViewModel
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    ProfilePicture = user.ProfilePicture,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    IsCurrentUser = isCurrentUser
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Profile/Index/{id}");
                return View("Error");
            }
        }

        // Hiển thị form chỉnh sửa profile
        [HttpGet]
        [Route("Edit")]
        public async Task<IActionResult> Edit()
        {
            try
            {
                _logger.LogInformation("Accessing Profile/Edit");

                if (!User.Identity.IsAuthenticated)
                {
                    _logger.LogWarning("User is not authenticated");
                    return RedirectToAction("Login", "Account");
                }

                var username = User.Identity?.Name;
                _logger.LogInformation($"Current user: {username}");

                if (string.IsNullOrEmpty(username))
                {
                    _logger.LogWarning("User.Identity.Name is null or empty");
                    return RedirectToAction("Login", "Account");
                }

                // Tìm user theo username hoặc email
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username || u.Email == username);

                if (user == null)
                {
                    _logger.LogWarning($"User not found in database: {username}");
                    // Đăng xuất người dùng vì không tìm thấy trong database
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    TempData["ErrorMessage"] = "Tài khoản không tồn tại hoặc đã bị xóa. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Account");
                }

                _logger.LogInformation($"Found user: {user.Username}");

                var viewModel = new UserProfileViewModel
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    ProfilePicture = user.ProfilePicture
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Profile/Edit GET");
                return View("Error");
            }
        }

        // Xử lý cập nhật profile
        [HttpPost]
        [Route("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserProfileViewModel viewModel, IFormFile? profilePicture)
        {
            try
            {
                _logger.LogInformation("Processing Profile/Edit POST");

                if (!User.Identity.IsAuthenticated)
                {
                    _logger.LogWarning("User is not authenticated");
                    return RedirectToAction("Login", "Account");
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state in Profile/Edit POST");
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        _logger.LogWarning($"Model error: {error.ErrorMessage}");
                    }
                    return View(viewModel);
                }

                // Tìm user theo ID
                var user = await _context.Users.FindAsync(viewModel.Id);
                if (user == null)
                {
                    _logger.LogWarning($"User not found with ID: {viewModel.Id}");
                    // Đăng xuất người dùng vì không tìm thấy trong database
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    TempData["ErrorMessage"] = "Tài khoản không tồn tại hoặc đã bị xóa. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Account");
                }

                // Kiểm tra xem username mới có bị trùng không
                if (user.Username != viewModel.Username)
                {
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Username == viewModel.Username && u.Id != user.Id);
                    if (existingUser != null)
                    {
                        _logger.LogWarning($"Username already exists: {viewModel.Username}");
                        ModelState.AddModelError("Username", "Tên đăng nhập này đã tồn tại");
                        return View(viewModel);
                    }
                }

                // Kiểm tra xem email mới có bị trùng không
                if (user.Email != viewModel.Email)
                {
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == viewModel.Email && u.Id != user.Id);
                    if (existingUser != null)
                    {
                        _logger.LogWarning($"Email already exists: {viewModel.Email}");
                        ModelState.AddModelError("Email", "Email này đã được sử dụng");
                        return View(viewModel);
                    }
                }

                // Xử lý upload ảnh đại diện
                if (profilePicture != null)
                {
                    try
                    {
                        _logger.LogInformation("Processing profile picture upload");

                        // Xóa ảnh cũ nếu có
                        if (!string.IsNullOrEmpty(user.ProfilePicture) && 
                            !user.ProfilePicture.StartsWith("/uploads/avatars/default-avatar"))
                        {
                            var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, 
                                "uploads", "avatars", Path.GetFileName(user.ProfilePicture));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                                _logger.LogInformation($"Deleted old profile picture: {user.ProfilePicture}");
                            }
                        }

                        // Lưu ảnh mới
                        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(profilePicture.FileName)}";
                        var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "avatars", fileName);

                        // Tạo thư mục nếu chưa tồn tại
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await profilePicture.CopyToAsync(stream);
                        }

                        user.ProfilePicture = $"/uploads/avatars/{fileName}";
                        _logger.LogInformation($"Saved new profile picture: {fileName}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error uploading profile picture");
                        ModelState.AddModelError("", "Có lỗi xảy ra khi tải lên ảnh đại diện");
                        return View(viewModel);
                    }
                }

                // Cập nhật thông tin
                user.Username = viewModel.Username;
                user.Email = viewModel.Email;
                user.FullName = viewModel.FullName;

                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Updated user profile: {user.Username}");

                    // Thêm thông báo thành công
                    TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                    // Chuyển hướng về trang chủ để reload thông tin mới nhất
                    return RedirectToAction("Index", "Home");
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Database error while updating profile");
                    ModelState.AddModelError("", "Có lỗi xảy ra khi lưu thông tin. Vui lòng thử lại sau.");
                    return View(viewModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Profile/Edit POST");
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật thông tin");
                return View(viewModel);
            }
        }
    }
}