# IUBAT ReadIt — Academic Community & Discussion Platform

![IUBAT ReadIt](wwwroot/images/logo.svg)

**IUBAT ReadIt** is a full-featured Reddit-style academic community platform built for students and faculty at IUBAT (International University of Business Agriculture and Technology). It connects students across computer science, business, mathematics, and general courses to share course outlines, video playlists, study notes, and threaded discussions.

---

## 🌟 Key Features

### 🎓 1. SubReadIt Course Communities & Feeds
- **Department & Course Subpages**: Dedicated hubs for subjects such as `c/CSC391` (Data Structures), `c/CSC433` (DBMS), `c/CSC247` (Architecture), `c/MAT247`, `c/ENG101`, and general university courses.
- **Feed Filtering & Sorting**: Sort discussions by **Hot** (decay algorithm), **Top** (upvotes), or **New**.
- **Interactive Voting & Copy Link**: Upvote/downvote posts and comments, with single-click post sharing.

### 🔑 2. Authentication & Role Separation
- **Dual Login Portals**:
  - **Student Portal** (`/Account/Login`): Includes 1-click **Sign in with Google** (Test Student OAuth simulation) and registration.
  - **Admin Portal** (`/Account/AdminLogin`): Dedicated secure login verifying `Admin` role privileges.
- **Security & Account Banning**: Middleware & Controller checks automatically prevent banned users from logging in or creating content.
- **Password Reset Flow**: Self-service forgot password & token reset system (`/Account/ForgotPassword`).

### 🚨 3. Content Reporting & Moderation
- **Post & Comment Reporting**: Students can flag inappropriate posts or comments with specific reasons (`Inappropriate Content`, `Harassment`, `Spam`, `Academic Misconduct`).
- **Admin Moderation Queue** (`/Admin/Reports`):
  - View reported snippets, reasons, reporter, and content author.
  - **Actions**: Approve report, Approve & Delete Content, Approve & Ban Author, or Reject Report.

### 📝 4. Notes Approval System
- **Student Uploads**: Upload PDF/document notes or Google Drive study links directly on subpage courses.
- **Admin Pre-Approval**: All student-submitted notes start in **Pending** status and are **NOT publicly visible** until reviewed.
- **Admin Notes Portal** (`/Admin/Notes`): Admins preview files, approve notes for public release, or reject with custom feedback.

### 🛡️ 5. Admin Dashboard & Subpage Management
- **Live Metrics Dashboard** (`/Admin`): Overview counters for Total Students, Pending Notes, Pending Reports, Total Posts, Total Comments, and Banned Users.
- **Subpage Management** (`/Admin/Subpages`):
  - Create new subpages (e.g. `/CSC440`, `/CSC183`, `/MAT257`).
  - Edit titles, descriptions, general/department tags, and curated YouTube video playlists.
  - Delete/Archive subpages.
- **User Accounts Management** (`/Admin/Users`): View user roles, inspect profiles, and toggle Ban/Unban status.

### 🎵 6. Floating Glassmorphic Study Music Player
- **Lofi & Focus Audio Dock**: Floating bottom-right music player supporting iTunes 30-second audio previews.
- **Interactive UI**: Album cover art, title/artist display, progress timeline seeking, mute toggle button, hover volume popover, and live iTunes track search.
- **Cross-Tab Sync**: Synchronizes volume and playback state across browser tabs via `BroadcastChannel`.

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