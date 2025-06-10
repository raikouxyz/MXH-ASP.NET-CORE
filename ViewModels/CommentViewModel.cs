using System.ComponentModel.DataAnnotations;

namespace MXH_ASP.NET_CORE.ViewModels
{
    public class CommentViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập nội dung bình luận")]
        [MaxLength(1000, ErrorMessage = "Nội dung bình luận không được vượt quá 1000 ký tự")]
        public string Content { get; set; }

        [Required]
        public int PostId { get; set; }
    }
} 