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

    /// <summary>
    /// ViewModel hiển thị danh sách bạn bè
    /// </summary>
    public class FriendViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string ProfilePicture { get; set; }
        public DateTime FriendsSince { get; set; }
        public bool CanUnfriend { get; set; } = true;
    }

    /// <summary>
    /// ViewModel hiển thị yêu cầu kết bạn
    /// </summary>
    public class FriendRequestViewModel
    {
        public int FriendshipId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string ProfilePicture { get; set; }
        public DateTime RequestDate { get; set; }
        public FriendshipStatus Status { get; set; }
    }

    /// <summary>
    /// ViewModel tổng hợp dùng cho trang danh sách bạn bè
    /// </summary>
    public class FriendListViewModel
    {
        public List<FriendViewModel> Friends { get; set; } = new List<FriendViewModel>();
        public List<FriendRequestViewModel> ReceivedRequests { get; set; } = new List<FriendRequestViewModel>();
        public List<FriendRequestViewModel> SentRequests { get; set; } = new List<FriendRequestViewModel>();
        public List<UserViewModel> SuggestedFriends { get; set; } = new List<UserViewModel>();
    }

    /// <summary>
    /// ViewModel đơn giản hiển thị thông tin người dùng để gợi ý kết bạn
    /// </summary>
    public class UserViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string ProfilePicture { get; set; }
    }
} 