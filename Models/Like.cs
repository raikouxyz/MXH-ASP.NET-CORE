using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MXH_ASP.NET_CORE.Models
{
    /// <summary>
    /// Model đại diện cho lượt thích bài viết trong hệ thống
    /// </summary>
    public class Like
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PostId { get; set; }

        [Required]
        public int UserId { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        [ForeignKey("PostId")]
        public Post Post { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        public Like()
        {
            CreatedAt = DateTime.UtcNow;
        }
    }
} 