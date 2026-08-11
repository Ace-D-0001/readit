using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Read_It.Data
{
    public class ApplicationDbContext : IdentityDbContext<Models.ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Models.Course> Courses { get; set; } = null!;
        public DbSet<Models.CourseResource> CourseResources { get; set; } = null!;
        public DbSet<Models.Post> Posts { get; set; } = null!;
        public DbSet<Models.Comment> Comments { get; set; } = null!;
        public DbSet<Models.Vote> Votes { get; set; } = null!;
        public DbSet<Models.CourseMembership> CourseMemberships { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Composite Key for CourseMembership
            builder.Entity<Models.CourseMembership>()
                .HasKey(cm => new { cm.UserId, cm.CourseId });

            // Relationships
            builder.Entity<Models.Comment>()
                .HasOne(c => c.ParentComment)
                .WithMany(c => c.ChildComments)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Models.CourseResource>()
                .HasOne(cr => cr.Course)
                .WithMany(c => c.Resources)
                .HasForeignKey(cr => cr.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.Entity<Models.Post>()
                .HasOne(p => p.Course)
                .WithMany(c => c.Posts)
                .HasForeignKey(p => p.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
