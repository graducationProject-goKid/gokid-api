# GoKid API

A gamified learning and task management platform for children, built for educational institutions and parents. Children complete daily tasks, participate in multi-day adventures, earn points, level up, and redeem gifts — all under parent and supervisor oversight.

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
- [Data Model Highlights](#data-model-highlights)
- [Changelog](#changelog)

---

## Overview

GoKid is a graduation project that gamifies children's daily learning and task completion. It supports two environments:

- **Home**: Parents assign tasks, set milestone rewards, and review submissions.
- **Institution**: Admins create weekly adventures, supervisors review adventure submissions, and children compete on leaderboards.

---

## User Roles

| Role | Description |
|---|---|
| **Child** | Completes tasks and adventures, earns points, levels up, buys gifts, views rankings |
| **Parent** | Creates/assigns tasks, sets rewards, reviews child submissions |
| **InstitutionAdmin** | Manages classes, supervisors, and designs adventures |
| **Supervisor** | Reviews adventure task submissions, tracks class progress |
| **PlatformAdmin** | Manages global task templates, gifts, and level definitions |

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

### Children Levels
- **15 Progression Levels**: From Starter (0 pts) to GoKid Legend (22,000 pts), seeded automatically on startup
- **Automatic Progression**: Child's level updates instantly whenever they earn enough points
- **No Downgrade Policy**: Levels only go up — a child never loses their achieved level
- **Level-Up Notifications**: When a child levels up, notifications are sent automatically to:
  - The child
  - The child's parent
  - All supervisors in the child's class (if enrolled in an institution)
- **LevelInfo in all responses**: Every endpoint that returns child data now embeds a `level` object (`id`, `name`, `order`, `badgeUrl`)
- **PlatformAdmin CRUD**: Admins can create, update, delete, and upload badge images for levels

| Order | Name | Min Points |
|---|---|---|
| 1 | Starter | 0 |
| 2 | Explorer | 100 |
| 3 | Adventurer | 250 |
| 4 | Trailblazer | 500 |
| 5 | Guardian | 800 |
| 6 | Hero | 1,200 |
| 7 | Champion | 1,800 |
| 8 | Legend | 2,500 |
| 9 | Master | 3,500 |
| 10 | Grand Master | 5,000 |
| 11 | Superstar | 7,000 |
| 12 | Elite | 9,500 |
| 13 | Ultimate Hero | 13,000 |
| 14 | Mythic | 17,000 |
| 15 | GoKid Legend | 22,000 |

### Notification System
- **Persistent Notifications**: All notifications are stored in the database and retrievable via API
- **Firebase Push Notifications**: Real-time push via FCM to registered mobile devices
- **SignalR**: In-app real-time delivery via WebSocket hub at `/hubs/notifications`
- **Notification Types**: Task assigned/approved/rejected, level up, adventure events, enrollment events, supervisor assignments

### Institution Management
- **Multi-Tenant**: Each institution has its own classes and children
- **Child Enrollment**: Via 6-digit registration code
- **Supervisor Assignment**: Supervisors can manage multiple classes
- **Institution Details**: Includes a classes summary (name, children count, supervisors count, created date)
- **InstitutionAdmin identity**: Admin account ID is now shared with `AspNetUsers` (same pattern as Parent and Child) ensuring JWT claims always match the admin record

### Profile Endpoints
- **Parent Profile**: Returns parent info plus the active child's full summary including level
- **Child Profile**: Returns child info including level, class, institution, and registration code

### Gamification
- Ranking/leaderboard system with level badges
- Stars and point milestones
- Collectible badges and characters
- Reward goals set by parents
- 15-tier level progression with automatic upgrade and notifications

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
| Push Notifications | Firebase Cloud Messaging (FCM) |
| Logging | Serilog |
| Validation | FluentValidation |
| Mapping | AutoMapper |
| TTS | Kokoro TTS (Text-to-Speech) |
| Docs | Swagger / OpenAPI |

---

## Architecture

```
GoKidAPI/
├── Controllers/        # HTTP layer — 16 controllers
├── Services/           # Business logic
│   ├── Auth/           # Login, register, profile
│   ├── LevelProgression/ # Automatic level-up logic and notifications
│   ├── Points/         # Points award + level-up trigger
│   ├── Levels/         # PlatformAdmin CRUD for level definitions
│   ├── Notifications/  # Persistent + push notifications
│   └── ...
├── Entity/             # EF Core domain models
│   ├── Account/        # Users, parents, children, supervisors
│   ├── Tasks/          # Task templates, child tasks, categories
│   ├── Adventures/     # Adventures, weekly assignments, progress
│   ├── Institution/    # Institutions, classes
│   ├── Levels/         # Level entity
│   └── Gifts/          # Gifts, rewards, points transactions
├── DTO/                # Request/response shapes
│   ├── Levels/         # LevelResponse, LevelInfo, CreateLevelRequest, UpdateLevelRequest
│   └── ...
├── Enums/              # Status and type enumerations
├── Data/               # AppDbContext + migrations
├── Jobs/               # Hangfire background jobs
├── Hubs/               # SignalR notification hub
├── Seeder/             # Role, user, category, and level seeders
└── Validators/         # FluentValidation rule sets
```

**Key patterns:**
- Soft delete via `IsDeleted` flag with global query filters
- Auditable entities (`CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`)
- `Response<T>` wrapper on all endpoints via `ResponseHandler`
- `PaginatedList<T>` for paginated responses
- Service layer between controllers and data access
- Shared primary key pattern: `Child.Id == AppUser.Id`, `Parent.Id == AppUser.Id`, `InstitutionAdmin.Id == AppUser.Id`
- AutoMapper for DTO ↔ entity mapping

---

## Getting Started

### Prerequisites
- .NET 8 SDK
- SQL Server
- Redis
- Cloudinary account
- SMTP server (for email/OTP)
- Firebase project (for push notifications)

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

**On first run, the seeder automatically creates:**
- Default roles (PlatformAdmin, Parent, Child, InstitutionAdmin, Supervisor)
- Default platform admin user
- Default task categories
- All 15 child levels

---

## Environment Configuration

Configure the following in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Default": "<SQL Server connection string>",
    "Redis": "<Redis connection string>"
  },
  "JWT": {
    "Secret": "<min 32-char secret>",
    "ValidIssuer": "GoKidAPI",
    "ValidAudience": "GoKidApp"
  },
  "Cloudinary": {
    "CloudName": "",
    "ApiKey": "",
    "ApiSecret": ""
  },
  "EmailSettings": {
    "FromEmail": "",
    "FromName": "GoKid",
    "SmtpServer": "",
    "SmtpPort": 587,
    "Username": "",
    "Password": "",
    "EnableSsl": true
  },
  "Firebase": {
    "CredentialFilePath": "firebase-credentials.json"
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
| GET | `/profile` | Get profile (parent or child) | Auth |

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
| GET | `/{childTaskId}/details` | Task details (child view) | Parent |
| GET | `/{childTaskId}/review` | Task review details (parent view) | Parent |
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
| GET | `/adventures/{weeklyAdventureId}/tasks` | Tasks pending review (with child level) | Supervisor |
| PUT | `/tasks/{childAdventureTaskId}/review` | Approve or reject (triggers level check) | Supervisor |
| GET | `/adventures/{weeklyAdventureId}/classes/{classId}/children` | Class progress (with child level) | Supervisor |
| GET | `/adventures/{weeklyAdventureId}/children/{childId}/history` | Child history (with child level) | Supervisor |
| GET | `/classes` | My supervised classes | Supervisor |

### Rankings — `/api/ranking`

| Method | Endpoint | Description | Auth |
|---|---|---|---|
| GET | `/global` | Global leaderboard — top 20, each item includes `level` | Auth |
| GET | `/institution` | Institution leaderboard — top 3, each item includes `level` | Auth |

### Statistics — `/api/statistics`

| Method | Endpoint | Description | Auth |
|---|---|---|---|
| GET | `/` | Stats (ThisWeek / ThisMonth / AllTime) + child `level` | Auth |

### Levels — `/api/levels`

| Method | Endpoint | Description | Auth |
|---|---|---|---|
| GET | `/` | List all levels (ordered by level order) | PlatformAdmin |
| GET | `/{levelId}` | Get level by ID | PlatformAdmin |
| POST | `/` | Create new level (with optional badge image) | PlatformAdmin |
| PUT | `/{levelId}` | Update level (patch-style, badge upload optional) | PlatformAdmin |
| DELETE | `/{levelId}` | Soft delete level (blocked if children are assigned) | PlatformAdmin |

### Notifications — `/api/notifications`

| Method | Endpoint | Description | Auth |
|---|---|---|---|
| GET | `/` | Get my notifications (paginated) | Auth |
| PUT | `/{notificationId}/read` | Mark notification as read | Auth |
| PUT | `/read-all` | Mark all notifications as read | Auth |
| DELETE | `/{notificationId}` | Delete a notification | Auth |
| POST | `/device-token` | Register FCM device token | Auth |

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
| GET | `/my-child-gifts` | Parent views child's gifts | Parent |

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
| GET | `/institution/children` | Institution children with level (paginated) | InstitutionAdmin |

### Institutions — `/api/institutions`

| Method | Endpoint | Description | Auth |
|---|---|---|---|
| POST | `/` | Create institution + admin account | PlatformAdmin |
| GET | `/` | List institutions | PlatformAdmin |
| GET | `/{institutionId}` | Institution details (includes classes summary) | Auth |
| PUT | `/{institutionId}` | Update institution | InstitutionAdmin |
| DELETE | `/{institutionId}` | Delete institution | PlatformAdmin |
| PATCH | `/{institutionId}/logo` | Upload institution logo | InstitutionAdmin |

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

All notifications are also persisted to the database and retrievable via `GET /api/notifications`.

| Type | Value | Sent To |
|---|---|---|
| `TaskAssigned` | 0 | Child |
| `TaskStarted` | 1 | Child |
| `ReviewRequested` | 2 | Parent |
| `TaskApproved` | 3 | Child |
| `TaskRejected` | 4 | Child |
| `PointsEarned` | 5 | Child |
| `AdventureStarted` | 6 | Child |
| `WeekBonus` | 7 | Child |
| `ChildLinked` | 8 | Parent |
| `GiftPurchased` | 9 | Parent |
| `RewardGiven` | 10 | Child |
| `AdventureNewDay` | 11 | Child |
| `SupervisorAssignedToClass` | 12 | Supervisor |
| `SupervisorUnassignedFromClass` | 13 | Supervisor |
| `ChildEnrolledToClass` | 14 | Supervisor |
| `ChildRemovedFromClass` | 15 | Supervisor |
| `WeeklyAdventureStarted` | 16 | Child |
| `DailyAdventureTasksAssigned` | 17 | Supervisor |
| `AdventureDayCompleted` | 18 | Supervisor |
| `LevelUp` | 19 | Child + Parent + Class Supervisors |

---

## Data Model Highlights

- **Child** has a 6-digit `RegistrationCode` used for login (no password required)
- **Child** has a `LevelId` FK to `Level`; updated automatically by `LevelProgressionService`
- **Parent** links to one active child at a time (`ActiveChildId`)
- **InstitutionAdmin.Id == AppUser.Id** — shared PK, same pattern as Parent and Child — JWT claims always match the admin record
- **ChildTask** status flow: `Pending → InProgress → ReviewRequested → Completed / Rejected`
- **ChildAdventureTask** status flow: `Locked → Pending → Completed / Missed`
- **PointsTransaction** records every point change with source type for full audit trail
- **Level** is soft-deletable; deletion is blocked if any child is assigned to it
- Soft delete everywhere via `IsDeleted` flag — no data is permanently lost

### LevelInfo — embedded in all child responses

Every endpoint that returns child data embeds a `level` object:

```json
{
  "level": {
    "id": "string",
    "name": "Guardian",
    "order": 5,
    "badgeUrl": "https://..."
  }
}
```

`level` is `null` if no level has been assigned to the child yet (e.g., child has 0 points and no level seeded).

### Level Progression Flow

Points are awarded in three paths, all triggering a level check:

| Path | Trigger |
|---|---|
| Child completes `InstantReward` or `VoiceQuestion` adventure task | `PointsService.AwardPointsAsync` → level check internally |
| Supervisor approves `EvidenceSubmission` adventure task | `SupervisorService.ReviewChildTaskAsync` → explicit level check |
| Parent approves a child task | `ParentTaskService.ReviewChildTaskAsync` → explicit level check |

---

## Changelog

### Latest Updates

#### Children Levels Feature
- Added `Level` entity with `Id`, `Name`, `Order`, `MinPoints`, `BadgeUrl`, `BadgePublicId`
- 15 levels seeded automatically on startup (Starter → GoKid Legend)
- Full CRUD for PlatformAdmin at `GET/POST/PUT/DELETE /api/levels` with badge upload via Cloudinary
- `Child.LevelId` FK added; EF configured with `DeleteBehavior.Restrict`
- `LevelInfo` lightweight DTO (`id`, `name`, `order`, `badgeUrl`) embedded in all child-related responses

#### Level Progression Service
- `ILevelProgressionService` / `LevelProgressionService` — centralised upgrade logic
- Compares child's `TotalPoints` against all active levels; picks the highest qualifying level
- Never downgrades — only upgrades
- On level-up: notifies child, parent, and all class supervisors via `NotificationType.LevelUp` (value 19)
- `PointsService.AwardPointsAsync` now accepts `updatedBy`, saves internally, and triggers level check — any future code that uses this service gets level progression automatically

#### LevelInfo Integration (all child endpoints)
Updated DTOs: `ClassChildrenListResponse`, `ChildAdventureHistoryResponse`, `InstitutionChildResponse`, `ChildAdventureTaskReviewResponse`, `RankingItemResponse`, `ParentStatisticsResponse` (replaced `CurrentLevel` string), `ChildTaskDetailsResponse`, `ParentChildTaskDetailsResponse`

Updated services: `StatisticsService`, `RankingService`, `ClassService`, `ParentTaskService`, `SupervisorService`

#### Institution Admin Identity Fix
- `InstitutionAdmin.Id` is now set to `AppUser.Id` at creation time (shared PK pattern, same as `Parent` and `Child`)
- Removed the previous separate `AppUserId` property that caused a mismatch between JWT claims and database lookups
- Updated `InstitutionService`, `InstitutionSupervisorService`, and `AdventureService` to use `a.Id` instead of `a.AppUserId`

#### Institution Details — Classes Summary
- `InstitutionDetailsResponse` now includes a `classes` array with `ClassSummary` objects
- Each `ClassSummary` contains: `id`, `name`, `childrenCount`, `supervisorsCount`, `createdAt`

#### Persistent Notifications
- All notifications stored in `Notifications` table with `IsRead` flag
- `GET /api/notifications` — paginated notification list
- `PUT /api/notifications/{id}/read` and `/read-all` — mark as read
- Device token registration for Firebase push delivery

#### Profile Endpoints
- `GET /api/account/profile` returns full profile for the authenticated user (Parent or Child)
- Parent profile includes `activeChild` with level info
- Child profile includes level, class, institution, and registration code

---

*GoKid — Graduation Project*
