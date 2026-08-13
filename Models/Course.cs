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
        public string? IconImageUrl { get; set; }
        public string? BannerImageUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// True = General course visible to all departments (e.g. English, Basic Math).
        /// General courses have no CourseDepartment rows — they're tied to no specific department.
        /// False = DepartmentSpecific — must have at least one CourseDepartment row.
        /// </summary>
        public bool IsGeneral { get; set; } = false;

        // Navigation properties
        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
        public virtual ICollection<CourseResource> Resources { get; set; } = new List<CourseResource>();
        public virtual ICollection<CourseMembership> Memberships { get; set; } = new List<CourseMembership>();

        /// <summary>Many-to-many: which departments this course belongs to (empty for General courses).</summary>
        public virtual ICollection<CourseDepartment> CourseDepartments { get; set; } = new List<CourseDepartment>();

        /// <summary>Users who follow this specific subject page.</summary>
        public virtual ICollection<CourseFollow> Followers { get; set; } = new List<CourseFollow>();

        /// <summary>User-submitted YouTube videos grouped by topic.</summary>
        public virtual ICollection<CourseVideo> Videos { get; set; } = new List<CourseVideo>();
    }
}
