using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MXH_ASP.NET_CORE.Models
{
    /// <summary>
    /// Model đại diện cho mối quan hệ bạn bè trong hệ thống
    /// </summary>
    public class Friendship
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RequesterId { get; set; }

        [Required]
        public int AddresseeId { get; set; }

        public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("RequesterId")]
        public virtual User Requester { get; set; }

        [ForeignKey("AddresseeId")]
        public virtual User Addressee { get; set; }
    }

    /// <summary>
    /// Enum định nghĩa trạng thái của mối quan hệ bạn bè
    /// </summary>
    public enum FriendshipStatus
    {
        Pending,    // Đang chờ xác nhận
        Accepted,   // Đã chấp nhận
        Rejected,   // Đã từ chối
        Blocked     // Đã chặn
    }
} 