using System;
using System.Collections.Generic;

namespace Read_It.Models
{
    public class Comment
    {
        public int Id { get; set; }

        public int PostId { get; set; }
        public virtual Post? Post { get; set; }

        public int? ParentCommentId { get; set; }
        public virtual Comment? ParentComment { get; set; }

        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser? User { get; set; }

        public string Body { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int UpVotes { get; set; }
        public int DownVotes { get; set; }
        public bool IsAcceptedSolution { get; set; }

        public virtual ICollection<Comment> ChildComments { get; set; } = new List<Comment>();
    }
}
