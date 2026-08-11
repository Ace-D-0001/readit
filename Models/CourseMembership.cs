using System;

namespace Read_It.Models
{
    public class CourseMembership
    {
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser? User { get; set; }

        public int CourseId { get; set; }
        public virtual Course? Course { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public CourseRole Role { get; set; } = CourseRole.Member;
    }
}
