# GoKid API

A gamified learning and task management platform for children, built for educational institutions and parents. Children complete daily tasks, participate in multi-day adventures, earn points, and redeem gifts — all under parent and supervisor oversight.

---

## Table of Contents

- [Overview](#overview)
- [User Roles](#user-roles)
- [Core Features](#core-features)
- [Tech Stack](#tech-stack)
- [Architecture](#architecture)
- [Getting Started](#getting-started)
- [Environment Configuration](#environment-configuration)
- [API Reference](#api-reference)
- [Background Jobs](#background-jobs)
- [Real-Time Notifications](#real-time-notifications)

---

## Overview

GoKid is a graduation project that gamifies children's daily learning and task completion. It supports two environments:

- **Home**: Parents assign tasks, set milestone rewards, and review submissions.
- **Institution**: Admins create weekly adventures, supervisors review adventure submissions, and children compete on leaderboards.

---

## User Roles

| Role | Description |
|---|---|
| **Child** | Completes tasks and adventures, earns points, buys gifts, views rankings |
| **Parent** | Creates/assigns tasks, sets rewards, reviews child submissions |
| **InstitutionAdmin** | Manages classes, supervisors, and designs adventures |
| **Supervisor** | Reviews adventure task submissions, tracks class progress |
| **PlatformAdmin** | Manages global task templates and gifts |

---

## Core Features

### Task System
- **4 Task Types**: Instant Reward, Text Question (auto-reviewed), Voice Question, Evidence Submission (photo/video)
- **Difficulty Levels**: Easy, Medium, Hard with corresponding base points
- **Daily Auto-Assignment**: 5 random tasks assigned to each child every day at midnight
- **Parent Assignment**: Parents can manually assign any task template to their child
- **Review Workflow**: Text auto-verified → Voice/Evidence requires parent or supervisor review

### Adventure System
- **Multi-Day Challenges**: Typically 7-day gamified quests with story narration
- **Day-by-Day Progression**: Tasks unlock sequentially; missed days reduce star rating
- **Stars & Points**: Each task awards stars; completing the full adventure gives 50 bonus points
- **AI Story Generation**: Adventures include intro/outro stories with AI text-to-speech narration
- **Supervisor Review**: Supervisors approve/reject each child's adventure task submission
- **Class Assignment**: Institution admins assign adventures to entire classes at once

### Points & Rewards
- **Points Transactions**: Full audit log with source tracking (task, adventure, gift purchase)
- **Parent Rewards**: Parents create milestone rewards (e.g., "reach 500 points → get a movie night")
- **Gift Store**: Platform gifts (badges, characters, collectibles) purchasable with points
- **Leaderboards**: Global top 20 and institution top 3 rankings

### Institution Management
- **Multi-Tenant**: Each institution has its own classes and children
- **Child Enrollment**: Via 6-digit registration code
- **Supervisor Assignment**: Supervisors can manage multiple classes

### Gamification
- Ranking/leaderboard system
- Stars and point milestones
- Collectible badges and characters
- Reward goals set by parents

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 8.0 Web API |
| Database | SQL Server + Entity Framework Core 8.0 |
| Authentication | ASP.NET Identity + JWT Bearer |
| Real-Time | SignalR |
| Background Jobs | Hangfire |
| Caching | Redis |
| File Uploads | Cloudinary |
| Logging | Serilog |
| Validation | FluentValidation |
| Mapping | AutoMapper |
| TTS | Kokoro TTS (Text-to-Speech) |
| Docs | Swagger / OpenAPI |

---

## Architecture

```
GoKidAPI/
├── Controllers/        # HTTP layer — 15 controllers
├── Services/           # Business logic
├── Entity/             # EF Core domain models
│   ├── Account/        # Users, parents, children, supervisors
│   ├── Tasks/          # Task templates, child tasks, categories
│   ├── Adventures/     # Adventures, weekly assignments, progress
│   ├── Institution/    # Institutions, classes
│   └── Gifts/          # Gifts, rewards, points transactions
├── DTO/                # Request/response shapes
├── Enums/              # Status and type enumerations
├── Data/               # AppDbContext + migrations
├── Jobs/               # Hangfire background jobs
├── Hubs/               # SignalR notification hub
└── Validators/         # FluentValidation rule sets
```

**Key patterns:**
- Soft delete via `IsDeleted` flag with global query filters
- Auditable entities (`CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`)
- Service layer between controllers and data access
- AutoMapper for DTO ↔ entity mapping

---

## Getting Started

### Prerequisites
- .NET 8 SDK
- SQL Server
- Redis
- Cloudinary account
- SMTP server (for email/OTP)

### Run Locally

```bash
# Clone the repository
git clone <repo-url>
cd GoKidAPI

# Apply database migrations
dotnet ef database update

# Run the API
dotnet run
```

Swagger UI will be available at `https://localhost:{port}/swagger`.
Hangfire dashboard at `https://localhost:{port}/hangfire`.

---

## Environment Configuration

Configure the following in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Default": "<SQL Server connection string>",
    "Redis": "<Redis connection string>"
  },
  "JwtOptions": {
    "SecretKey": "<min 32-char secret>",
    "Issuer": "GoKidAPI",
    "Audience": "GoKidApp"
  },
  "CloudinaryOptions": {
    "CloudName": "",
    "ApiKey": "",
    "ApiSecret": ""
  },
  "EmailOptions": {
    "Host": "",
    "Port": 587,
    "UserName": "",
    "Password": ""
  }
}
```

---

## API Reference

### Authentication — `/api/account`

| Method | Endpoint | Description | Auth |
|---|---|---|---|
| POST | `/register` | Parent registration | Public |
| POST | `/verify-otp` | Email OTP verification | Public |
| POST | `/login` | Login (all roles) | Public |
| POST | `/refresh-token` | Refresh JWT | Public |
| POST | `/resend-otp` | Resend OTP | Public |
| POST | `/logout` | Invalidate tokens | Auth |
| POST | `/child` | Create child account | Parent |
| POST | `/forgot-password` | Request password reset OTP | Public |
| POST | `/reset-password` | Reset with OTP | Public |
| POST | `/change-password` | Change password | Auth |

### Child Tasks — `/api/child`

| Method | Endpoint | Description | Auth |
|---|---|---|---|
| GET | `/` | Get child's task list | Child |
| GET | `/points` | Get current point balance | Child |
| POST | `/submit/{taskId}` | Submit task answer/evidence | Child |

### Adventures — `/api/adventure`

| Method | Endpoint | Description | Auth |
|---|---|---|---|
| POST | `/` | Create adventure | InstitutionAdmin |
| GET | `/` | List adventures (paginated) | Auth |
| GET | `/{adventureId}` | Adventure details | Auth |
| POST | `/classes/{classId}/assign-adventure` | Assign to class | InstitutionAdmin |
| PUT | `/{adventureId}` | Update adventure | InstitutionAdmin |
| DELETE | `/{adventureId}` | Soft delete | InstitutionAdmin |
| PATCH | `/{adventureId}/status` | Toggle Active/Inactive | InstitutionAdmin |
| POST | `/{adventureId}/generate-story` | AI-generate story | InstitutionAdmin |
| GET | `/{adventureId}/story` | Get story with voice | Auth |

### Child Adventures — `/api/child-adventure`

| Method | Endpoint | Description | Auth |
|---|---|---|---|
| GET | `/` | My weekly adventures | Child |
| GET | `/{weeklyAdventureId}` | Adventure details | Child |
| GET | `/weekly-adventures/{weeklyAdventureId}/tasks` | Day tasks with lock status | Child |
| POST | `/adventure-tasks/submit` | Submit an adventure task | Child |

### Parent Tasks — `/api/parent-task`

| Method | Endpoint | Description | Auth |
|---|---|---|---|
| POST | `/assign` | Assign task to child | Parent |
| GET | `/` | My assigned tasks | Parent |
| GET | `/{childTaskId}/details` | Task review details | Parent |
| POST | `/review` | Approve or reject task | Parent |

### Task Templates — `/api/task-templates`

| Method | Endpoint | Description | Auth |
|---|---|---|---|
| POST | `/instant-reward` | Create instant reward task | PlatformAdmin |
| POST | `/text-question` | Create text question task | PlatformAdmin |
| POST | `/voice-question` | Create voice question task | PlatformAdmin |
| POST | `/evidence-submission` | Create evidence task | PlatformAdmin |
| GET | `/` | List templates (paginated, filterable) | Auth |
| GET | `/{id}` | Get template by ID | Auth |
| GET | `/subcategory/{subCategoryId}` | Filter by subcategory | Auth |
| GET | `/category/{categoryId}` | Filter by category | Auth |

### Supervisor — `/api/supervisor`

| Method | Endpoint | Description | Auth |
|---|---|---|---|
| GET | `/adventures` | My assigned adventures | Supervisor |
| GET | `/adventures/{weeklyAdventureId}/tasks` | Tasks pending review | Supervisor |
| PUT | `/tasks/{childAdventureTaskId}/review` | Approve or reject | Supervisor |
| GET | `/adventures/{weeklyAdventureId}/classes/{classId}/children` | Class progress | Supervisor |
| GET | `/adventures/{weeklyAdventureId}/children/{childId}/history` | Child history | Supervisor |
| GET | `/classes` | My supervised classes | Supervisor |

### Rankings — `/api/ranking`

| Method | Endpoint | Description | Auth |
|---|---|---|---|
| GET | `/global` | Global leaderboard (top 20) | Auth |
| GET | `/institution` | Institution leaderboard (top 3) | Auth |

### Statistics — `/api/statistics`

| Method | Endpoint | Description | Auth |
|---|---|---|---|
| GET | `/` | Stats (ThisWeek / ThisMonth / AllTime) | Auth |

### Rewards — `/api/rewards`

| Method | Endpoint | Description | Auth |
|---|---|---|---|
| POST | `/` | Create reward milestone | Parent |
| DELETE | `/{rewardId}` | Delete reward | Parent |
| GET | `/` | Parent's rewards | Parent |
| PUT | `/{rewardId}/give` | Mark reward as given | Parent |
| GET | `/my-rewards` | Child's received rewards | Child |

### Gifts — `/api/gifts`

| Method | Endpoint | Description | Auth |
|---|---|---|---|
| POST | `/` | Create gift | PlatformAdmin |
| PUT | `/{giftId}` | Update gift | PlatformAdmin |
| DELETE | `/{giftId}` | Delete gift | PlatformAdmin |
| PATCH | `/{giftId}/status` | Toggle status | PlatformAdmin |
| GET | `/` | All gifts | PlatformAdmin |
| GET | `/available` | Available gifts (affordable) | Child |
| POST | `/{giftId}/purchase` | Purchase with points | Child |
| GET | `/my-gifts` | My purchased gifts | Child |

### Classes — `/api/class`

| Method | Endpoint | Description | Auth |
|---|---|---|---|
| POST | `/` | Create class | InstitutionAdmin |
| PUT | `/{classId}` | Update class | InstitutionAdmin |
| DELETE | `/{classId}` | Delete class | InstitutionAdmin |
| GET | `/{classId}` | Class details | Auth |
| GET | `/` | List classes (paginated) | Auth |
| POST | `/assign-supervisor/{classId}` | Assign supervisor | InstitutionAdmin |
| DELETE | `/{classId}/supervisors/{supervisorId}` | Remove supervisor | InstitutionAdmin |
| POST | `/institution/enroll-child` | Enroll child by code | InstitutionAdmin |
| DELETE | `/institution/children/{childId}` | Remove child from institution | InstitutionAdmin |
| POST | `/{classId}/enroll-child` | Assign child to class | InstitutionAdmin |
| DELETE | `/{classId}/children/{childId}` | Remove child from class | InstitutionAdmin |
| GET | `/institution/children` | Institution children (paginated) | InstitutionAdmin |

### Categories — `/api/category` & `/api/task-sub-category`

Standard CRUD for task categories and subcategories (PlatformAdmin write, Auth read).

---

## Background Jobs

Powered by **Hangfire**, running daily at midnight UTC:

| Job | Schedule | Description |
|---|---|---|
| `DailyTaskAssignmentJob` | Daily 12 AM UTC | Assigns 5 random tasks to each child, avoiding duplicates |
| `AdventureAssignmentJob` | Daily 12 AM UTC | Unlocks next adventure day, marks missed tasks, handles completion bonuses |

---

## Real-Time Notifications

**SignalR Hub** at `/hubs/notifications`

Notification types:
- `TaskCompleted` — when a child submits a task
- `RewardAwarded` — when a parent gives a reward
- `AdventureStart` — when a new adventure begins
- Task review results (approved/rejected)

---

## Data Model Highlights

- **Child** has a 6-digit `RegistrationCode` used for login (no password required)
- **Parent** links to one active child at a time
- **ChildTask** status flow: `Pending → InProgress → ReviewRequested → Completed / Rejected`
- **ChildAdventureTask** status flow: `Locked → Pending → Completed / Missed`
- **PointsTransaction** records every point change with source type for full audit trail
- Soft delete everywhere via `IsDeleted` flag — no data is permanently lost

---

*GoKid — Graduation Project*
