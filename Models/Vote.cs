namespace Read_It.Models
{
    public class Vote
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser? User { get; set; }

        public VoteTargetType TargetType { get; set; }
        public int TargetId { get; set; }

        // 1 for Upvote, -1 for Downvote
        public int VoteValue { get; set; }
    }
}
