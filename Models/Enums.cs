namespace Read_It.Models
{
    public enum CourseResourceType
    {
        Outline,
        Playlist,
        Notes,
        Other
    }

    public enum PostFlair
    {
        Question,
        Notes,
        Discussion,
        Meme,
        Announcement
    }

    public enum VoteTargetType
    {
        Post,
        Comment,
        Video
    }

    public enum CourseRole
    {
        Member,
        Moderator
    }

    public enum ReportTargetType
    {
        Post,
        Comment
    }

    public enum ReportStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public enum ResourceStatus
    {
        Pending,
        Approved,
        Rejected
    }
}
