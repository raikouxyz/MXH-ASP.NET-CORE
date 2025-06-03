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
} 