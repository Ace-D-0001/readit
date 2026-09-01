using System;

namespace Read_It.Models
{
    public class PostBookmark
    {
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser? User { get; set; }

        public int PostId { get; set; }
        public virtual Post? Post { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
