using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Read_It.Data;
using Read_It.Models;

namespace Read_It
{
    public static class DbSeeder
    {
        public static void Seed(IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            context.Database.EnsureCreated();

            // 1. Seed Users if none exist
            if (!context.Users.Any())
            {
                var users = new List<ApplicationUser>
                {
                    new ApplicationUser { UserName = "prof_rahim@iubat.edu", Email = "prof_rahim@iubat.edu", Bio = "Assistant Professor, CSE Dept at IUBAT", EmailConfirmed = true },
                    new ApplicationUser { UserName = "tasmia_cse", Email = "tasmia@iubat.edu", Bio = "3rd Year BCSE Student | Algorithm enthusiast", EmailConfirmed = true },
                    new ApplicationUser { UserName = "tanvir_iubat", Email = "tanvir@iubat.edu", Bio = "Database & Web Systems learner", EmailConfirmed = true },
                    new ApplicationUser { UserName = "fahim_dev", Email = "fahim@iubat.edu", Bio = "Competitive programmer @ IUBAT", EmailConfirmed = true }
                };

                foreach (var user in users)
                {
                    userManager.CreateAsync(user, "Password123!").Wait();
                }
            }

            var authorProf = context.Users.FirstOrDefault(u => u.UserName == "prof_rahim@iubat.edu");
            var authorTasmia = context.Users.FirstOrDefault(u => u.UserName == "tasmia_cse");
            var authorTanvir = context.Users.FirstOrDefault(u => u.UserName == "tanvir_iubat");
            var authorFahim = context.Users.FirstOrDefault(u => u.UserName == "fahim_dev");

            // 2. Seed Courses if none exist
            if (!context.Courses.Any())
            {
                var course1 = new Course
                {
                    Code = "CSC247",
                    Title = "Computer Organization and Architecture",
                    Description = "Study of computer architecture, digital logic gates, CPU microarchitecture, and memory hierarchy at IUBAT.",
                    Department = "Computer Science and Engineering"
                };

                var course2 = new Course
                {
                    Code = "CSC391",
                    Title = "Data Structure and Algorithm",
                    Description = "In-depth study of arrays, trees, graphs, sorting, searching, and algorithmic complexity.",
                    Department = "Computer Science and Engineering"
                };

                var course3 = new Course
                {
                    Code = "CSC433",
                    Title = "Database Management Systems",
                    Description = "Relational database concepts, ER diagrams, SQL queries, normalization, and indexing.",
                    Department = "Computer Science and Engineering"
                };

                var course4 = new Course
                {
                    Code = "MAT247",
                    Title = "Numerical Analysis",
                    Description = "Numerical solution of algebraic equations, interpolation, numerical integration, and differential equations.",
                    Department = "Mathematics"
                };

                context.Courses.AddRange(course1, course2, course3, course4);
                context.SaveChanges();
            }

            var csc247 = context.Courses.FirstOrDefault(c => c.Code == "CSC247");
            var csc391 = context.Courses.FirstOrDefault(c => c.Code == "CSC391");
            var csc433 = context.Courses.FirstOrDefault(c => c.Code == "CSC433");
            var mat247 = context.Courses.FirstOrDefault(c => c.Code == "MAT247");

            // 3. Seed Course Resources if none exist
            if (!context.CourseResources.Any() && csc391 != null && csc433 != null && csc247 != null)
            {
                var resources = new List<CourseResource>
                {
                    new CourseResource { CourseId = csc391.Id, Type = CourseResourceType.Outline, Title = "CSC 391 Official Course Outline & Grading Policy", Url = "https://iubat.edu/syllabus/csc391.pdf", UploadedByUserId = authorProf?.Id, CreatedAt = DateTime.UtcNow.AddDays(-20) },
                    new CourseResource { CourseId = csc391.Id, Type = CourseResourceType.Playlist, Title = "Complete Data Structures & Algorithms Video Lectures", Url = "https://youtube.com/playlist?list=sample_dsa_iubat", UploadedByUserId = authorTasmia?.Id, CreatedAt = DateTime.UtcNow.AddDays(-15) },
                    new CourseResource { CourseId = csc391.Id, Type = CourseResourceType.QuestionBank, Title = "Midterm & Final Previous Year Question Bank (2022-2025)", Url = "https://drive.google.com/qbank-csc391", UploadedByUserId = authorFahim?.Id, CreatedAt = DateTime.UtcNow.AddDays(-10) },
                    
                    new CourseResource { CourseId = csc433.Id, Type = CourseResourceType.Notes, Title = "DBMS Lecture Notes & SQL Cheat Sheet", Url = "https://drive.google.com/dbms-notes-iubat", UploadedByUserId = authorTanvir?.Id, CreatedAt = DateTime.UtcNow.AddDays(-12) },
                    new CourseResource { CourseId = csc433.Id, Type = CourseResourceType.Playlist, Title = "SQL Joins & Indexing Video Guide", Url = "https://youtube.com/playlist?list=sample_dbms", UploadedByUserId = authorProf?.Id, CreatedAt = DateTime.UtcNow.AddDays(-8) },

                    new CourseResource { CourseId = csc247.Id, Type = CourseResourceType.Outline, Title = "CSC 247 Course Syllabus & Lab Manual", Url = "https://iubat.edu/syllabus/csc247.pdf", UploadedByUserId = authorProf?.Id, CreatedAt = DateTime.UtcNow.AddDays(-25) }
                };

                context.CourseResources.AddRange(resources);
                context.SaveChanges();
            }

            // 4. Seed Posts if none exist
            if (!context.Posts.Any() && csc391 != null && csc433 != null && authorProf != null && authorTasmia != null)
            {
                var post1 = new Post
                {
                    CourseId = csc391.Id,
                    UserId = authorProf.Id,
                    Title = "CSC 391 Midterm Exam Announcement & Practice Problems",
                    Body = "Dear students,\n\nThe Midterm Examination for CSC 391 (Data Structures and Algorithms) will cover Binary Search Trees, AVL Tree rotations, and Graph BFS/DFS algorithm analysis. Please make sure to attempt the practice problems uploaded in the course resources section.\n\nOffice hours are held every Sunday and Tuesday from 2:00 PM to 4:00 PM.",
                    Flair = PostFlair.Announcement,
                    UpVotes = 42,
                    DownVotes = 1,
                    IsPinned = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                };

                var post2 = new Post
                {
                    CourseId = csc391.Id,
                    UserId = authorTasmia.Id,
                    Title = "Detailed Explanation: QuickSort vs MergeSort Time Complexity & Space Tradeoffs",
                    Body = "Hey everyone! A lot of students asked about why MergeSort requires O(N) auxiliary space while QuickSort is in-place O(1) extra memory. Here is a quick breakdown:\n\n1. **MergeSort**: Guarantees O(N log N) worst-case time complexity, but requires extra space to merge sub-arrays.\n2. **QuickSort**: Average case is O(N log N), but worst-case can degenerate to O(N^2) if pivot selection is poor.\n\nHope this helps for the upcoming quiz!",
                    Flair = PostFlair.Notes,
                    UpVotes = 29,
                    DownVotes = 0,
                    IsPinned = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                };

                var post3 = new Post
                {
                    CourseId = csc433.Id,
                    UserId = authorTanvir != null ? authorTanvir.Id : authorTasmia.Id,
                    Title = "How to resolve 3NF vs BCNF normalization questions in DBMS?",
                    Body = "I am practicing SQL normalization questions for CSC 433 assignment 2. Can someone explain a simple trick to identify if a relation is in 3NF but fails BCNF? Specifically when there are overlapping candidate keys.",
                    Flair = PostFlair.Question,
                    UpVotes = 18,
                    DownVotes = 2,
                    IsPinned = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                };

                var post4 = new Post
                {
                    CourseId = csc433.Id,
                    UserId = authorFahim != null ? authorFahim.Id : authorTasmia.Id,
                    Title = "When the SQL query runs fine in your head vs when MySQL executes it",
                    Body = "Spent 3 hours debugging a syntax error only to realize I wrote `WHERE` after `GROUP BY` instead of `HAVING`... Stay hydrated folks! 😅",
                    Flair = PostFlair.Meme,
                    UpVotes = 65,
                    DownVotes = 3,
                    IsPinned = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-12)
                };

                context.Posts.AddRange(post1, post2, post3, post4);
                context.SaveChanges();

                // 5. Seed Comments for Post 3 & Post 2
                var comment1 = new Comment
                {
                    PostId = post3.Id,
                    UserId = authorProf.Id,
                    Body = "Great question Tanvir! Remember: A relation is in BCNF if for every functional dependency X -> Y, X is a superkey. In 3NF, Y can also be a prime attribute, which is allowed in 3NF but violates BCNF.",
                    UpVotes = 14,
                    DownVotes = 0,
                    CreatedAt = DateTime.UtcNow.AddDays(-1).AddHours(-5)
                };

                context.Comments.Add(comment1);
                context.SaveChanges();

                var reply1 = new Comment
                {
                    PostId = post3.Id,
                    ParentCommentId = comment1.Id,
                    UserId = authorTanvir != null ? authorTanvir.Id : authorTasmia.Id,
                    Body = "Thank you Professor! That clears it up completely. So BCNF strictly eliminates left-hand non-superkey dependencies regardless of prime attributes.",
                    UpVotes = 8,
                    DownVotes = 0,
                    CreatedAt = DateTime.UtcNow.AddDays(-1).AddHours(-3)
                };

                var reply2 = new Comment
                {
                    PostId = post3.Id,
                    ParentCommentId = comment1.Id,
                    UserId = authorFahim != null ? authorFahim.Id : authorTasmia.Id,
                    Body = "This concept always comes up in the final exam! Bookmark this thread guys.",
                    UpVotes = 5,
                    DownVotes = 0,
                    CreatedAt = DateTime.UtcNow.AddDays(-1).AddHours(-1)
                };

                context.Comments.AddRange(reply1, reply2);

                var commentPost2 = new Comment
                {
                    PostId = post2.Id,
                    UserId = authorFahim != null ? authorFahim.Id : authorProf.Id,
                    Body = "Awesome writeup Tasmia! Randomized QuickSort also helps prevent the O(N^2) worst case in practice.",
                    UpVotes = 10,
                    DownVotes = 0,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                };

                context.Comments.Add(commentPost2);
                context.SaveChanges();
            }
        }
    }
}
