using System;

namespace Read_It.Models
{
    public class Report
    {
        public int Id { get; set; }

        public ReportTargetType TargetType { get; set; }

        public int? PostId { get; set; }
        public virtual Post? Post { get; set; }

        public int? CommentId { get; set; }
        public virtual Comment? Comment { get; set; }

        public string ReportedByUserId { get; set; } = string.Empty;
        public virtual ApplicationUser? ReportedByUser { get; set; }

        public string Reason { get; set; } = string.Empty;
        public ReportStatus Status { get; set; } = ReportStatus.Pending;

        public string? AdminNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
    }
}
