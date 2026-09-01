using System;

namespace Read_It.Models
{
    public class AdminLog
    {
        public int Id { get; set; }

        public string AdminUserId { get; set; } = string.Empty;
        public string AdminUserName { get; set; } = string.Empty;

        public string ActionType { get; set; } = string.Empty;
        public string TargetDescription { get; set; } = string.Empty;
        public string? Details { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
