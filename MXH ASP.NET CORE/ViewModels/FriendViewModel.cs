using MXH_ASP.NET_CORE.Models;
using System.ComponentModel.DataAnnotations;

namespace MXH_ASP.NET_CORE.ViewModels
{
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