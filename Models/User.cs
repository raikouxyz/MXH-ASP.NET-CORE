using System.ComponentModel.DataAnnotations;

namespace MXH_ASP.NET_CORE.Models
{
    /// <summary>
    /// Enum định nghĩa các vai trò người dùng trong hệ thống
    /// </summary>
    public enum UserRole
    {
        User = 0,    // Người dùng thường
        Admin = 1    // Quản trị viên
    }

    /// <summary>
    /// Model đại diện cho thông tin người dùng trong hệ thống
    /// </summary>
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [StringLength(100)]
        public string FullName { get; set; }

        [StringLength(15)]
        [Phone]
        public string PhoneNumber { get; set; }

        public string? ProfilePicture { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        // Thêm thuộc tính IsActive để quản lý trạng thái tài khoản
        public bool IsActive { get; set; } = true;

        // Sử dụng enum UserRole thay vì string
        [Required]
        public UserRole Role { get; set; } = UserRole.User; // Mặc định là User

        // Navigation properties
        public virtual ICollection<Post> Posts { get; set; }
        public virtual ICollection<Comment> Comments { get; set; }
        public virtual ICollection<Friendship> Friendships { get; set; }

        public User()
        {
            Posts = new List<Post>();
            Comments = new List<Comment>();
            Friendships = new List<Friendship>();
        }
    }

    /// <summary>
    /// ViewModel hiển thị thông tin hồ sơ người dùng
    /// </summary>
    public class UserProfileViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [StringLength(50, ErrorMessage = "Tên đăng nhập không được vượt quá 50 ký tự")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Display(Name = "Ảnh đại diện")]
        public string? ProfilePicture { get; set; }

        [Display(Name = "Ngày tham gia")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Đăng nhập lần cuối")]
        public DateTime? LastLoginAt { get; set; }

        public bool IsCurrentUser { get; set; }
    }

    /// <summary>
    /// ViewModel cho đăng nhập
    /// </summary>
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập hoặc email")]
        [Display(Name = "Tên đăng nhập hoặc Email")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }
    }

    /// <summary>
    /// ViewModel cho đăng ký tài khoản
    /// </summary>
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ 3-50 ký tự")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6-100 ký tự")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [RegularExpression(@"^(0[0-9]{9,10})$", ErrorMessage = "Số điện thoại không hợp lệ (phải bắt đầu bằng số 0 và có 10-11 chữ số)")]
        public string PhoneNumber { get; set; }

        [StringLength(100)]
        public string FullName { get; set; }
    }

    /// <summary>
    /// ViewModel cho đổi mật khẩu
    /// </summary>
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu hiện tại")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [StringLength(100, ErrorMessage = "Mật khẩu phải có ít nhất {2} ký tự.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu mới")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; }
    }

    /// <summary>
    /// ViewModel cho chức năng quên mật khẩu
    /// </summary>
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})$", ErrorMessage = "Số điện thoại không đúng định dạng Việt Nam")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; }
    }

    /// <summary>
    /// ViewModel cho chức năng xác thực mã OTP
    /// </summary>
    public class VerifyOtpViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã xác thực")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã xác thực phải có 6 ký tự")]
        [Display(Name = "Mã xác thực")]
        public string OtpCode { get; set; }
    }

    /// <summary>
    /// ViewModel cho chức năng đặt lại mật khẩu
    /// </summary>
    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [StringLength(100, ErrorMessage = "Mật khẩu phải có ít nhất {2} ký tự", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp")]
        public string ConfirmPassword { get; set; }
    }
} 