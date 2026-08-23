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
        public string? Description { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        
        public ResourceStatus Status { get; set; } = ResourceStatus.Approved;
        public string? RejectionReason { get; set; }

        public string? UploadedByUserId { get; set; }
        public virtual ApplicationUser? UploadedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
