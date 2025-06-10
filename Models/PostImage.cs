using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MXH_ASP.NET_CORE.Models
{
    public class PostImage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PostId { get; set; }

        [Required]
        public string ImageUrl { get; set; }

        public int Order { get; set; }

        [ForeignKey("PostId")]
        public virtual Post Post { get; set; }
    }
}