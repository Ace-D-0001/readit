using System;

namespace Read_It.Models
{
    /// <summary>
    /// A user following a specific subReadIt (course page).
    /// Following is per-subject only — no department-level following.
    /// Following only affects a user's home feed visibility, not page access.
    /// </summary>
    public class CourseFollow
    {
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser? User { get; set; }

        public int CourseId { get; set; }
        public virtual Course? Course { get; set; }

        public DateTime FollowedAt { get; set; } = DateTime.UtcNow;
    }
}
