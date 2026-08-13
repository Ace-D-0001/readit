using System;

namespace Read_It.Models
{
    /// <summary>
    /// A user-submitted YouTube video for a subject page, grouped under a topic heading.
    /// Multiple users can submit under the same topic; within a topic, highest UpVotes shown first.
    /// </summary>
    public class CourseVideo
    {
        public int Id { get; set; }

        public int CourseId { get; set; }
        public virtual Course? Course { get; set; }

        /// <summary>Topic heading, e.g. "Recursion", "Binary Trees", "SQL Joins"</summary>
        public string Topic { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        public string VideoUrl { get; set; } = string.Empty;   // YouTube URL

        public string SubmittedByUserId { get; set; } = string.Empty;
        public virtual ApplicationUser? SubmittedByUser { get; set; }

        public int UpVotes { get; set; } = 0;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}
