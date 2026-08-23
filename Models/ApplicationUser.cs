using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace Read_It.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? Bio { get; set; }
        public string? Avatar { get; set; }
        public bool IsBanned { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<CourseMembership> Memberships { get; set; } = new List<CourseMembership>();
        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

        /// <summary>Subject pages (courses) this user follows individually.</summary>
        public virtual ICollection<CourseFollow> Following { get; set; } = new List<CourseFollow>();
    }
}
