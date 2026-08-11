using System;

namespace Read_It.Models
{
    public class CourseResource
    {
        public int Id { get; set; }
        
        public int CourseId { get; set; }
        public virtual Course? Course { get; set; }

        public CourseResourceType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        
        public string? UploadedByUserId { get; set; }
        public virtual ApplicationUser? UploadedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
