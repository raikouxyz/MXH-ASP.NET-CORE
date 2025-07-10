using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace MXH_ASP.NET_CORE.Models
{
    /// <summary>
    /// Model đại diện cho bài viết trong hệ thống
    /// </summary>
    public class Post
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(5000)]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }

        // Foreign key
        [Required]
        public int UserId { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
        public virtual ICollection<Comment> Comments { get; set; }
        public virtual ICollection<Like> Likes { get; set; }
        public virtual ICollection<PostImage> Images { get; set; }

        public Post()
        {
            Comments = new List<Comment>();
            Likes = new List<Like>();
            Images = new List<PostImage>();
        }
    }

    /// <summary>
    /// ViewModel để tạo bài viết mới
    /// </summary>
    public class CreatePostViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập nội dung bài viết")]
        [StringLength(5000, ErrorMessage = "Nội dung tối đa 5000 ký tự")]
        [Display(Name = "Nội dung")]
        public string Content { get; set; }

        [Display(Name = "Hình ảnh")]
        public List<IFormFile> ImageFiles { get; set; } = new List<IFormFile>();
    }

    /// <summary>
    /// ViewModel để hiển thị bài viết
    /// </summary>
    public class PostViewModel
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public List<string> ImageUrls { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // Thông tin tác giả
        public int UserId { get; set; }
        public string Username { get; set; }
        public string UserFullName { get; set; }
        public string ProfilePicture { get; set; }
        
        // Thông tin tương tác
        public int CommentCount { get; set; }
        public int LikeCount { get; set; }
        
        // Cờ đánh dấu người dùng hiện tại có quyền chỉnh sửa hoặc xóa
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }

        // Cờ đánh dấu người dùng hiện tại đã thích bài viết này chưa
        public bool IsLiked { get; set; }

        // Thêm trường mới
        public bool IsFavorite { get; set; }

        public PostViewModel()
        {
            ImageUrls = new List<string>();
        }
    }
    
    /// <summary>
    /// ViewModel để chỉnh sửa bài viết
    /// </summary>
    public class EditPostViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Vui lòng nhập nội dung bài viết")]
        [StringLength(5000, ErrorMessage = "Nội dung tối đa 5000 ký tự")]
        [Display(Name = "Nội dung")]
        public string Content { get; set; }

        [Display(Name = "Hình ảnh mới")]
        public List<IFormFile>? NewImageFiles { get; set; }

        [Display(Name = "Hình ảnh hiện tại")]
        public List<string> ExistingImageUrls { get; set; }

        public EditPostViewModel()
        {
            ExistingImageUrls = new List<string>();
        }
    }
} 