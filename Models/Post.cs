using System;
using System.Collections.Generic;

namespace Read_It.Models
{
    public class Post
    {
        public int Id { get; set; }
        
        public int CourseId { get; set; }
        public virtual Course? Course { get; set; }

        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser? User { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public PostFlair Flair { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int UpVotes { get; set; }
        public int DownVotes { get; set; }
        public bool IsPinned { get; set; }
        public bool IsLocked { get; set; }
        public bool IsAnonymous { get; set; }
        public int? AcceptedCommentId { get; set; }
        public DateTime? ExpiresAt { get; set; }

        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<PostBookmark> Bookmarks { get; set; } = new List<PostBookmark>();
    }
}
