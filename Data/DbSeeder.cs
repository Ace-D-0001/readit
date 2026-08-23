using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            context.Database.EnsureCreated();

            // ── 0. Roles ────────────────────────────────────────────────────────────────
            string[] roles = new[] { "Admin", "Student" };
            foreach (var r in roles)
            {
                if (!roleManager.RoleExistsAsync(r).Result)
                {
                    roleManager.CreateAsync(new IdentityRole(r)).Wait();
                }
            }

            // ── 1. Users ────────────────────────────────────────────────────────────────
            // Test Admin
            var adminUser = userManager.FindByEmailAsync("admin@iubat.edu").Result;
            if (adminUser == null)
            {
                adminUser = new ApplicationUser { UserName = "admin", Email = "admin@iubat.edu", Bio = "System Administrator", EmailConfirmed = true };
                userManager.CreateAsync(adminUser, "Admin123!").Wait();
                userManager.AddToRoleAsync(adminUser, "Admin").Wait();
            }

            // Test Student
            var studentUser = userManager.FindByEmailAsync("student@iubat.edu").Result;
            if (studentUser == null)
            {
                studentUser = new ApplicationUser { UserName = "student", Email = "student@iubat.edu", Bio = "Test Student Account", EmailConfirmed = true };
                userManager.CreateAsync(studentUser, "Student123!").Wait();
                userManager.AddToRoleAsync(studentUser, "Student").Wait();
            }

            if (!context.Users.Any(u => u.UserName == "prof_rahim"))
            {
                var users = new List<ApplicationUser>
                {
                    new ApplicationUser { UserName = "prof_rahim", Email = "prof_rahim@iubat.edu", Bio = "Assistant Professor, CSE Dept at IUBAT", EmailConfirmed = true },
                    new ApplicationUser { UserName = "tasmia_cse",  Email = "tasmia@iubat.edu",     Bio = "3rd Year BCSE Student | Algorithm enthusiast", EmailConfirmed = true },
                    new ApplicationUser { UserName = "tanvir_iubat",Email = "tanvir@iubat.edu",     Bio = "Database & Web Systems learner", EmailConfirmed = true },
                    new ApplicationUser { UserName = "fahim_dev",   Email = "fahim@iubat.edu",      Bio = "Competitive programmer @ IUBAT", EmailConfirmed = true },
                    new ApplicationUser { UserName = "nusrat_bba",  Email = "nusrat@iubat.edu",     Bio = "BBA Senior | Business & Communication", EmailConfirmed = true }
                };
                foreach (var u in users)
                {
                    userManager.CreateAsync(u, "Password123!").Wait();
                    userManager.AddToRoleAsync(u, "Student").Wait();
                }
            }

            var profRahim  = context.Users.FirstOrDefault(u => u.UserName == "prof_rahim");
            var tasmia     = context.Users.FirstOrDefault(u => u.UserName == "tasmia_cse");
            var tanvir     = context.Users.FirstOrDefault(u => u.UserName == "tanvir_iubat");
            var fahim      = context.Users.FirstOrDefault(u => u.UserName == "fahim_dev");
            var nusrat     = context.Users.FirstOrDefault(u => u.UserName == "nusrat_bba");

            // ── 2. Departments ──────────────────────────────────────────────────────────
            if (!context.Departments.Any())
            {
                context.Departments.AddRange(
                    new Department { Name = "Computer Science and Engineering", Abbreviation = "CSE" },
                    new Department { Name = "Business Administration",          Abbreviation = "BBA" },
                    new Department { Name = "Mathematics",                      Abbreviation = "MAT" },
                    new Department { Name = "English",                          Abbreviation = "ENG" }
                );
                context.SaveChanges();
            }

            var deptCSE = context.Departments.FirstOrDefault(d => d.Abbreviation == "CSE");
            var deptBBA = context.Departments.FirstOrDefault(d => d.Abbreviation == "BBA");
            var deptMAT = context.Departments.FirstOrDefault(d => d.Abbreviation == "MAT");
            var deptENG = context.Departments.FirstOrDefault(d => d.Abbreviation == "ENG");

            // ── 3. Courses ──────────────────────────────────────────────────────────────
            if (!context.Courses.Any())
            {
                // General courses — visible to ALL departments, no CourseDepartment rows
                var eng101 = new Course
                {
                    Code = "ENG101", Title = "English Language I", IsGeneral = true,
                    Description = "Foundation English communication, academic writing, and vocabulary skills common to all IUBAT departments."
                };
                var mat101 = new Course
                {
                    Code = "MAT101", Title = "Basic Mathematics", IsGeneral = true,
                    Description = "Core algebra, calculus foundations, and mathematical logic prerequisite common to every department at IUBAT."
                };

                // CSE-specific courses
                var csc247 = new Course
                {
                    Code = "CSC247", Title = "Computer Organization and Architecture", IsGeneral = false,
                    Description = "Study of computer architecture, digital logic gates, CPU microarchitecture, and memory hierarchy."
                };
                var csc391 = new Course
                {
                    Code = "CSC391", Title = "Data Structure and Algorithm", IsGeneral = false,
                    Description = "In-depth study of arrays, trees, graphs, sorting, searching, and algorithmic complexity."
                };
                var csc433 = new Course
                {
                    Code = "CSC433", Title = "Database Management Systems", IsGeneral = false,
                    Description = "Relational database concepts, ER diagrams, SQL queries, normalization, and indexing."
                };

                // Shared: CSE + MAT (cross-department course)
                var mat247 = new Course
                {
                    Code = "MAT247", Title = "Numerical Analysis", IsGeneral = false,
                    Description = "Numerical methods for algebraic equations, interpolation, integration, and differential equations. Offered to both CSE and Mathematics students."
                };

                context.Courses.AddRange(eng101, mat101, csc247, csc391, csc433, mat247);
                context.SaveChanges();

                // Attach CSE courses to CSE dept
                if (deptCSE != null)
                {
                    context.CourseDepartments.AddRange(
                        new CourseDepartment { CourseId = csc247.Id, DepartmentId = deptCSE.Id },
                        new CourseDepartment { CourseId = csc391.Id, DepartmentId = deptCSE.Id },
                        new CourseDepartment { CourseId = csc433.Id, DepartmentId = deptCSE.Id },
                        new CourseDepartment { CourseId = mat247.Id, DepartmentId = deptCSE.Id }
                    );
                }
                // MAT247 also belongs to Mathematics dept
                if (deptMAT != null)
                {
                    context.CourseDepartments.Add(
                        new CourseDepartment { CourseId = mat247.Id, DepartmentId = deptMAT.Id }
                    );
                }
                context.SaveChanges();
            }

            var courseCSC391 = context.Courses.FirstOrDefault(c => c.Code == "CSC391");
            var courseCSC433 = context.Courses.FirstOrDefault(c => c.Code == "CSC433");
            var courseCSC247 = context.Courses.FirstOrDefault(c => c.Code == "CSC247");
            var courseMAT247 = context.Courses.FirstOrDefault(c => c.Code == "MAT247");
            var courseENG101 = context.Courses.FirstOrDefault(c => c.Code == "ENG101");
            var courseMAT101 = context.Courses.FirstOrDefault(c => c.Code == "MAT101");

            // ── 4. Course Resources ─────────────────────────────────────────────────────
            if (!context.CourseResources.Any())
            {
                var resources = new List<CourseResource>();

                if (courseCSC391 != null)
                {
                    resources.Add(new CourseResource { CourseId = courseCSC391.Id, Type = CourseResourceType.Outline, Title = "CSC 391 Official Syllabus & Grading Criteria (Google Docs)", Url = "https://docs.google.com/document/d/1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms/edit", UploadedByUserId = profRahim?.Id, Status = ResourceStatus.Approved, CreatedAt = DateTime.UtcNow.AddDays(-20) });
                    resources.Add(new CourseResource { CourseId = courseCSC391.Id, Type = CourseResourceType.Outline, Title = "IUBAT Course Module Web Page", Url = "https://iubat.edu/programs/bcse/courses/csc391", UploadedByUserId = profRahim?.Id, Status = ResourceStatus.Approved, CreatedAt = DateTime.UtcNow.AddDays(-19) });
                    resources.Add(new CourseResource { CourseId = courseCSC391.Id, Type = CourseResourceType.Notes, Title = "CSC 391 Solved Exam Papers & Notes (Google Drive)", Url = "https://drive.google.com/drive/folders/1QBank_CSC391_IUBAT", UploadedByUserId = fahim?.Id, Status = ResourceStatus.Approved, CreatedAt = DateTime.UtcNow.AddDays(-10) });
                    resources.Add(new CourseResource { CourseId = courseCSC391.Id, Type = CourseResourceType.Notes, Title = "Data Structures Interactive Visualization Guide", Url = "https://visualgo.net/en", UploadedByUserId = tasmia?.Id, Status = ResourceStatus.Approved, CreatedAt = DateTime.UtcNow.AddDays(-7) });
                }

                if (courseCSC433 != null)
                {
                    resources.Add(new CourseResource { CourseId = courseCSC433.Id, Type = CourseResourceType.Outline, Title = "CSC 433 Course Syllabus & Weekly Topics (Notion)", Url = "https://iubat-cse.notion.site/CSC433-DBMS-Syllabus-2026", UploadedByUserId = profRahim?.Id, Status = ResourceStatus.Approved, CreatedAt = DateTime.UtcNow.AddDays(-18) });
                    resources.Add(new CourseResource { CourseId = courseCSC433.Id, Type = CourseResourceType.Notes, Title = "DBMS Midterm & Final Study Notes Collection", Url = "https://drive.google.com/drive/folders/1DBMS_QBank_Collection", UploadedByUserId = tanvir?.Id, Status = ResourceStatus.Approved, CreatedAt = DateTime.UtcNow.AddDays(-6) });
                    resources.Add(new CourseResource { CourseId = courseCSC433.Id, Type = CourseResourceType.Notes, Title = "SQL Syntax Reference & Exercises", Url = "https://www.w3schools.com/sql/", UploadedByUserId = tanvir?.Id, Status = ResourceStatus.Approved, CreatedAt = DateTime.UtcNow.AddDays(-12) });
                }

                if (courseCSC247 != null)
                {
                    resources.Add(new CourseResource { CourseId = courseCSC247.Id, Type = CourseResourceType.Outline, Title = "CSC 247 Course Plan & Lab Experiments Link", Url = "https://github.com/iubat-cse/csc247-architecture-outline", UploadedByUserId = profRahim?.Id, Status = ResourceStatus.Approved, CreatedAt = DateTime.UtcNow.AddDays(-25) });
                    resources.Add(new CourseResource { CourseId = courseCSC247.Id, Type = CourseResourceType.Notes, Title = "Logic Gates & CPU Architecture Study Notes", Url = "https://drive.google.com/drive/folders/1CSC247_Arch_Exams", UploadedByUserId = fahim?.Id, Status = ResourceStatus.Approved, CreatedAt = DateTime.UtcNow.AddDays(-15) });
                }

                if (courseMAT247 != null)
                {
                    resources.Add(new CourseResource { CourseId = courseMAT247.Id, Type = CourseResourceType.Outline, Title = "MAT 247 Numerical Methods Course Topics Webpage", Url = "https://iubat.edu/programs/math/courses/mat247", UploadedByUserId = profRahim?.Id, Status = ResourceStatus.Approved, CreatedAt = DateTime.UtcNow.AddDays(-14) });
                    resources.Add(new CourseResource { CourseId = courseMAT247.Id, Type = CourseResourceType.Notes, Title = "Numerical Analysis Solved Study Notes", Url = "https://drive.google.com/drive/folders/1MAT247_Numerical_QBank", UploadedByUserId = tasmia?.Id, Status = ResourceStatus.Approved, CreatedAt = DateTime.UtcNow.AddDays(-8) });
                }

                if (courseENG101 != null)
                {
                    resources.Add(new CourseResource { CourseId = courseENG101.Id, Type = CourseResourceType.Outline, Title = "ENG 101 Course Structure & Essay Guidelines", Url = "https://docs.google.com/document/d/1ENG101_IUBAT_Syllabus", UploadedByUserId = nusrat?.Id, Status = ResourceStatus.Approved, CreatedAt = DateTime.UtcNow.AddDays(-30) });
                }

                context.CourseResources.AddRange(resources);
                context.SaveChanges();
            }

            // ── 5. YouTube Videos by Topic ──────────────────────────────────────────────
            if (!context.CourseVideos.Any())
            {
                var videos = new List<CourseVideo>();

                if (courseCSC391 != null && tasmia != null && fahim != null && profRahim != null)
                {
                    videos.AddRange(new[]
                    {
                        // Topic: Sorting Algorithms
                        new CourseVideo { CourseId = courseCSC391.Id, Topic = "Sorting Algorithms", Title = "QuickSort Algorithm & Partitioning Logic (Abdul Bari)", VideoUrl = "https://www.youtube.com/watch?v=7h1s2SojIRw", SubmittedByUserId = tasmia.Id, UpVotes = 45, SubmittedAt = DateTime.UtcNow.AddDays(-15) },
                        new CourseVideo { CourseId = courseCSC391.Id, Topic = "Sorting Algorithms", Title = "MergeSort Algorithm & Divide and Conquer (FreeCodeCamp)", VideoUrl = "https://www.youtube.com/watch?v=4VqmGXwpLqc", SubmittedByUserId = fahim.Id, UpVotes = 28, SubmittedAt = DateTime.UtcNow.AddDays(-12) },
                        new CourseVideo { CourseId = courseCSC391.Id, Topic = "Sorting Algorithms", Title = "HeapSort Animation & Code Walkthrough (Bro Code)", VideoUrl = "https://www.youtube.com/watch?v=t0Cq6tVNRBA", SubmittedByUserId = tasmia.Id, UpVotes = 14, SubmittedAt = DateTime.UtcNow.AddDays(-6) },

                        // Topic: Trees & Graphs
                        new CourseVideo { CourseId = courseCSC391.Id, Topic = "Trees & Graphs", Title = "AVL Tree Rotations Made Easy (Gate Smashers)", VideoUrl = "https://www.youtube.com/watch?v=jDM6_TnYIqE", SubmittedByUserId = profRahim.Id, UpVotes = 52, SubmittedAt = DateTime.UtcNow.AddDays(-20) },
                        new CourseVideo { CourseId = courseCSC391.Id, Topic = "Trees & Graphs", Title = "Graph Traversal BFS & DFS Explained (Neso Academy)", VideoUrl = "https://www.youtube.com/watch?v=pcKY4hjDrxk", SubmittedByUserId = tasmia.Id, UpVotes = 34, SubmittedAt = DateTime.UtcNow.AddDays(-10) },

                        // Topic: Recursion
                        new CourseVideo { CourseId = courseCSC391.Id, Topic = "Recursion", Title = "Recursion Call Stack & Base Case Masterclass (Abdul Bari)", VideoUrl = "https://www.youtube.com/watch?v=mEBEw_xScsE", SubmittedByUserId = fahim.Id, UpVotes = 39, SubmittedAt = DateTime.UtcNow.AddDays(-8) }
                    });
                }

                if (courseCSC433 != null && tanvir != null && profRahim != null && tasmia != null)
                {
                    videos.AddRange(new[]
                    {
                        // Topic: SQL Fundamentals
                        new CourseVideo { CourseId = courseCSC433.Id, Topic = "SQL Fundamentals", Title = "SQL Joins Explained Visually — Inner, Left, Right, Outer", VideoUrl = "https://www.youtube.com/watch?v=9yeOJ0ZMUYw", SubmittedByUserId = tanvir.Id, UpVotes = 41, SubmittedAt = DateTime.UtcNow.AddDays(-14) },
                        new CourseVideo { CourseId = courseCSC433.Id, Topic = "SQL Fundamentals", Title = "SQL Group By and Having Clause Breakdown (Gate Smashers)", VideoUrl = "https://www.youtube.com/watch?v=7RzNEBpvFV0", SubmittedByUserId = tasmia.Id, UpVotes = 22, SubmittedAt = DateTime.UtcNow.AddDays(-9) },

                        // Topic: Normalization
                        new CourseVideo { CourseId = courseCSC433.Id, Topic = "Normalization", Title = "Database Normalization — 1NF, 2NF, 3NF, BCNF (Gate Smashers)", VideoUrl = "https://www.youtube.com/watch?v=xoTyrdT9SZI", SubmittedByUserId = profRahim.Id, UpVotes = 68, SubmittedAt = DateTime.UtcNow.AddDays(-22) },
                        new CourseVideo { CourseId = courseCSC433.Id, Topic = "Normalization", Title = "BCNF Functional Dependency Examples Step-by-Step", VideoUrl = "https://www.youtube.com/watch?v=ABwD8IYByfk", SubmittedByUserId = tanvir.Id, UpVotes = 19, SubmittedAt = DateTime.UtcNow.AddDays(-5) }
                    });
                }

                if (courseCSC247 != null && fahim != null && profRahim != null)
                {
                    videos.AddRange(new[]
                    {
                        // Topic: Digital Logic
                        new CourseVideo { CourseId = courseCSC247.Id, Topic = "Digital Logic", Title = "K-Map Simplification 3 & 4 Variables (Neso Academy)", VideoUrl = "https://www.youtube.com/watch?v=RO5alU6Zybw", SubmittedByUserId = fahim.Id, UpVotes = 33, SubmittedAt = DateTime.UtcNow.AddDays(-11) },
                        // Topic: Memory Hierarchy
                        new CourseVideo { CourseId = courseCSC247.Id, Topic = "Memory Hierarchy", Title = "Cache Memory Mapping Techniques — Direct, Associative", VideoUrl = "https://www.youtube.com/watch?v=6JozX68T_Zg", SubmittedByUserId = profRahim.Id, UpVotes = 27, SubmittedAt = DateTime.UtcNow.AddDays(-7) }
                    });
                }

                context.CourseVideos.AddRange(videos);
                context.SaveChanges();
            }

            // ── 6. Posts ────────────────────────────────────────────────────────────────
            if (!context.Posts.Any() && courseCSC391 != null && courseCSC433 != null && profRahim != null && tasmia != null)
            {
                var post1 = new Post
                {
                    CourseId = courseCSC391.Id, UserId = profRahim.Id,
                    Title = "CSC 391 Midterm Exam Announcement & Practice Problems",
                    Body = "Dear students,\n\nThe Midterm Examination for CSC 391 (Data Structures and Algorithms) will cover Binary Search Trees, AVL Tree rotations, and Graph BFS/DFS algorithm analysis. Please make sure to attempt the practice problems uploaded in the course resources section.\n\nOffice hours are held every Sunday and Tuesday from 2:00 PM to 4:00 PM.",
                    Flair = PostFlair.Announcement, UpVotes = 42, DownVotes = 1, IsPinned = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                };
                var post2 = new Post
                {
                    CourseId = courseCSC391.Id, UserId = tasmia.Id,
                    Title = "QuickSort vs MergeSort — Time Complexity & Space Tradeoffs Explained",
                    Body = "Hey everyone! A lot of students asked about why MergeSort requires O(N) auxiliary space while QuickSort is in-place O(1). Here is a quick breakdown:\n\n1. **MergeSort**: Guarantees O(N log N) worst-case, but requires extra space to merge sub-arrays.\n2. **QuickSort**: Average O(N log N), but worst-case can degrade to O(N²) with bad pivot selection.\n\nHope this helps for the upcoming quiz!",
                    Flair = PostFlair.Notes, UpVotes = 29, DownVotes = 0, IsPinned = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                };
                var post3 = new Post
                {
                    CourseId = courseCSC433.Id, UserId = tanvir?.Id ?? tasmia.Id,
                    Title = "How to resolve 3NF vs BCNF normalization questions in DBMS?",
                    Body = "I am practicing SQL normalization questions for CSC 433 assignment 2. Can someone explain a simple trick to identify if a relation is in 3NF but fails BCNF? Specifically when there are overlapping candidate keys.",
                    Flair = PostFlair.Question, UpVotes = 18, DownVotes = 2, IsPinned = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                };
                var post4 = new Post
                {
                    CourseId = courseCSC433.Id, UserId = fahim?.Id ?? tasmia.Id,
                    Title = "When the SQL query runs fine in your head vs when MySQL executes it 😅",
                    Body = "Spent 3 hours debugging a syntax error only to realize I wrote WHERE after GROUP BY instead of HAVING... Stay hydrated folks.",
                    Flair = PostFlair.Meme, UpVotes = 65, DownVotes = 3, IsPinned = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-12)
                };

                context.Posts.AddRange(post1, post2, post3, post4);
                context.SaveChanges();

                // Comments on post3
                var c1 = new Comment { PostId = post3.Id, UserId = profRahim.Id, Body = "Great question! Remember: in BCNF every functional dependency X→Y requires X to be a superkey. In 3NF, Y can also be a prime attribute — that's the key difference.", UpVotes = 14, CreatedAt = DateTime.UtcNow.AddDays(-1).AddHours(-5) };
                context.Comments.Add(c1);
                context.SaveChanges();

                context.Comments.AddRange(
                    new Comment { PostId = post3.Id, ParentCommentId = c1.Id, UserId = tanvir?.Id ?? tasmia.Id, Body = "Thank you Professor! So BCNF is strictly a superset of requirements — every BCNF relation is in 3NF but not vice versa.", UpVotes = 8, CreatedAt = DateTime.UtcNow.AddDays(-1).AddHours(-3) },
                    new Comment { PostId = post3.Id, ParentCommentId = c1.Id, UserId = fahim?.Id ?? tasmia.Id,  Body = "This concept always comes up in the final exam! Bookmarking this thread.", UpVotes = 5, CreatedAt = DateTime.UtcNow.AddDays(-1).AddHours(-1) },
                    new Comment { PostId = post2.Id, UserId = fahim?.Id ?? profRahim.Id, Body = "Awesome writeup Tasmia! Randomized pivot selection in QuickSort reduces the chance of hitting O(N²) worst case in practice.", UpVotes = 10, CreatedAt = DateTime.UtcNow.AddDays(-2) }
                );
                context.SaveChanges();
            }

            // ── 7. CourseFollows (seed test follows) ────────────────────────────────────
            if (!context.CourseFollows.Any() && courseCSC391 != null && courseCSC433 != null)
            {
                var follows = new List<CourseFollow>();
                if (tasmia  != null) follows.Add(new CourseFollow { UserId = tasmia.Id,  CourseId = courseCSC391.Id, FollowedAt = DateTime.UtcNow.AddDays(-15) });
                if (tanvir  != null) follows.Add(new CourseFollow { UserId = tanvir.Id,  CourseId = courseCSC433.Id, FollowedAt = DateTime.UtcNow.AddDays(-10) });
                if (fahim   != null) follows.Add(new CourseFollow { UserId = fahim.Id,   CourseId = courseCSC391.Id, FollowedAt = DateTime.UtcNow.AddDays(-8)  });
                if (fahim   != null) follows.Add(new CourseFollow { UserId = fahim.Id,   CourseId = courseCSC433.Id, FollowedAt = DateTime.UtcNow.AddDays(-5)  });
                context.CourseFollows.AddRange(follows);
                context.SaveChanges();
            }
        }
    }
}
