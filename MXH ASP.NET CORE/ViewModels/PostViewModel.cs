using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MXH_ASP.NET_CORE.ViewModels
{
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
        public IFormFile? ImageFile { get; set; }
    }

    /// <summary>
    /// ViewModel để hiển thị bài viết
    /// </summary>
    public class PostViewModel
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public string ImageUrl { get; set; }
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
    }
} 