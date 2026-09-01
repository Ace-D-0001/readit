using System;

namespace Read_It.Models
{
    public enum NotificationType
    {
        Reply,
        NoteApproved,
        NoteRejected,
        Warning,
        AcceptedSolution,
        Announcement,
        System
    }

    public class Notification
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser? User { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? LinkUrl { get; set; }
        public NotificationType Type { get; set; } = NotificationType.System;

        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
