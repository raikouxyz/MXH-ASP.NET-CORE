using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; }
        
        public DateTime? UpdatedAt { get; set; }

        // Foreign key
        [Required]
        public int UserId { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public User User { get; set; }
        public ICollection<Comment> Comments { get; set; }
        public ICollection<Like> Likes { get; set; }

        public Post()
        {
            Comments = new List<Comment>();
            Likes = new List<Like>();
            CreatedAt = DateTime.UtcNow;
        }
    }
} 