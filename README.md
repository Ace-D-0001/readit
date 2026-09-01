# IUBAT ReadIt — Academic Community & Discussion Platform

![IUBAT ReadIt](wwwroot/images/logo.svg)

**IUBAT ReadIt** is a full-featured Reddit-style academic community platform built for students and faculty at IUBAT (International University of Business Agriculture and Technology). It connects students across computer science, business, mathematics, and general courses to share course outlines, video playlists, study notes, and threaded discussions.

---

## 🌟 Key Features

### 🎓 1. Student Academic & Community Suite
- **Department & Course Subpages**: Dedicated hubs for subjects such as `c/CSC391` (Data Structures), `c/CSC433` (DBMS), `c/CSC247` (Architecture), `c/MAT247`, `c/ENG101`, and general university courses.
- **Dual Feed Modes**: Switch seamlessly between **All University Feed** and **My Enrolled Subjects** (personalized to followed courses).
- **Exam-Categorized Notes**: Filter and upload study materials by **Midterm**, **Finals**, **Labs / Assignments**, and **Lecture Notes**.
- **Post Bookmarking / Saved Library**: Save helpful questions, notes, and discussions to your private library on your profile.
- **Accepted Solution Checkmark**: Post authors can mark peer answers as the accepted solution with green badge recognition.
- **Anonymous Question Posting**: Students can toggle anonymous posting (`u/AnonymousStudent`) to ask sensitive academic questions without hesitation.
- **Live In-App Notification Bell**: Real-time unread count and dropdown feed notifying students of replies, note approvals, warnings, and announcements.
- **Feed Filtering & Sorting**: Sort discussions by **Hot** (decay algorithm), **Top** (upvotes), or **New**.
- **Interactive Voting & Copy Link**: Upvote/downvote posts and comments, with single-click post sharing.

### 🛡️ 2. Administrative Governance & Control Center
- **Live Cockpit Metrics** (`/Admin`): Real-time analytics for students, pending notes, pending reports, posts, comments, announcements, and banned users.
- **Administrative Audit Action Logs** (`/Admin/AuditLogs`): Immutable trail tracking admin actions (bans, approvals, deletions, warnings, thread locks).
- **Discussion Thread Locking**: Admins can freeze toxic/concluded threads to prevent new comments.
- **Student Formal Warning System**: Issue formal warnings (`UserWarning`) with mandatory student acknowledgement banners.
- **Broadcast Announcements**: Post and pin university notices with optional expiry timestamps (`ExpiresAt`).
- **Content Moderation & Reports Queue** (`/Admin/Reports`): Review flagged posts and comments with 1-click delete, ban, or dismiss.
- **Notes Quality Control** (`/Admin/Notes`): Review pending student note submissions with approve or reject feedback.
- **Subpage Management** (`/Admin/Subpages`): Create, edit, and manage department subjects, descriptions, and curated video playlists.
- **User Accounts & Bans** (`/Admin/Users`): Search student directory, issue warnings, or toggle ban status.

### 🔑 3. Authentication & Role Separation
- **Dual Login Portals**:
  - **Student Portal** (`/Account/Login`): Includes 1-click **Sign in with Google** (OAuth simulation) and student registration.
  - **Admin Portal** (`/Account/AdminLogin`): Dedicated secure login verifying `Admin` role privileges.
- **Role Isolation**: Admins are neutral moderators (voting and course following restricted to students; admin comments and posts badged).
- **Password Reset Flow**: Self-service forgot password & token reset system (`/Account/ForgotPassword`).

### 🎵 4. Floating Glassmorphic Study Music Player
- **Lofi & Focus Audio Dock**: Floating bottom-right music player supporting iTunes audio previews.
- **Interactive UI**: Album cover art, title/artist display, progress timeline seeking, mute toggle button, volume slider, and live track search.
- **Cross-Tab Sync**: Synchronizes playback state and volume across browser tabs.

---

## 🔑 Test Accounts (Development Mode)

| Account Type | Email | Password | Role | Access Level |
| :--- | :--- | :--- | :--- | :--- |
| 🛡️ **Test Admin** | `admin@iubat.edu` | `Admin123!` | `Admin` | Full Dashboard, Reports Moderation, Notes Approval, Subpage CRUD, User Ban |
| 🎓 **Test Student** | `student@iubat.edu` | `Student123!` | `Student` | Google OAuth Sign In, Create Posts/Comments, Report Content, Upload Notes (Pending) |

---

## 🛠️ Technology Stack

- **Framework**: ASP.NET Core 10.0 MVC
- **Database**: Entity Framework Core 10.0 with SQLite (`Read_It.db`)
- **Identity & Security**: ASP.NET Core Identity with Role-Based Access Control (`[Authorize(Roles = "Admin")]`)
- **Frontend**: Razor Pages/Views, HTML5, Vanilla CSS3 (Custom Glassmorphism design tokens), Javascript (ES6+)
- **Icons & Typography**: Bootstrap Icons, Google Fonts (Inter)
- **Audio Engine**: Native HTML5 Audio API with iTunes Search Proxy API

---

## 🚀 How to Run Locally

### Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) installed on Windows/Mac/Linux.

### Steps
1. **Clone the repository**:
   ```bash
   git clone https://github.com/Ace-D-0001/readit.git
   cd readit
   ```

2. **Run the application**:
   ```bash
   dotnet run
   ```

3. **Open in browser**:
   Navigate to **`http://localhost:5165`**

*(Database `Read_It.db` and initial test accounts/courses will be automatically created on first run).*

---

## 📁 Project Structure

```
Read_It/
├── Controllers/
│   ├── AccountController.cs    # Auth, Google login mock, Register, Reset password
│   ├── AdminController.cs      # Dashboard, Reports, Notes approval, Subpage CRUD, Users
│   ├── CoursesController.cs    # Subpage feed, Note uploads, Resource links
│   ├── HomeController.cs       # Home feed & sorting
│   ├── MusicProxyController.cs # iTunes 30s preview proxy
│   └── PostsController.cs      # Posts, Comments, Voting, Reporting
├── Models/
│   ├── ApplicationUser.cs      # Custom IdentityUser with IsBanned flag
│   ├── CourseResource.cs       # Notes & Outlines schema (Status, FilePath)
│   ├── Report.cs               # Post/Comment user flag reports
│   └── Enums.cs                # ResourceStatus, ReportStatus, PostFlair
├── Views/
│   ├── Account/                # Login, AdminLogin, Register, AccessDenied
│   ├── Admin/                  # Dashboard, Reports, Notes, Subpages, Users
│   ├── Courses/                # Course details, Notes upload section
│   ├── Home/                   # Main home feed
│   ├── Posts/                  # Post details, create, edit
│   └── Shared/                 # _Layout.cshtml (Music player, Sidebar, Navbar)
└── wwwroot/
    ├── css/theme.css           # Custom glassmorphic dark theme
    ├── js/study-player.js      # Floating iTunes study music engine
    └── uploads/notes/          # Student PDF note uploads storage
```

---

## 📝 License

Distributed under the MIT License. See `LICENSE` for more information.