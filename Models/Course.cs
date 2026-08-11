using System;
using System.Collections.Generic;

namespace Read_It.Models
{
    public class Course
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string? IconImageUrl { get; set; }
        public string? BannerImageUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
        public virtual ICollection<CourseResource> Resources { get; set; } = new List<CourseResource>();
        public virtual ICollection<CourseMembership> Memberships { get; set; } = new List<CourseMembership>();
    }
}
