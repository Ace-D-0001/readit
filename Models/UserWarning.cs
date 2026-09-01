using System;

namespace Read_It.Models
{
    public class UserWarning
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser? User { get; set; }

        public string AdminUserId { get; set; } = string.Empty;
        public string AdminUserName { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;
        public bool IsDismissed { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
