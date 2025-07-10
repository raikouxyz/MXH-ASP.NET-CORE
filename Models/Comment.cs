using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MXH_ASP.NET_CORE.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Content { get; set; }

        [Required]
        public int PostId { get; set; }

        [Required]
        public int UserId { get; set; }

        public DateTime CreatedAt { get; set; }
        
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("PostId")]
        public Post Post { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        public Comment()
        {
            CreatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// ViewModel cho bình luận
    /// </summary>
    public class CommentViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập nội dung bình luận")]
        [MaxLength(1000, ErrorMessage = "Nội dung bình luận không được vượt quá 1000 ký tự")]
        public string Content { get; set; }

        [Required]
        public int PostId { get; set; }
    }
} 