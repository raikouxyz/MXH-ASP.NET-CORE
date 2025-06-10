using System.ComponentModel.DataAnnotations;

namespace MXH_ASP.NET_CORE.ViewModels
{
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
} 