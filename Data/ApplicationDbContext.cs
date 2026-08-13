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

        public DbSet<Models.Department> Departments { get; set; } = null!;
        public DbSet<Models.CourseDepartment> CourseDepartments { get; set; } = null!;
        public DbSet<Models.Course> Courses { get; set; } = null!;
        public DbSet<Models.CourseResource> CourseResources { get; set; } = null!;
        public DbSet<Models.CourseFollow> CourseFollows { get; set; } = null!;
        public DbSet<Models.CourseVideo> CourseVideos { get; set; } = null!;
        public DbSet<Models.Post> Posts { get; set; } = null!;
        public DbSet<Models.Comment> Comments { get; set; } = null!;
        public DbSet<Models.Vote> Votes { get; set; } = null!;
        public DbSet<Models.CourseMembership> CourseMemberships { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ----- CourseMembership: composite PK -----
            builder.Entity<Models.CourseMembership>()
                .HasKey(cm => new { cm.UserId, cm.CourseId });

            // ----- CourseDepartment: composite PK -----
            builder.Entity<Models.CourseDepartment>()
                .HasKey(cd => new { cd.CourseId, cd.DepartmentId });

            builder.Entity<Models.CourseDepartment>()
                .HasOne(cd => cd.Course)
                .WithMany(c => c.CourseDepartments)
                .HasForeignKey(cd => cd.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Models.CourseDepartment>()
                .HasOne(cd => cd.Department)
                .WithMany(d => d.CourseDepartments)
                .HasForeignKey(cd => cd.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // ----- CourseFollow: composite PK -----
            builder.Entity<Models.CourseFollow>()
                .HasKey(cf => new { cf.UserId, cf.CourseId });

            builder.Entity<Models.CourseFollow>()
                .HasOne(cf => cf.User)
                .WithMany(u => u.Following)
                .HasForeignKey(cf => cf.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Models.CourseFollow>()
                .HasOne(cf => cf.Course)
                .WithMany(c => c.Followers)
                .HasForeignKey(cf => cf.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // ----- CourseVideo -----
            builder.Entity<Models.CourseVideo>()
                .HasOne(v => v.Course)
                .WithMany(c => c.Videos)
                .HasForeignKey(v => v.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Models.CourseVideo>()
                .HasOne(v => v.SubmittedByUser)
                .WithMany()
                .HasForeignKey(v => v.SubmittedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ----- Comment self-reference -----
            builder.Entity<Models.Comment>()
                .HasOne(c => c.ParentComment)
                .WithMany(c => c.ChildComments)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            // ----- CourseResource -----
            builder.Entity<Models.CourseResource>()
                .HasOne(cr => cr.Course)
                .WithMany(c => c.Resources)
                .HasForeignKey(cr => cr.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // ----- Post -----
            builder.Entity<Models.Post>()
                .HasOne(p => p.Course)
                .WithMany(c => c.Posts)
                .HasForeignKey(p => p.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
