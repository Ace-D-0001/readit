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

            Console.WriteLine(">>> [DbSeeder] Ensuring database created and seeding...");
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
            // Required Test Admin (admin@gmail.com / 1234567)
            var adminUser = userManager.FindByEmailAsync("admin@gmail.com").Result;
            if (adminUser == null)
            {
                adminUser = new ApplicationUser { UserName = "admin", Email = "admin@gmail.com", Bio = "System Administrator & Moderation Lead — StudyHub", EmailConfirmed = true };
                var res = userManager.CreateAsync(adminUser, "1234567").Result;
                if (!res.Succeeded)
                {
                    // If username 'admin' taken by legacy admin@iubat.edu, use email as username or update
                    adminUser.UserName = "admin_portal";
                    userManager.CreateAsync(adminUser, "1234567").Wait();
                }
                userManager.AddToRoleAsync(adminUser, "Admin").Wait();
            }
            else
            {
                // Ensure password is reset to 1234567
                var token = userManager.GeneratePasswordResetTokenAsync(adminUser).Result;
                userManager.ResetPasswordAsync(adminUser, token, "1234567").Wait();
                userManager.AddToRoleAsync(adminUser, "Admin").Wait();
            }

            // Legacy admin support
            var legacyAdmin = userManager.FindByEmailAsync("admin@iubat.edu").Result;
            if (legacyAdmin != null)
            {
                var token = userManager.GeneratePasswordResetTokenAsync(legacyAdmin).Result;
                userManager.ResetPasswordAsync(legacyAdmin, token, "1234567").Wait();
            }

            // Required Test Student (student@gmail.com / 1234567)
            var studentUser = userManager.FindByEmailAsync("student@gmail.com").Result;
            if (studentUser == null)
            {
                studentUser = new ApplicationUser { UserName = "student", Email = "student@gmail.com", Bio = "Computer Science Student — StudyHub", EmailConfirmed = true };
                var res = userManager.CreateAsync(studentUser, "1234567").Result;
                if (!res.Succeeded)
                {
                    studentUser.UserName = "student_user";
                    userManager.CreateAsync(studentUser, "1234567").Wait();
                }
                userManager.AddToRoleAsync(studentUser, "Student").Wait();
            }
            else
            {
                // Ensure password is reset to 1234567
                var token = userManager.GeneratePasswordResetTokenAsync(studentUser).Result;
                userManager.ResetPasswordAsync(studentUser, token, "1234567").Wait();
                userManager.AddToRoleAsync(studentUser, "Student").Wait();
            }

            // Legacy student support
            var legacyStudent = userManager.FindByEmailAsync("student@iubat.edu").Result;
            if (legacyStudent != null)
            {
                var token = userManager.GeneratePasswordResetTokenAsync(legacyStudent).Result;
                userManager.ResetPasswordAsync(legacyStudent, token, "1234567").Wait();
            }

            // Additional Demo Academic Users
            var demoUsers = new[]
            {
                new { Username = "prof_rahim", Email = "prof_rahim@iubat.edu", Bio = "Assistant Professor, CSE Dept at IUBAT | Data Structures & DBMS", Role = "Student" },
                new { Username = "prof_karim", Email = "prof_karim@iubat.edu", Bio = "Associate Professor, Math & Sciences Dept at IUBAT", Role = "Student" },
                new { Username = "tasmia_cse",  Email = "tasmia@iubat.edu",     Bio = "3rd Year BCSE Student | Algorithm enthusiast & Peer Tutor", Role = "Student" },
                new { Username = "tanvir_iubat",Email = "tanvir@iubat.edu",     Bio = "Database & Web Systems learner | Open-source contributor", Role = "Student" },
                new { Username = "fahim_dev",   Email = "fahim@iubat.edu",      Bio = "Competitive programmer @ IUBAT ACM ICPC Team", Role = "Student" },
                new { Username = "nusrat_bba",  Email = "nusrat@iubat.edu",     Bio = "BBA Senior | Business Strategy & Financial Accounting", Role = "Student" },
                new { Username = "arif_tech",   Email = "arif@iubat.edu",       Bio = "Sophomore BCSE | Linux & Networks hobbyist", Role = "Student" },
                new { Username = "sadia_math",  Email = "sadia@iubat.edu",      Bio = "Math enthusiast | Numerical methods & Statistics", Role = "Student" }
            };

            foreach (var du in demoUsers)
            {
                if (userManager.FindByNameAsync(du.Username).Result == null)
                {
                    var u = new ApplicationUser { UserName = du.Username, Email = du.Email, Bio = du.Bio, EmailConfirmed = true };
                    userManager.CreateAsync(u, "Password123!").Wait();
                    userManager.AddToRoleAsync(u, du.Role).Wait();
                }
            }

            var profRahim = context.Users.FirstOrDefault(u => u.UserName == "prof_rahim");
            var profKarim = context.Users.FirstOrDefault(u => u.UserName == "prof_karim");
            var tasmia    = context.Users.FirstOrDefault(u => u.UserName == "tasmia_cse") ?? studentUser;
            var tanvir    = context.Users.FirstOrDefault(u => u.UserName == "tanvir_iubat") ?? studentUser;
            var fahim     = context.Users.FirstOrDefault(u => u.UserName == "fahim_dev") ?? studentUser;
            var nusrat    = context.Users.FirstOrDefault(u => u.UserName == "nusrat_bba") ?? studentUser;
            var arif      = context.Users.FirstOrDefault(u => u.UserName == "arif_tech") ?? studentUser;
            var sadia     = context.Users.FirstOrDefault(u => u.UserName == "sadia_math") ?? studentUser;

            // ── 2. Comprehensive Course Catalog ─────────────────────────────────────────
            var courseDefinitions = new[]
            {
                new { Code = "CSC147", Title = "Structured Programming", Desc = "Fundamental programming concepts in C, control flow, functions, arrays, pointers, and memory management." },
                new { Code = "CSC247", Title = "Computer Organization & Architecture", Desc = "Study of computer architecture, digital logic gates, CPU microarchitecture, and memory hierarchy." },
                new { Code = "CSC283", Title = "Object Oriented Programming", Desc = "Principles of OOP using Java and C++, classes, inheritance, polymorphism, encapsulation, and design patterns." },
                new { Code = "CSC391", Title = "Data Structure and Algorithm", Desc = "In-depth study of arrays, trees, graphs, sorting, searching, dynamic programming, and algorithmic complexity." },
                new { Code = "CSC433", Title = "Database Management Systems", Desc = "Relational database concepts, ER diagrams, SQL queries, normalization (1NF-BCNF), and transaction processing." },
                new { Code = "CSC441", Title = "Operating Systems", Desc = "Process synchronization, multi-threading, CPU scheduling algorithms, virtual memory, paging, and deadlocks." },
                new { Code = "CSC471", Title = "Computer Networks", Desc = "Network architectures, OSI and TCP/IP stack, IP subnetting, routing protocols, and socket programming." },
                new { Code = "MAT101", Title = "Basic Mathematics", Desc = "Core algebra, trigonometry, matrices, quadratic equations, and mathematical logic foundation." },
                new { Code = "MAT147", Title = "Calculus and Analytical Geometry", Desc = "Differential and integral calculus, limits, continuity, rate of change, curve sketching, and 3D geometry." },
                new { Code = "MAT247", Title = "Numerical Analysis", Desc = "Numerical methods for algebraic equations, Newton-Raphson, interpolation, numerical integration, and differential equations." },
                new { Code = "STA240", Title = "Statistics and Probability", Desc = "Descriptive statistics, probability distributions, hypothesis testing, regression analysis, and variance analysis." },
                new { Code = "ENG101", Title = "English Language I", Desc = "Foundation English communication, academic reading comprehension, formal writing, and vocabulary development." },
                new { Code = "ENG102", Title = "English Language II", Desc = "Advanced academic writing, technical research reports, formal presentations, and critical discourse." },
                new { Code = "MGT101", Title = "Principles of Management", Desc = "Introduction to management functions: planning, organizing, leading, controlling, and organizational behavior." }
            };

            foreach (var cDef in courseDefinitions)
            {
                var existing = context.Courses.FirstOrDefault(c => c.Code == cDef.Code);
                if (existing == null)
                {
                    context.Courses.Add(new Course
                    {
                        Code = cDef.Code,
                        Title = cDef.Title,
                        Description = cDef.Desc,
                        CreatedAt = DateTime.UtcNow.AddDays(-60)
                    });
                }
            }
            context.SaveChanges();

            // Load courses dictionary for easy lookup
            var courses = context.Courses.ToDictionary(c => c.Code, c => c);

            // ── 3. Course Outlines & Resources ──────────────────────────────────────────
            var resourceSeeds = new[]
            {
                // CSC147
                new { Code = "CSC147", Type = CourseResourceType.Outline, Title = "CSC 147 Official Syllabus & Lab Manual (PDF)", Url = "https://iubat.edu/programs/bcse/courses/csc147", ExamCat = "General", Status = ResourceStatus.Approved, User = profRahim?.Id },
                new { Code = "CSC147", Type = CourseResourceType.Notes,   Title = "C Programming Complete Syntax & Pointer Cheat Sheet", Url = "https://drive.google.com/drive/folders/1CSC147_Pointers_Guide", ExamCat = "Lecture Notes", Status = ResourceStatus.Approved, User = fahim?.Id },
                new { Code = "CSC147", Type = CourseResourceType.Notes,   Title = "CSC 147 Midterm Solved Question Bank (2024-2025)", Url = "https://drive.google.com/drive/folders/1CSC147_Midterm_Solved", ExamCat = "Midterm", Status = ResourceStatus.Approved, User = tasmia?.Id },
                new { Code = "CSC147", Type = CourseResourceType.Notes,   Title = "CSC 147 Lab Practice Problems: Dynamic Memory & Structs", Url = "https://github.com/iubat-cse/csc147-lab-exercises", ExamCat = "Lab / Assignment", Status = ResourceStatus.Approved, User = arif?.Id },
                new { Code = "CSC147", Type = CourseResourceType.Notes,   Title = "CSC 147 Student Handwritten Final Exam Revision Summary", Url = "https://drive.google.com/drive/folders/1CSC147_Student_Final", ExamCat = "Final Exam", Status = ResourceStatus.Pending, User = studentUser?.Id },

                // CSC247
                new { Code = "CSC247", Type = CourseResourceType.Outline, Title = "CSC 247 Course Plan, Grading Criteria & Lab Schedule", Url = "https://github.com/iubat-cse/csc247-architecture-outline", ExamCat = "General", Status = ResourceStatus.Approved, User = profRahim?.Id },
                new { Code = "CSC247", Type = CourseResourceType.Notes,   Title = "Logic Gates, Karnaugh Maps & Boolean Algebra Solved Notes", Url = "https://drive.google.com/drive/folders/1CSC247_Arch_Exams", ExamCat = "Midterm", Status = ResourceStatus.Approved, User = fahim?.Id },
                new { Code = "CSC247", Type = CourseResourceType.Notes,   Title = "Memory Hierarchy, Cache Mapping & Pipelining Complete Guide", Url = "https://drive.google.com/drive/folders/1CSC247_Cache_Notes", ExamCat = "Final Exam", Status = ResourceStatus.Approved, User = tasmia?.Id },
                new { Code = "CSC247", Type = CourseResourceType.Notes,   Title = "Logisim Circuit Simulation Lab Report Submissions", Url = "https://drive.google.com/drive/folders/1CSC247_Lab_Logisim", ExamCat = "Lab / Assignment", Status = ResourceStatus.Pending, User = arif?.Id },

                // CSC283
                new { Code = "CSC283", Type = CourseResourceType.Outline, Title = "CSC 283 Java & OOP Course Outline & Weekly Milestones", Url = "https://iubat.edu/programs/bcse/courses/csc283", ExamCat = "General", Status = ResourceStatus.Approved, User = profRahim?.Id },
                new { Code = "CSC283", Type = CourseResourceType.Notes,   Title = "Polymorphism, Abstract Classes & Interfaces in Java with Code", Url = "https://github.com/iubat-cse/csc283-oop-examples", ExamCat = "Lecture Notes", Status = ResourceStatus.Approved, User = tanvir?.Id },
                new { Code = "CSC283", Type = CourseResourceType.Notes,   Title = "CSC 283 Midterm Solved Papers & Java Exception Handling", Url = "https://drive.google.com/drive/folders/1CSC283_Midterm_QBank", ExamCat = "Midterm", Status = ResourceStatus.Approved, User = fahim?.Id },
                new { Code = "CSC283", Type = CourseResourceType.Notes,   Title = "OOP Term Project Architecture: Swing / JavaFX Desktop App", Url = "https://github.com/iubat-cse/oop-project-template", ExamCat = "Lab / Assignment", Status = ResourceStatus.Approved, User = tasmia?.Id },

                // CSC391
                new { Code = "CSC391", Type = CourseResourceType.Outline, Title = "CSC 391 Official Syllabus & Grading Criteria (Google Docs)", Url = "https://docs.google.com/document/d/1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms/edit", ExamCat = "General", Status = ResourceStatus.Approved, User = profRahim?.Id },
                new { Code = "CSC391", Type = CourseResourceType.Notes,   Title = "CSC 391 Solved Exam Papers & Notes Collection (Google Drive)", Url = "https://drive.google.com/drive/folders/1QBank_CSC391_IUBAT", ExamCat = "Midterm", Status = ResourceStatus.Approved, User = fahim?.Id },
                new { Code = "CSC391", Type = CourseResourceType.Notes,   Title = "Data Structures Interactive Visualization Guide & Trees", Url = "https://visualgo.net/en", ExamCat = "Lecture Notes", Status = ResourceStatus.Approved, User = tasmia?.Id },
                new { Code = "CSC391", Type = CourseResourceType.Notes,   Title = "Graph BFS, DFS, Dijkstra & Bellman-Ford Implementation Notes", Url = "https://drive.google.com/drive/folders/1CSC391_Graphs_Solved", ExamCat = "Final Exam", Status = ResourceStatus.Approved, User = studentUser?.Id },
                new { Code = "CSC391", Type = CourseResourceType.Notes,   Title = "CSC 391 Student Note: Dynamic Programming 0/1 Knapsack Walkthrough", Url = "https://drive.google.com/drive/folders/1CSC391_DP_Knapsack", ExamCat = "Final Exam", Status = ResourceStatus.Pending, User = tanvir?.Id },

                // CSC433
                new { Code = "CSC433", Type = CourseResourceType.Outline, Title = "CSC 433 Course Syllabus & Weekly Topics (Notion Hub)", Url = "https://iubat-cse.notion.site/CSC433-DBMS-Syllabus-2026", ExamCat = "General", Status = ResourceStatus.Approved, User = profRahim?.Id },
                new { Code = "CSC433", Type = CourseResourceType.Notes,   Title = "DBMS Midterm & Final Study Notes Collection (Google Drive)", Url = "https://drive.google.com/drive/folders/1DBMS_QBank_Collection", ExamCat = "Midterm", Status = ResourceStatus.Approved, User = tanvir?.Id },
                new { Code = "CSC433", Type = CourseResourceType.Notes,   Title = "SQL Syntax Reference, Subqueries, Joins & Exercises", Url = "https://www.w3schools.com/sql/", ExamCat = "Lecture Notes", Status = ResourceStatus.Approved, User = tasmia?.Id },
                new { Code = "CSC433", Type = CourseResourceType.Notes,   Title = "Normalization Step-by-Step Solved Proofs (1NF to BCNF)", Url = "https://drive.google.com/drive/folders/1CSC433_Normalization_Proofs", ExamCat = "Final Exam", Status = ResourceStatus.Approved, User = fahim?.Id },
                new { Code = "CSC433", Type = CourseResourceType.Notes,   Title = "CSC 433 Lab: PostgreSQL Triggers & Stored Procedures Handout", Url = "https://github.com/iubat-cse/dbms-lab-triggers", ExamCat = "Lab / Assignment", Status = ResourceStatus.Pending, User = studentUser?.Id },

                // CSC441
                new { Code = "CSC441", Type = CourseResourceType.Outline, Title = "CSC 441 Operating Systems Syllabus & CPU Scheduling Handouts", Url = "https://iubat.edu/programs/bcse/courses/csc441", ExamCat = "General", Status = ResourceStatus.Approved, User = profRahim?.Id },
                new { Code = "CSC441", Type = CourseResourceType.Notes,   Title = "Process Synchronization: Semaphores & Dining Philosophers", Url = "https://drive.google.com/drive/folders/1CSC441_Semaphores_Notes", ExamCat = "Lecture Notes", Status = ResourceStatus.Approved, User = arif?.Id },
                new { Code = "CSC441", Type = CourseResourceType.Notes,   Title = "CSC 441 Midterm Solved Papers: Scheduling, Deadlock Banker's Alg", Url = "https://drive.google.com/drive/folders/1CSC441_Midterm_Banker", ExamCat = "Midterm", Status = ResourceStatus.Approved, User = fahim?.Id },
                new { Code = "CSC441", Type = CourseResourceType.Notes,   Title = "Virtual Memory & Page Replacement Algorithms (FIFO, LRU, Optimal)", Url = "https://drive.google.com/drive/folders/1CSC441_Virtual_Memory", ExamCat = "Final Exam", Status = ResourceStatus.Approved, User = tasmia?.Id },

                // CSC471
                new { Code = "CSC471", Type = CourseResourceType.Outline, Title = "CSC 471 Computer Networks Course Schedule & Packet Tracer Labs", Url = "https://iubat.edu/programs/bcse/courses/csc471", ExamCat = "General", Status = ResourceStatus.Approved, User = profRahim?.Id },
                new { Code = "CSC471", Type = CourseResourceType.Notes,   Title = "IP Subnetting Cheat Sheet (IPv4 Classless CIDR & VLSM)", Url = "https://drive.google.com/drive/folders/1CSC471_Subnetting_Guide", ExamCat = "Midterm", Status = ResourceStatus.Approved, User = arif?.Id },
                new { Code = "CSC471", Type = CourseResourceType.Notes,   Title = "Transport Layer Protocols: TCP 3-Way Handshake vs UDP", Url = "https://drive.google.com/drive/folders/1CSC471_TCP_UDP_Summary", ExamCat = "Final Exam", Status = ResourceStatus.Approved, User = fahim?.Id },
                new { Code = "CSC471", Type = CourseResourceType.Notes,   Title = "Cisco Packet Tracer Lab 4: OSPF & RIP Routing Configuration", Url = "https://github.com/iubat-cse/packet-tracer-labs", ExamCat = "Lab / Assignment", Status = ResourceStatus.Pending, User = studentUser?.Id },

                // MAT101
                new { Code = "MAT101", Type = CourseResourceType.Outline, Title = "MAT 101 Basic Mathematics Syllabus & Exam Structure", Url = "https://iubat.edu/programs/math/courses/mat101", ExamCat = "General", Status = ResourceStatus.Approved, User = profKarim?.Id },
                new { Code = "MAT101", Type = CourseResourceType.Notes,   Title = "MAT 101 Algebra & Trigonometric Identities Solved Sheets", Url = "https://drive.google.com/drive/folders/1MAT101_Algebra_Notes", ExamCat = "Midterm", Status = ResourceStatus.Approved, User = sadia?.Id },
                new { Code = "MAT101", Type = CourseResourceType.Notes,   Title = "Matrices, Determinants & Cramer's Rule Step-by-Step", Url = "https://drive.google.com/drive/folders/1MAT101_Matrices_Guide", ExamCat = "Final Exam", Status = ResourceStatus.Approved, User = nusrat?.Id },

                // MAT147
                new { Code = "MAT147", Type = CourseResourceType.Outline, Title = "MAT 147 Calculus & Analytic Geometry Course Syllabus", Url = "https://iubat.edu/programs/math/courses/mat147", ExamCat = "General", Status = ResourceStatus.Approved, User = profKarim?.Id },
                new { Code = "MAT147", Type = CourseResourceType.Notes,   Title = "Calculus Differentiation Formulas & Chain Rule Solved Examples", Url = "https://drive.google.com/drive/folders/1MAT147_Diff_Notes", ExamCat = "Midterm", Status = ResourceStatus.Approved, User = sadia?.Id },
                new { Code = "MAT147", Type = CourseResourceType.Notes,   Title = "Definite & Indefinite Integration Techniques Summary", Url = "https://drive.google.com/drive/folders/1MAT147_Integration_CheatSheet", ExamCat = "Final Exam", Status = ResourceStatus.Approved, User = fahim?.Id },
                new { Code = "MAT147", Type = CourseResourceType.Notes,   Title = "Analytical Geometry: Conic Sections (Parabola, Ellipse, Hyperbola)", Url = "https://drive.google.com/drive/folders/1MAT147_Conics", ExamCat = "Lecture Notes", Status = ResourceStatus.Pending, User = studentUser?.Id },

                // MAT247
                new { Code = "MAT247", Type = CourseResourceType.Outline, Title = "MAT 247 Numerical Methods Course Topics Webpage", Url = "https://iubat.edu/programs/math/courses/mat247", ExamCat = "General", Status = ResourceStatus.Approved, User = profKarim?.Id },
                new { Code = "MAT247", Type = CourseResourceType.Notes,   Title = "Numerical Analysis Solved Study Notes & Newton Raphson Code", Url = "https://drive.google.com/drive/folders/1MAT247_Numerical_QBank", ExamCat = "Midterm", Status = ResourceStatus.Approved, User = tasmia?.Id },
                new { Code = "MAT247", Type = CourseResourceType.Notes,   Title = "Trapezoidal & Simpson's 1/3 and 3/8 Rule Solved Problems", Url = "https://drive.google.com/drive/folders/1MAT247_Integration_Solved", ExamCat = "Final Exam", Status = ResourceStatus.Approved, User = sadia?.Id },

                // STA240
                new { Code = "STA240", Type = CourseResourceType.Outline, Title = "STA 240 Statistics & Probability Official Course Plan", Url = "https://iubat.edu/programs/math/courses/sta240", ExamCat = "General", Status = ResourceStatus.Approved, User = profKarim?.Id },
                new { Code = "STA240", Type = CourseResourceType.Notes,   Title = "Probability Distributions: Binomial, Poisson & Normal Distribution", Url = "https://drive.google.com/drive/folders/1STA240_Distributions_Notes", ExamCat = "Midterm", Status = ResourceStatus.Approved, User = nusrat?.Id },
                new { Code = "STA240", Type = CourseResourceType.Notes,   Title = "Hypothesis Testing, Z-Test, T-Test & Chi-Square Solved Cases", Url = "https://drive.google.com/drive/folders/1STA240_Hypothesis_Tests", ExamCat = "Final Exam", Status = ResourceStatus.Approved, User = sadia?.Id },

                // ENG101
                new { Code = "ENG101", Type = CourseResourceType.Outline, Title = "ENG 101 Course Structure, Rubrics & Essay Guidelines", Url = "https://docs.google.com/document/d/1ENG101_IUBAT_Syllabus", ExamCat = "General", Status = ResourceStatus.Approved, User = nusrat?.Id },
                new { Code = "ENG101", Type = CourseResourceType.Notes,   Title = "Grammar Mastery: Subject-Verb Agreement & Sentence Correction", Url = "https://drive.google.com/drive/folders/1ENG101_Grammar_Mastery", ExamCat = "Lecture Notes", Status = ResourceStatus.Approved, User = studentUser?.Id },
                new { Code = "ENG101", Type = CourseResourceType.Notes,   Title = "Formal Email & Academic Essay Writing Models", Url = "https://drive.google.com/drive/folders/1ENG101_Essays_Solved", ExamCat = "Midterm", Status = ResourceStatus.Approved, User = nusrat?.Id },

                // ENG102
                new { Code = "ENG102", Type = CourseResourceType.Outline, Title = "ENG 102 Technical Report Writing & Term Paper Guidelines", Url = "https://iubat.edu/programs/arts/courses/eng102", ExamCat = "General", Status = ResourceStatus.Approved, User = nusrat?.Id },
                new { Code = "ENG102", Type = CourseResourceType.Notes,   Title = "IEEE Referencing & Citation Format Guide for BCSE Students", Url = "https://drive.google.com/drive/folders/1ENG102_IEEE_Referencing", ExamCat = "Lecture Notes", Status = ResourceStatus.Approved, User = tasmia?.Id },
                new { Code = "ENG102", Type = CourseResourceType.Notes,   Title = "Final Research Proposal Model Paper (Sample A+ Grade)", Url = "https://drive.google.com/drive/folders/1ENG102_Sample_Proposal", ExamCat = "Final Exam", Status = ResourceStatus.Approved, User = nusrat?.Id },

                // MGT101
                new { Code = "MGT101", Type = CourseResourceType.Outline, Title = "MGT 101 Principles of Management Course Outline & Case Studies", Url = "https://iubat.edu/programs/cba/courses/mgt101", ExamCat = "General", Status = ResourceStatus.Approved, User = nusrat?.Id },
                new { Code = "MGT101", Type = CourseResourceType.Notes,   Title = "Fayol's 14 Principles of Management & Maslow's Hierarchy Solved", Url = "https://drive.google.com/drive/folders/1MGT101_Principles_Summary", ExamCat = "Midterm", Status = ResourceStatus.Approved, User = nusrat?.Id },
                new { Code = "MGT101", Type = CourseResourceType.Notes,   Title = "SWOT Analysis & Strategic Management Real-World Examples", Url = "https://drive.google.com/drive/folders/1MGT101_SWOT_Analysis", ExamCat = "Final Exam", Status = ResourceStatus.Approved, User = studentUser?.Id }
            };

            foreach (var r in resourceSeeds)
            {
                if (courses.ContainsKey(r.Code))
                {
                    int cid = courses[r.Code].Id;
                    if (!context.CourseResources.Any(res => res.CourseId == cid && res.Title == r.Title))
                    {
                        context.CourseResources.Add(new CourseResource
                        {
                            CourseId = cid,
                            Type = r.Type,
                            Title = r.Title,
                            Url = r.Url,
                            ExamCategory = r.ExamCat,
                            Status = r.Status,
                            UploadedByUserId = r.User,
                            CreatedAt = DateTime.UtcNow.AddDays(-new Random().Next(3, 25))
                        });
                    }
                }
            }
            context.SaveChanges();

            // ── 4. Curated YouTube Videos by Topic ───────────────────────────────────────
            var videoSeeds = new[]
            {
                // CSC147
                new { Code = "CSC147", Topic = "Pointers in C", Title = "Pointers in C / C++ Explained (FreeCodeCamp / mycodeschool)", Url = "https://www.youtube.com/watch?v=zuegQmMdy8M", Upvotes = 42, User = fahim?.Id },
                new { Code = "CSC147", Topic = "Pointers in C", Title = "Dynamic Memory Allocation in C — malloc, calloc, realloc, free", Url = "https://www.youtube.com/watch?v=udgHqM9ZJvA", Upvotes = 31, User = arif?.Id },
                new { Code = "CSC147", Topic = "Functions & Recursion", Title = "C Programming Tutorial — Functions and Header Files", Url = "https://www.youtube.com/watch?v=KJgsSFOSQv0", Upvotes = 25, User = tasmia?.Id },

                // CSC247
                new { Code = "CSC247", Topic = "Digital Logic", Title = "K-Map Simplification 3 & 4 Variables (Neso Academy)", Url = "https://www.youtube.com/watch?v=RO5alU6Zybw", Upvotes = 38, User = fahim?.Id },
                new { Code = "CSC247", Topic = "Memory Hierarchy", Title = "Cache Memory Mapping Techniques — Direct, Associative, Set-Associative", Url = "https://www.youtube.com/watch?v=6JozX68T_Zg", Upvotes = 29, User = profRahim?.Id },
                new { Code = "CSC247", Topic = "CPU Microarchitecture", Title = "Instruction Pipelining and Hazard Resolution (Gate Smashers)", Url = "https://www.youtube.com/watch?v=zJgQY5b5f8Y", Upvotes = 22, User = arif?.Id },

                // CSC283
                new { Code = "CSC283", Topic = "OOP Foundations", Title = "Java OOP Basics — Classes, Objects, Constructors (Bro Code)", Url = "https://www.youtube.com/watch?v=A74TOX803D0", Upvotes = 35, User = tanvir?.Id },
                new { Code = "CSC283", Topic = "Polymorphism & Abstraction", Title = "Abstract Classes vs Interfaces in Java (Telusko)", Url = "https://www.youtube.com/watch?v=9Jp44oBoUeg", Upvotes = 40, User = tasmia?.Id },
                new { Code = "CSC283", Topic = "Exception Handling", Title = "Java Exception Handling Try-Catch-Finally Masterclass", Url = "https://www.youtube.com/watch?v=1XAfapkBQjk", Upvotes = 19, User = studentUser?.Id },

                // CSC391
                new { Code = "CSC391", Topic = "Sorting Algorithms", Title = "QuickSort Algorithm & Partitioning Logic (Abdul Bari)", Url = "https://www.youtube.com/watch?v=7h1s2SojIRw", Upvotes = 55, User = tasmia?.Id },
                new { Code = "CSC391", Topic = "Sorting Algorithms", Title = "MergeSort Algorithm & Divide and Conquer (FreeCodeCamp)", Url = "https://www.youtube.com/watch?v=4VqmGXwpLqc", Upvotes = 34, User = fahim?.Id },
                new { Code = "CSC391", Topic = "Trees & Graphs", Title = "AVL Tree Rotations Made Easy (Gate Smashers)", Url = "https://www.youtube.com/watch?v=jDM6_TnYIqE", Upvotes = 62, User = profRahim?.Id },
                new { Code = "CSC391", Topic = "Trees & Graphs", Title = "Graph Traversal BFS & DFS Explained (Neso Academy)", Url = "https://www.youtube.com/watch?v=pcKY4hjDrxk", Upvotes = 41, User = tasmia?.Id },
                new { Code = "CSC391", Topic = "Dynamic Programming", Title = "0/1 Knapsack Problem Dynamic Programming (Abdul Bari)", Url = "https://www.youtube.com/watch?v=nLmhmB6NzcM", Upvotes = 48, User = fahim?.Id },

                // CSC433
                new { Code = "CSC433", Topic = "SQL Fundamentals", Title = "SQL Joins Explained Visually — Inner, Left, Right, Outer", Url = "https://www.youtube.com/watch?v=9yeOJ0ZMUYw", Upvotes = 45, User = tanvir?.Id },
                new { Code = "CSC433", Topic = "SQL Fundamentals", Title = "SQL Group By and Having Clause Breakdown (Gate Smashers)", Url = "https://www.youtube.com/watch?v=7RzNEBpvFV0", Upvotes = 27, User = tasmia?.Id },
                new { Code = "CSC433", Topic = "Normalization", Title = "Database Normalization — 1NF, 2NF, 3NF, BCNF (Gate Smashers)", Url = "https://www.youtube.com/watch?v=xoTyrdT9SZI", Upvotes = 74, User = profRahim?.Id },
                new { Code = "CSC433", Topic = "Indexing & Transactions", Title = "B-Tree and B+ Tree Indexing in Relational Databases", Url = "https://www.youtube.com/watch?v=aZjYr87r1b8", Upvotes = 33, User = tanvir?.Id },

                // CSC441
                new { Code = "CSC441", Topic = "Process Scheduling", Title = "CPU Scheduling Algorithms — FCFS, SJF, Round Robin (Gate Smashers)", Url = "https://www.youtube.com/watch?v=ewabbnyw_eM", Upvotes = 43, User = fahim?.Id },
                new { Code = "CSC441", Topic = "Synchronization", Title = "Critical Section Problem & Peterson's Algorithm (Neso Academy)", Url = "https://www.youtube.com/watch?v=l_kYh29Mv3A", Upvotes = 30, User = arif?.Id },
                new { Code = "CSC441", Topic = "Deadlocks", Title = "Deadlock Prevention, Avoidance & Banker's Algorithm", Url = "https://www.youtube.com/watch?v=2T3pt_yv8bM", Upvotes = 37, User = profRahim?.Id },

                // CSC471
                new { Code = "CSC471", Topic = "Subnetting", Title = "IP Subnetting Made Easy for Beginners (NetworkChuck)", Url = "https://www.youtube.com/watch?v=5WfiTHiU4x8", Upvotes = 52, User = arif?.Id },
                new { Code = "CSC471", Topic = "OSI & TCP/IP Model", Title = "OSI Model 7 Layers Explained with Real Life Examples", Url = "https://www.youtube.com/watch?v=LANW3m7UgLN", Upvotes = 39, User = fahim?.Id },
                new { Code = "CSC471", Topic = "Routing", Title = "Routing Protocols Distance Vector vs Link State (OSPF vs BGP)", Url = "https://www.youtube.com/watch?v=zJgQY5b5f8Y", Upvotes = 28, User = tanvir?.Id },

                // MAT101
                new { Code = "MAT101", Topic = "Algebra & Matrices", Title = "Matrix Multiplication and Determinants (Khan Academy)", Url = "https://www.youtube.com/watch?v=2spnLzH_jYY", Upvotes = 26, User = sadia?.Id },
                new { Code = "MAT101", Topic = "Trigonometry", Title = "Trigonometric Unit Circle & Angle Identities Explained", Url = "https://www.youtube.com/watch?v=1-hrT1Ys39o", Upvotes = 21, User = profKarim?.Id },

                // MAT147
                new { Code = "MAT147", Topic = "Differentiation", Title = "Essence of Calculus — The Derivative (3Blue1Brown)", Url = "https://www.youtube.com/watch?v=9vKqVkMQHKk", Upvotes = 85, User = sadia?.Id },
                new { Code = "MAT147", Topic = "Integration", Title = "Integration by Substitution and By Parts (Khan Academy)", Url = "https://www.youtube.com/watch?v=rfG8ce4nNh0", Upvotes = 38, User = fahim?.Id },

                // MAT247
                new { Code = "MAT247", Topic = "Root Finding", Title = "Newton Raphson Method Step by Step (Neso Academy)", Url = "https://www.youtube.com/watch?v=E_y3QvMv2h0", Upvotes = 33, User = sadia?.Id },
                new { Code = "MAT247", Topic = "Numerical Integration", Title = "Simpson's 1/3 Rule Formula and Calculation (Gate Smashers)", Url = "https://www.youtube.com/watch?v=QZ8oI4zXyH4", Upvotes = 25, User = profKarim?.Id },

                // STA240
                new { Code = "STA240", Topic = "Distributions", Title = "Normal Distribution and Empirical Rule (StatQuest with Josh Starmer)", Url = "https://www.youtube.com/watch?v=rzFX5NWojp0", Upvotes = 44, User = sadia?.Id },
                new { Code = "STA240", Topic = "Hypothesis Testing", Title = "Hypothesis Testing, P-Values, and Significance Level (StatQuest)", Url = "https://www.youtube.com/watch?v=vemZtEM63GY", Upvotes = 49, User = nusrat?.Id },

                // ENG101
                new { Code = "ENG101", Topic = "Academic Writing", Title = "How to Write an Effective Academic Essay (Harvard Extension)", Url = "https://www.youtube.com/watch?v=IY6V7GkS2Fk", Upvotes = 24, User = nusrat?.Id },

                // ENG102
                new { Code = "ENG102", Topic = "Technical Reports", Title = "Technical Report Writing Guide & Structure (Engineering Mindset)", Url = "https://www.youtube.com/watch?v=2A22nK8fQ2Q", Upvotes = 22, User = nusrat?.Id },

                // MGT101
                new { Code = "MGT101", Topic = "Management Principles", Title = "Fayol's 14 Principles of Management with Real Examples", Url = "https://www.youtube.com/watch?v=8qWcE5Yl8aY", Upvotes = 30, User = nusrat?.Id },
                new { Code = "MGT101", Topic = "Leadership & Motivation", Title = "Maslow's Hierarchy of Needs Applied to Management", Url = "https://www.youtube.com/watch?v=O-4ithG_07Q", Upvotes = 27, User = profRahim?.Id }
            };

            foreach (var v in videoSeeds)
            {
                if (courses.ContainsKey(v.Code))
                {
                    int cid = courses[v.Code].Id;
                    if (!context.CourseVideos.Any(vid => vid.CourseId == cid && vid.Title == v.Title))
                    {
                        context.CourseVideos.Add(new CourseVideo
                        {
                            CourseId = cid,
                            Topic = v.Topic,
                            Title = v.Title,
                            VideoUrl = v.Url,
                            UpVotes = v.Upvotes,
                            SubmittedByUserId = v.User ?? (studentUser?.Id ?? "student"),
                            SubmittedAt = DateTime.UtcNow.AddDays(-new Random().Next(4, 30))
                        });
                    }
                }
            }
            context.SaveChanges();

            // ── 5. Posts and Threaded Discussions Across Courses ────────────────────────
            var postDefinitions = new List<PostSeedDef>
            {
                // CSC147
                new PostSeedDef {
                    Code = "CSC147",
                    Author = profRahim,
                    Title = "CSC 147 Midterm Exam Guidelines & Topics Checklist",
                    Body = "Dear students,\n\nThe Midterm Examination for CSC 147 will take place next Tuesday. Topics include:\n1. Loops, nested conditions & switch statements\n2. 1D & 2D Arrays manipulation\n3. Functions with pass-by-value and pass-by-reference (pointers)\n4. String operations without standard library helpers.\n\nPlease check the solved question bank in the course resources.",
                    Flair = PostFlair.Announcement,
                    Upvotes = 38,
                    Downvotes = 0,
                    IsPinned = true,
                    DaysAgo = 6,
                    AcceptedAnswer = (string?)null,
                    AcceptedAuthor = (ApplicationUser?)null
                },
                new PostSeedDef {
                    Code = "CSC147",
                    Author = tanvir,
                    Title = "Understanding pointer arithmetic in C: why does `ptr + 1` increase by 4 bytes for integers?",
                    Body = "I'm practicing pointer arithmetic and noticed that incrementing an `int*` increases the memory address by 4 instead of 1. Can someone explain why this happens and how `sizeof(int)` plays into it?",
                    Flair = PostFlair.Question,
                    Upvotes = 24,
                    Downvotes = 1,
                    IsPinned = false,
                    DaysAgo = 4,
                    AcceptedAnswer = "In C, pointer arithmetic is type-aware! When you do `ptr + 1`, the compiler scales the increment by `sizeof(*ptr)`. Because an `int` on 32/64-bit systems is 4 bytes, `ptr + 1` advances the pointer to point to the next integer element in memory (4 bytes forward). If it were a `double*`, it would advance by 8 bytes!",
                    AcceptedAuthor = fahim
                },
                new PostSeedDef {
                    Code = "CSC147",
                    Author = fahim,
                    Title = "Common Segmentation Fault causes every first-year student should watch out for",
                    Body = "Here are the top 3 reasons you get SegFault in C:\n1. Dereferencing a NULL pointer (`*ptr = 10` without `malloc`).\n2. Writing past array boundaries (`int a[5]; a[5] = 1;`).\n3. Forgetting `&` in `scanf(\"%d\", n);` instead of `&n`.\n\nHope this saves you hours in lab sessions!",
                    Flair = PostFlair.Notes,
                    Upvotes = 47,
                    Downvotes = 0,
                    IsPinned = false,
                    DaysAgo = 2,
                    AcceptedAnswer = (string?)null,
                    AcceptedAuthor = (ApplicationUser?)null
                },

                // CSC247
                new PostSeedDef {
                    Code = "CSC247",
                    Author = profRahim,
                    Title = "CSC 247 Architecture: Direct vs 2-Way Set Associative Cache Tradeoffs",
                    Body = "A reminder for the upcoming exam: understand why 2-way set associative cache reduces conflict misses compared to direct mapped cache, but increases hit latency due to comparator hardware overhead. Check the lecture notes uploaded under Final Exam category.",
                    Flair = PostFlair.Announcement,
                    Upvotes = 32,
                    Downvotes = 0,
                    IsPinned = true,
                    DaysAgo = 7,
                    AcceptedAnswer = (string?)null,
                    AcceptedAuthor = (ApplicationUser?)null
                },
                new PostSeedDef {
                    Code = "CSC247",
                    Author = arif,
                    Title = "How do we calculate the number of Tag, Index, and Offset bits in Cache?",
                    Body = "Given a 32-bit physical address, 64 KB cache, and 64-byte block size in direct mapped cache, what is the exact formula to derive Tag bits, Index bits, and Byte Offset?",
                    Flair = PostFlair.Question,
                    Upvotes = 28,
                    Downvotes = 1,
                    IsPinned = false,
                    DaysAgo = 3,
                    AcceptedAnswer = "Here is the step-by-step formula:\n1. **Block Offset**: log2(Block Size) = log2(64) = 6 bits.\n2. **Number of Lines**: Cache Size / Block Size = 64KB / 64B = 1024 lines.\n3. **Index Bits**: log2(1024) = 10 bits.\n4. **Tag Bits**: 32 - (10 + 6) = 16 bits.\nTotal = 16 Tag + 10 Index + 6 Offset = 32 bits.",
                    AcceptedAuthor = tasmia
                },

                // CSC283
                new PostSeedDef {
                    Code = "CSC283",
                    Author = fahim,
                    Title = "Why favor Composition over Inheritance in OOP? Real-world perspective",
                    Body = "In modern software engineering, 'Favor composition over inheritance' is a golden rule. In inheritance, child classes are tightly coupled with the superclass hierarchy. In composition, you inject dependencies, making components easily testable with mock interfaces.",
                    Flair = PostFlair.Discussion,
                    Upvotes = 41,
                    Downvotes = 0,
                    IsPinned = false,
                    DaysAgo = 3,
                    AcceptedAnswer = (string?)null,
                    AcceptedAuthor = (ApplicationUser?)null
                },
                new PostSeedDef {
                    Code = "CSC283",
                    Author = studentUser,
                    Title = "Difference between `final`, `finally`, and `finalize()` in Java?",
                    Body = "These three words always confuse me in midterms. Can someone give a crisp memory trick to distinguish them?",
                    Flair = PostFlair.Question,
                    Upvotes = 19,
                    Downvotes = 0,
                    IsPinned = false,
                    DaysAgo = 1,
                    AcceptedAnswer = "`final` is a keyword to restrict modification (constant variable, uninheritable class, unoverridable method).\n`finally` is a block paired with try-catch that ALWAYS executes (used for resource cleanup like closing files).\n`finalize()` was an old Object method called prior to garbage collection (now deprecated).",
                    AcceptedAuthor = profRahim
                },

                // CSC391
                new PostSeedDef {
                    Code = "CSC391",
                    Author = profRahim,
                    Title = "CSC 391 Midterm Examination Announcement & Practice Problems",
                    Body = "Dear students,\n\nThe Midterm Examination for CSC 391 (Data Structures and Algorithms) will cover Binary Search Trees, AVL Tree rotations, and Graph BFS/DFS algorithm analysis. Please make sure to attempt the practice problems uploaded in the course resources section.\n\nOffice hours are held every Sunday and Tuesday from 2:00 PM to 4:00 PM.",
                    Flair = PostFlair.Announcement,
                    Upvotes = 49,
                    Downvotes = 1,
                    IsPinned = true,
                    DaysAgo = 8,
                    AcceptedAnswer = (string?)null,
                    AcceptedAuthor = (ApplicationUser?)null
                },
                new PostSeedDef {
                    Code = "CSC391",
                    Author = tasmia,
                    Title = "QuickSort vs MergeSort — Time Complexity & Space Tradeoffs Explained",
                    Body = "Hey everyone! A lot of students asked about why MergeSort requires O(N) auxiliary space while QuickSort is in-place O(1). Here is a quick breakdown:\n\n1. **MergeSort**: Guarantees O(N log N) worst-case, but requires extra space to merge sub-arrays.\n2. **QuickSort**: Average O(N log N), but worst-case can degrade to O(N²) with bad pivot selection.\n\nHope this helps for the upcoming quiz!",
                    Flair = PostFlair.Notes,
                    Upvotes = 37,
                    Downvotes = 0,
                    IsPinned = false,
                    DaysAgo = 5,
                    AcceptedAnswer = (string?)null,
                    AcceptedAuthor = (ApplicationUser?)null
                },
                new PostSeedDef {
                    Code = "CSC391",
                    Author = fahim,
                    Title = "How do you detect a cycle in a Directed Graph using DFS?",
                    Body = "I understand how BFS Kahn's algorithm detects cycles in DAGs, but what is the exact coloring technique (White/Gray/Black) used with recursive DFS?",
                    Flair = PostFlair.Question,
                    Upvotes = 31,
                    Downvotes = 0,
                    IsPinned = false,
                    DaysAgo = 2,
                    AcceptedAnswer = "In 3-color DFS:\n- **White (0)**: Vertex is unvisited.\n- **Gray (1)**: Vertex is currently on the recursion call stack (being explored).\n- **Black (2)**: Vertex and all its descendants are completely explored.\nIf during DFS from vertex U you encounter an edge to a **Gray** vertex V, that edge is a **Back Edge**, meaning a cycle exists!",
                    AcceptedAuthor = profRahim
                },

                // CSC433
                new PostSeedDef {
                    Code = "CSC433",
                    Author = tanvir,
                    Title = "How to resolve 3NF vs BCNF normalization questions in DBMS?",
                    Body = "I am practicing SQL normalization questions for CSC 433 assignment 2. Can someone explain a simple trick to identify if a relation is in 3NF but fails BCNF? Specifically when there are overlapping candidate keys.",
                    Flair = PostFlair.Question,
                    Upvotes = 35,
                    Downvotes = 1,
                    IsPinned = false,
                    DaysAgo = 4,
                    AcceptedAnswer = "Great question! Look at every functional dependency X → Y. For BCNF, X **must** be a superkey in every case. For 3NF, either X is a superkey OR Y is a prime attribute (part of any candidate key). So if Y is a prime attribute and X is NOT a superkey, the table is in 3NF, but NOT in BCNF!",
                    AcceptedAuthor = profRahim
                },
                new PostSeedDef {
                    Code = "CSC433",
                    Author = fahim,
                    Title = "When the SQL query runs fine in your head vs when PostgreSQL executes it 😅",
                    Body = "Spent 3 hours debugging a syntax error only to realize I wrote WHERE after GROUP BY instead of HAVING... Remember: WHERE filters rows before aggregation, HAVING filters grouped sets after aggregation! Stay hydrated folks.",
                    Flair = PostFlair.Meme,
                    Upvotes = 68,
                    Downvotes = 2,
                    IsPinned = false,
                    DaysAgo = 1,
                    AcceptedAnswer = (string?)null,
                    AcceptedAuthor = (ApplicationUser?)null
                },

                // CSC441
                new PostSeedDef {
                    Code = "CSC441",
                    Author = profRahim,
                    Title = "OS Lab Assignment 2: Thread Synchronization with Mutex Locks in C",
                    Body = "All BCSE section students must complete the producer-consumer problem using POSIX pthread mutexes and condition variables. Submission deadline is next Thursday 11:59 PM on the university portal.",
                    Flair = PostFlair.Announcement,
                    Upvotes = 25,
                    Downvotes = 0,
                    IsPinned = true,
                    DaysAgo = 5,
                    AcceptedAnswer = (string?)null,
                    AcceptedAuthor = (ApplicationUser?)null
                },
                new PostSeedDef {
                    Code = "CSC441",
                    Author = arif,
                    Title = "What is the difference between Preemptive and Non-Preemptive SJF (Shortest Job First)?",
                    Body = "Can someone clarify how Shortest Remaining Time First (SRTF) handles newly arriving processes with shorter burst times compared to standard non-preemptive SJF?",
                    Flair = PostFlair.Question,
                    Upvotes = 20,
                    Downvotes = 0,
                    IsPinned = false,
                    DaysAgo = 3,
                    AcceptedAnswer = "In Non-Preemptive SJF, once a CPU burst is allocated to a process, it runs to completion without interruption. In Preemptive SJF (SRTF), whenever a new process arrives with a remaining CPU burst shorter than the currently running process, the OS immediately context-switches and preempts the current process to run the shorter one!",
                    AcceptedAuthor = fahim
                },

                // CSC471
                new PostSeedDef {
                    Code = "CSC471",
                    Author = arif,
                    Title = "Subnetting a /24 network into 4 equal subnets: Cheat Sheet",
                    Body = "To split 192.168.1.0/24 into 4 equal subnets:\n- We need 2 borrowed bits: 2² = 4 subnets.\n- New subnet mask: /26 (255.255.255.192).\n- Subnet block size: 256 - 192 = 64.\nSubnets are: .0/26, .64/26, .128/26, .192/26.\nEach subnet provides 62 usable host addresses (64 - 2).",
                    Flair = PostFlair.Notes,
                    Upvotes = 43,
                    Downvotes = 0,
                    IsPinned = false,
                    DaysAgo = 4,
                    AcceptedAnswer = (string?)null,
                    AcceptedAuthor = (ApplicationUser?)null
                },
                new PostSeedDef {
                    Code = "CSC471",
                    Author = studentUser,
                    Title = "Why does TCP require a 3-Way Handshake instead of just 2?",
                    Body = "Why can't the client and server establish a reliable connection with just SYN and ACK? Why is the final ACK from client necessary?",
                    Flair = PostFlair.Question,
                    Upvotes = 27,
                    Downvotes = 0,
                    IsPinned = false,
                    DaysAgo = 2,
                    AcceptedAnswer = "The third packet (Client's ACK) is necessary to prevent duplicate historical packets from creating ghost connections! If an old delayed SYN packet arrives at the server, the server responds with SYN-ACK. If 2-way was sufficient, a connection would open without client consent. With the 3-way handshake, the client checks the ACK number against its own sequence number and resets invalid connections!",
                    AcceptedAuthor = arif
                },

                // MAT101
                new PostSeedDef {
                    Code = "MAT101",
                    Author = profKarim,
                    Title = "MAT 101 Midterm Exam: Review of Cramer's Rule for 3x3 Systems",
                    Body = "Please make sure to review determinant evaluation using cofactor expansion. Cramer's rule is only applicable when the determinant of the coefficient matrix D is non-zero (D != 0).",
                    Flair = PostFlair.Announcement,
                    Upvotes = 21,
                    Downvotes = 0,
                    IsPinned = true,
                    DaysAgo = 5,
                    AcceptedAnswer = (string?)null,
                    AcceptedAuthor = (ApplicationUser?)null
                },
                new PostSeedDef {
                    Code = "MAT101",
                    Author = sadia,
                    Title = "Help with quadratic equation factoring shortcuts",
                    Body = "What is the quickest way to factor ax² + bx + c when 'a' is not 1 without using the quadratic formula every time?",
                    Flair = PostFlair.Question,
                    Upvotes = 15,
                    Downvotes = 0,
                    IsPinned = false,
                    DaysAgo = 2,
                    AcceptedAnswer = "Use the 'AC method'! Multiply A * C. Find two numbers that multiply to A*C and add up to B. Split the middle term with these two numbers and factor by grouping. It's much faster than trial and error!",
                    AcceptedAuthor = nusrat
                },

                // MAT147
                new PostSeedDef {
                    Code = "MAT147",
                    Author = sadia,
                    Title = "L'Hôpital's Rule: when and how to apply it correctly",
                    Body = "Remember: L'Hôpital's rule can ONLY be applied when evaluating limits that result in indeterminate forms 0/0 or ∞/∞. You must differentiate the numerator and denominator independently (NOT using quotient rule!).",
                    Flair = PostFlair.Notes,
                    Upvotes = 36,
                    Downvotes = 0,
                    IsPinned = false,
                    DaysAgo = 3,
                    AcceptedAnswer = (string?)null,
                    AcceptedAuthor = (ApplicationUser?)null
                },

                // MAT247
                new PostSeedDef {
                    Code = "MAT247",
                    Author = sadia,
                    Title = "Newton-Raphson Method: When does it fail to converge?",
                    Body = "Newton-Raphson fails when the derivative at the current guess is zero (f'(x) = 0, causing division by zero), or when the initial guess is too far from the root, causing oscillation around inflection points.",
                    Flair = PostFlair.Notes,
                    Upvotes = 29,
                    Downvotes = 0,
                    IsPinned = false,
                    DaysAgo = 4,
                    AcceptedAnswer = (string?)null,
                    AcceptedAuthor = (ApplicationUser?)null
                },

                // STA240
                new PostSeedDef {
                    Code = "STA240",
                    Author = nusrat,
                    Title = "How to interpret P-Value in Hypothesis Testing?",
                    Body = "The p-value is the probability of observing test results at least as extreme as the observed results, under the assumption that the null hypothesis is true. If p-value < alpha (e.g. 0.05), we reject the null hypothesis!",
                    Flair = PostFlair.Notes,
                    Upvotes = 32,
                    Downvotes = 0,
                    IsPinned = false,
                    DaysAgo = 3,
                    AcceptedAnswer = (string?)null,
                    AcceptedAuthor = (ApplicationUser?)null
                },

                // ENG101 & ENG102
                new PostSeedDef {
                    Code = "ENG101",
                    Author = nusrat,
                    Title = "How to write a compelling thesis statement for argument essays",
                    Body = "A strong thesis statement must be debatable, concise, and focused. Avoid generic statements like 'Smoking is bad.' Instead write: 'Because secondhand smoke increases respiratory illnesses in children, smoking in multi-family housing should be prohibited.'",
                    Flair = PostFlair.Notes,
                    Upvotes = 26,
                    Downvotes = 0,
                    IsPinned = false,
                    DaysAgo = 5,
                    AcceptedAnswer = (string?)null,
                    AcceptedAuthor = (ApplicationUser?)null
                },
                new PostSeedDef {
                    Code = "ENG102",
                    Author = tasmia,
                    Title = "IEEE Citation format for online web resources & GitHub repos",
                    Body = "For IEEE style: [1] Author, 'Title of webpage or repository,' Website Name, Year. [Online]. Available: URL. [Accessed: Month DD, YYYY]. Make sure to keep formatting consistent across your term paper references.",
                    Flair = PostFlair.Notes,
                    Upvotes = 31,
                    Downvotes = 0,
                    IsPinned = false,
                    DaysAgo = 4,
                    AcceptedAnswer = (string?)null,
                    AcceptedAuthor = (ApplicationUser?)null
                },

                // MGT101
                new PostSeedDef {
                    Code = "MGT101",
                    Author = nusrat,
                    Title = "Henri Fayol's 14 Principles of Management: The Big 4 for Midterms",
                    Body = "If you are studying for the MGT 101 midterm, prioritize these 4 principles:\n1. **Division of Work**: Specialization increases output.\n2. **Unity of Command**: Employees receive orders from one superior only.\n3. **Unity of Direction**: One plan for activities with the same objective.\n4. **Scalar Chain**: The line of authority from top management to lowest ranks.",
                    Flair = PostFlair.Notes,
                    Upvotes = 28,
                    Downvotes = 0,
                    IsPinned = false,
                    DaysAgo = 3,
                    AcceptedAnswer = (string?)null,
                    AcceptedAuthor = (ApplicationUser?)null
                }
            };

            foreach (var pDef in postDefinitions)
            {
                if (courses.ContainsKey(pDef.Code) && pDef.Author != null)
                {
                    int cid = courses[pDef.Code].Id;
                    var existingPost = context.Posts.FirstOrDefault(p => p.CourseId == cid && p.Title == pDef.Title);
                    if (existingPost == null)
                    {
                        var post = new Post
                        {
                            CourseId = cid,
                            UserId = pDef.Author.Id,
                            Title = pDef.Title,
                            Body = pDef.Body,
                            Flair = pDef.Flair,
                            UpVotes = pDef.Upvotes,
                            DownVotes = pDef.Downvotes,
                            IsPinned = pDef.IsPinned,
                            CreatedAt = DateTime.UtcNow.AddDays(-pDef.DaysAgo)
                        };
                        context.Posts.Add(post);
                        context.SaveChanges();

                        // Add accepted answer comment if provided
                        if (!string.IsNullOrEmpty(pDef.AcceptedAnswer) && pDef.AcceptedAuthor != null)
                        {
                            var comment = new Comment
                            {
                                PostId = post.Id,
                                UserId = pDef.AcceptedAuthor.Id,
                                Body = pDef.AcceptedAnswer,
                                UpVotes = 18,
                                DownVotes = 0,
                                IsAcceptedSolution = true,
                                CreatedAt = post.CreatedAt.AddHours(2)
                            };
                            context.Comments.Add(comment);
                            context.SaveChanges();

                            // Set accepted comment ID on post
                            post.AcceptedCommentId = comment.Id;
                            context.SaveChanges();

                            // Follow-up comment
                            context.Comments.Add(new Comment
                            {
                                PostId = post.Id,
                                ParentCommentId = comment.Id,
                                UserId = pDef.Author.Id,
                                Body = "Thank you so much! This made the concept crystal clear.",
                                UpVotes = 5,
                                CreatedAt = comment.CreatedAt.AddMinutes(45)
                            });
                            context.SaveChanges();
                        }
                    }
                }
            }

            // ── 6. Course Followers (Enrolled Subjects) ──────────────────────────────────
            var allCourses = context.Courses.ToList();
            var activeStudents = new[] { studentUser, tasmia, tanvir, fahim, nusrat, arif, sadia };

            foreach (var st in activeStudents)
            {
                if (st != null)
                {
                    // Each student follows 4-6 courses
                    foreach (var c in allCourses.Take(6))
                    {
                        if (!context.CourseFollows.Any(cf => cf.UserId == st.Id && cf.CourseId == c.Id))
                        {
                            context.CourseFollows.Add(new CourseFollow
                            {
                                UserId = st.Id,
                                CourseId = c.Id,
                                FollowedAt = DateTime.UtcNow.AddDays(-15)
                            });
                        }
                    }
                }
            }
            context.SaveChanges();

            // ── 7. Admin Moderation Reports ─────────────────────────────────────────────
            var samplePostToReport = context.Posts.FirstOrDefault(p => p.Flair == PostFlair.Meme);
            if (samplePostToReport != null && !context.Reports.Any(r => r.PostId == samplePostToReport.Id))
            {
                context.Reports.Add(new Report
                {
                    TargetType = ReportTargetType.Post,
                    PostId = samplePostToReport.Id,
                    ReportedByUserId = studentUser?.Id ?? "student",
                    Reason = "Off-topic academic meme in course discussion feed.",
                    Status = ReportStatus.Pending,
                    CreatedAt = DateTime.UtcNow.AddHours(-6)
                });
            }

            var sampleCommentToReport = context.Comments.FirstOrDefault();
            if (sampleCommentToReport != null && !context.Reports.Any(r => r.CommentId == sampleCommentToReport.Id))
            {
                context.Reports.Add(new Report
                {
                    TargetType = ReportTargetType.Comment,
                    CommentId = sampleCommentToReport.Id,
                    ReportedByUserId = tasmia?.Id ?? "student",
                    Reason = "Suspected duplicate or low quality comment.",
                    Status = ReportStatus.Pending,
                    CreatedAt = DateTime.UtcNow.AddHours(-3)
                });
            }

            if (!context.Reports.Any(r => r.Status == ReportStatus.Approved))
            {
                context.Reports.Add(new Report
                {
                    TargetType = ReportTargetType.Post,
                    ReportedByUserId = tanvir?.Id ?? "student",
                    Reason = "Commercial tutoring spam advertisement.",
                    Status = ReportStatus.Approved,
                    AdminNotes = "Spam removed and warning sent.",
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    ResolvedAt = DateTime.UtcNow.AddDays(-1)
                });
            }
            context.SaveChanges();

            // ── 8. User Warnings & Notifications ─────────────────────────────────────────
            if (studentUser != null && !context.UserWarnings.Any(w => w.UserId == studentUser.Id))
            {
                context.UserWarnings.Add(new UserWarning
                {
                    UserId = studentUser.Id,
                    AdminUserId = adminUser?.Id ?? "admin",
                    AdminUserName = "admin",
                    Reason = "Friendly reminder: Please use appropriate flairs (Question/Notes) when posting.",
                    CreatedAt = DateTime.UtcNow.AddDays(-4),
                    IsDismissed = false
                });
            }

            if (studentUser != null && !context.Notifications.Any(n => n.UserId == studentUser.Id))
            {
                context.Notifications.AddRange(
                    new Notification
                    {
                        UserId = studentUser.Id,
                        Title = "Welcome to IUBAT ReadIt! 🎓",
                        Message = "Explore course subReadIts, download exam notes, and join peer discussions.",
                        LinkUrl = "/Courses",
                        Type = NotificationType.System,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow.AddDays(-1)
                    },
                    new Notification
                    {
                        UserId = studentUser.Id,
                        Title = "New Solution Accepted! ⭐",
                        Message = "Your answer on CSC 391 was marked as the accepted solution by the author.",
                        LinkUrl = "/Courses/Details?code=CSC391",
                        Type = NotificationType.AcceptedSolution,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow.AddHours(-10)
                    },
                    new Notification
                    {
                        UserId = studentUser.Id,
                        Title = "📢 University Announcement in c/CSC391",
                        Message = "Midterm examination schedule and practice problems published.",
                        LinkUrl = "/Courses/Details?code=CSC391",
                        Type = NotificationType.Announcement,
                        IsRead = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-3)
                    }
                );
            }

            // ── 9. Post Bookmarks for Student ───────────────────────────────────────────
            var popularPosts = context.Posts.Take(3).ToList();
            if (studentUser != null)
            {
                foreach (var p in popularPosts)
                {
                    if (!context.PostBookmarks.Any(pb => pb.UserId == studentUser.Id && pb.PostId == p.Id))
                    {
                        context.PostBookmarks.Add(new PostBookmark
                        {
                            UserId = studentUser.Id,
                            PostId = p.Id,
                            CreatedAt = DateTime.UtcNow.AddDays(-1)
                        });
                    }
                }
            }

            // ── 10. Initial Admin Audit Logs ────────────────────────────────────────────
            if (!context.AdminLogs.Any())
            {
                context.AdminLogs.AddRange(
                    new AdminLog
                    {
                        AdminUserId = adminUser?.Id ?? "admin",
                        AdminUserName = "admin",
                        ActionType = "System Initialization",
                        TargetDescription = "IUBAT ReadIt Database & Course Catalog",
                        Details = "Seeded 14 academic courses, resources, videos, and initial role policies.",
                        CreatedAt = DateTime.UtcNow.AddDays(-10)
                    },
                    new AdminLog
                    {
                        AdminUserId = adminUser?.Id ?? "admin",
                        AdminUserName = "admin",
                        ActionType = "Broadcast Announcement",
                        TargetDescription = "c/CSC391 — Midterm Guidelines",
                        Details = "Pinned announcement to top of CSC 391 feed.",
                        CreatedAt = DateTime.UtcNow.AddDays(-6)
                    },
                    new AdminLog
                    {
                        AdminUserId = adminUser?.Id ?? "admin",
                        AdminUserName = "admin",
                        ActionType = "Approve Note",
                        TargetDescription = "Note #1 — CSC 391 Solved Exam Papers",
                        Details = "Course: c/CSC391",
                        CreatedAt = DateTime.UtcNow.AddDays(-4)
                    }
                );
            }

            context.SaveChanges();
            Console.WriteLine(">>> [DbSeeder] Database seeding finished successfully!");
        }

        public class PostSeedDef
        {
            public string Code { get; set; } = string.Empty;
            public ApplicationUser? Author { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Body { get; set; } = string.Empty;
            public PostFlair Flair { get; set; }
            public int Upvotes { get; set; }
            public int Downvotes { get; set; }
            public bool IsPinned { get; set; }
            public int DaysAgo { get; set; }
            public string? AcceptedAnswer { get; set; }
            public ApplicationUser? AcceptedAuthor { get; set; }
        }
    }
}
