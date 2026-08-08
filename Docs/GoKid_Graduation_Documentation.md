# GoKid — Graduation Project Documentation

**Document scope:** Chapter 1 (Introduction), Chapter 3 (Data Modeling), Chapter 6 (Conclusion & Future Work)
**Basis:** Direct static analysis of the GoKidAPI ASP.NET Core 8 solution — entities, `AppDbContext`, controllers, service layer, and EF Core migration history. No content in this document is invented; where the source code does not provide sufficient information, this is stated explicitly rather than assumed.

---

## Chapter 1 — Introduction

### 1.1 Introduction

GoKid is a gamified child-development platform composed of a central ASP.NET Core 8 Web API (GoKidAPI) that serves three distinct classes of client: a parent-facing mobile application, a child-facing mobile application, and institution-facing web dashboards used by school/nursery administrators and class supervisors. The system is built around a single, unified identity model — every human actor in the platform (parent, child, institution administrator, supervisor, and platform administrator) is represented as one row in the ASP.NET Core Identity user table (`AspNetUsers`), differentiated by a `UserType` discriminator and an associated Identity role. Role-specific profile data is then attached through a satellite table pattern (`Parents`, `Childrens`, `Supervisors`, `InstitutionAdmins`), which keeps authentication concerns and domain concerns cleanly separated while still allowing a single sign-on/token-issuance pipeline to serve every actor in the system.

At its core, GoKid implements a task-and-reward loop: children complete tasks — either informal, parent-assigned "general" tasks, or structured, day-by-day "Adventures" issued by an educational institution — and are rewarded with points, badges (Levels), and redeemable Gifts. The platform layers a narrative, story-driven experience (the Adventure system, including AI-generated narrative text and synthesized narration audio) on top of a conventional task-management substrate, distinguishing it from a purely administrative chore-tracking tool.

The system was designed to satisfy three cooperating stakeholder groups simultaneously:

| Stakeholder | Primary need addressed by GoKid |
|---|---|
| Parents | A structured way to assign, monitor, and positively reinforce their child's daily habits and responsibilities, without requiring in-person supervision at all times. |
| Educational institutions (schools/nurseries) | A tool to extend structured, curriculum-aligned activities ("Adventures") beyond the classroom, with supervisor oversight and measurable engagement data. |
| Children | An engaging, game-like experience — points, levels, badges, and redeemable rewards — that reframes routine responsibilities as an adventure rather than an obligation. |

### 1.2 Vision

To establish a digital environment in which the everyday responsibilities of childhood — habits, learning tasks, and institutional assignments — are transformed into a continuous, positively reinforced growth journey, jointly visible to parents, educators, and children, so that character development is measurable, motivating, and shared across the environments in which a child grows.

### 1.3 Mission

To provide a reliable, secure, and role-aware backend platform that:

1. Unifies task assignment, submission, and review across two parallel tracks — informal parent-assigned tasks and formal institution-driven "Adventures" — under one consistent data model and API surface.
2. Converts completed responsibilities into a transparent, auditable points economy (`PointsTransaction`), a non-regressive level-progression system, and a redeemable gift/reward catalog.
3. Keeps every stakeholder informed in real time through a dual-channel notification system (push notifications for mobile actors, SignalR for web-dashboard actors).
4. Gives institutions the administrative tooling (classes, supervisors, enrollment by registration code, dashboards) needed to operate the platform at scale across multiple schools.

### 1.4 Motivation

Two adjacent, well-understood problem spaces motivated this project:

- **Parental habit formation tools** typically operate in isolation from a child's institutional environment — a parent's chore app has no visibility into what a child is doing at school, and a school's activity system has no channel back to the parent. GoKid's data model treats `Parent`, `Institution`, and `Child` as first-class, interlinked entities specifically so that a single child's `TotalPoints`, `Level`, and task history are shared, consistent facts across both contexts, rather than duplicated or siloed.
- **Gamification in children's applications is frequently reduced to cosmetic badges** with no underlying economy. GoKid was motivated by the intent to build a genuine, closed-loop points economy: every point awarded is traceable to a `PointsTransaction` with a `SourceType` (task completion, adventure bonus, achievement, etc.), and every point spent (on a `Gift`) is equally logged as a negative transaction — while a separate, spend-independent `HighestPoints` field preserves competitive ranking integrity even after a child spends their balance. This is a deliberate design decision (discussed further in Section 1.6) rather than an incidental side effect of the schema.

### 1.5 Problem Definition

The problem addressed by GoKid can be decomposed into four concrete sub-problems, each of which is directly reflected in a corresponding subsystem of the implemented API:

1. **Fragmented oversight of a child's responsibilities.** A parent cannot easily assign, and later verify, non-academic tasks (chores, habits) in a way that also produces a rewarding experience for the child. *(Addressed by the `ChildTask` / `ParentTaskService` subsystem.)*
2. **Lack of an extensible institutional activity model.** Schools and nurseries need to assign structured, multi-day, curriculum-like activities to entire classes, track each child's day-by-day progress, and route evidence of completion to a human reviewer (a supervisor) rather than relying purely on self-report. *(Addressed by the `Adventure` / `WeeklyAdventure` / `ChildAdventureTask` / `SupervisorService.ReviewChildTaskAsync` subsystem.)*
3. **Absence of a unified, tamper-evident rewards economy.** Without a single ledger of point-earning and point-spending events, a gamified system cannot guarantee that a child's displayed rank or balance is trustworthy, nor can it prevent a level from being lost simply because points were spent. *(Addressed by `PointsTransaction`, `PointsService.AwardPointsAsync`, and the explicit non-downgrading rule in `LevelProgressionService`.)*
4. **Disconnected communication across three different client surfaces (parent app, child app, institution web dashboard).** A task review event, a level-up, or a new institutional assignment must reach the correct actor through the correct channel — a push notification for a mobile user, an in-browser real-time event for a dashboard user. *(Addressed by `NotificationService`, which persists every event and then branches delivery by `UserType` — Firebase Cloud Messaging for Parent/Child, SignalR for InstitutionAdmin/Supervisor.)*

### 1.6 Proposed Solution

GoKid's proposed solution is a single ASP.NET Core 8 Web API, backed by SQL Server via Entity Framework Core, structured around the following architectural decisions:

**1. A unified identity table with role-specific satellite profiles.** Rather than modeling `Parent`, `Child`, `InstitutionAdmin`, and `Supervisor` as independent authentication systems, all five actor types (`Parent`, `Child`, `InstitutionAdmin`, `Supervisor`, `PlatformAdmin`) share the single ASP.NET Identity `AspNetUsers` table, disambiguated by a `UserType` enumeration and an ASP.NET Identity role. `Parent`, `Child`, and `InstitutionAdmin` share their primary key directly with their `AspNetUsers` row (a strict 1-to-1 "identity extension" pattern with cascading delete), while `Supervisor` is intentionally modeled with an independent primary key plus an `AppUserId` foreign key — reflecting that a supervisor's institutional record can be provisioned by an `InstitutionAdmin` slightly ahead of, or independently from, strict identity-table coupling. This is a deliberate simplification: rather than five separate authentication mechanisms, the platform maintains a single JWT-issuance pipeline (`AuthService` + `ITokenStoreService`) and simply varies the login *method* — email/password for adult actors, a system-generated six-digit `RegistrationCode` (no password) for children, reflecting the reality that a young child cannot be expected to manage a password.

**2. Two parallel, converging task tracks.** The platform deliberately does not force institutional and parental task assignment into a single workflow, because their review semantics differ: a parent-assigned `ChildTask` is typically self-reviewed by the parent, while an institution-assigned `ChildAdventureTask` must be reviewed by a `Supervisor` who has been explicitly granted access to that class via the `ClassSupervisor` join entity. Both tracks, however, converge on the same reward primitive — `PointsService.AwardPointsAsync` — so that the child experiences one continuous point balance and level-progression path regardless of which track produced the achievement. This convergence point is the most important architectural decision in the system: it is the single place where task-track–specific business logic (evidence review, auto-graded answers, AI-evaluated voice submissions) hands off to track-agnostic gamification logic (points, levels, notifications).

**3. A non-destructive, soft-delete-oriented persistence strategy.** Domain records (institutions, classes, gifts, levels, task templates, etc.) are not physically deleted; an `IsDeleted` flag (inherited from a shared `AuditableEntity` base class) is set instead, and referential-integrity foreign keys are configured with `Restrict` delete behavior rather than `Cascade` wherever a hard delete could silently orphan historical or financial (points-related) data. This ensures that a child's historical `PointsTransaction` ledger, past `ChildTask`/`ChildAdventureTask` submissions, and level history remain intact and auditable even after an institution, class, or gift catalog item is retired.

**4. Layered, feature-oriented service architecture.** Each functional area of the system (Adventures, Points, Levels, Gifts, Rewards, Rankings, Statistics, Dashboards, Notifications, Institution/Class/Supervisor management) is implemented as an isolated service behind an interface (`I*Service` / `*Service`), consumed by thin controllers whose responsibility is limited to request/response marshaling, `[Authorize]` enforcement, and delegating to the service layer. This separation allows, for example, the three dashboard aggregation services (Platform, Institution, Supervisor) to independently apply differing cache lifetimes (5, 5, and 3 minutes respectively) without touching controller code.

**5. Dual-channel, asynchronous notification delivery.** Every notable domain event (task approval/rejection, level-up, new adventure assignment, gift purchase, class/supervisor assignment) is first durably persisted to a `Notifications` table (so a client can always retrieve notification history even if it was offline at delivery time), and then fanned out through the channel appropriate to the recipient's `UserType` — Firebase Cloud Messaging push for mobile actors (Parent, Child), and a SignalR hub (`NotificationHub`) real-time event for web-dashboard actors (InstitutionAdmin, Supervisor).

### 1.7 System Architecture

GoKid follows a **layered (N-tier) monolithic API architecture** — a single deployable ASP.NET Core 8 application exposing REST endpoints consumed by heterogeneous clients, rather than a distributed microservices topology. This is an appropriate and defensible choice for the project's current scale: the domain is cohesive (a single relational schema with dense cross-entity relationships — Institution, Class, Child, Adventure — that would be costly to partition across service boundaries), and a monolith allows the single EF Core `DbContext` to enforce referential integrity (soft-delete filters, restrict-on-delete constraints) consistently across the entire domain, which would otherwise require distributed-transaction or eventual-consistency handling.

**Layers, bottom to top:**

| Layer | Composition | Responsibility |
|---|---|---|
| Data Access | `AppDbContext` (EF Core, SQL Server), `Migrations/` | Schema definition, relationship/constraint enforcement, soft-delete query filters, enum-to-string conversion |
| Domain / Entity | `Entity/*` | Persistence-mapped domain classes (Identity extensions, Institution/Class/Adventure graph, Task system, Points/Levels, Gifts/Rewards, Notifications) |
| Service (business logic) | `Services/*` (one folder per feature, `I*Service` + implementation) | All business rules: authentication, points economy, level progression, task submission/review, dashboard aggregation, notification routing |
| API / Presentation | `Controllers/*` (20 controllers) | HTTP endpoint definition, model binding (including multipart/form-data for image/voice uploads), `[Authorize(Roles=...)]` enforcement, response shaping via a shared `Response<T>` envelope |
| Cross-cutting infrastructure | `Extensions/ServiceCollectionExtensions.cs` (DI wiring), `Hubs/NotificationHub` (SignalR), `Seeder/*` (startup data seeding), `Validators/*` (FluentValidation), `Helpers/`, `Shared/` (pagination, response wrapper) | Dependency injection composition, real-time transport, data seeding, input validation, shared utilities |

**External/integrated systems:**

| Integration | Purpose |
|---|---|
| Firebase Cloud Messaging | Push notification delivery to parent/child mobile clients |
| Cloudinary | Image upload/hosting (avatars, gift images, institution logos, level badges, task/category icons, adventure banners) |
| A Text-to-Speech engine ("Kokoro TTS", `IStoryTtsService`) | Converts AI-generated Adventure story text into narrated audio |
| An AI story-generation component (`IStoryGenerationService`) | Generates Adventure narrative text (intro/day-by-day/outro) |
| A voice-shadowing/evaluation API (`CallVoiceShadowingApi` in `ChildTaskService`) | Evaluates a child's spoken-voice task submission against an expected answer |
| Duende IdentityServer EF operational store (`DeviceCodes`, `Keys`, `PersistedGrants` tables) | Present in the schema as infrastructure for OAuth/OIDC token management; the current login flow issues JWTs directly through a custom `ITokenStoreService` rather than a full OIDC authorization-code flow — see the note in Section 1.8 (Scope) regarding this partially-utilized component |
| QRCoder | Generates a QR-encoded representation of a child's registration code, for streamlined child-device login |

**Suggested diagram — "GoKid System Architecture" (to be recreated in Draw.io/Visio):**

A layered architecture diagram with four horizontal swimlanes, top to bottom:

1. **Client layer** (three boxes side-by-side): "Parent Mobile App", "Child Mobile App", "Institution Web Dashboard" — each with an arrow labeled "HTTPS / REST + JWT" pointing down into the API layer, plus a second, dashed arrow from the Web Dashboard box labeled "SignalR (WebSocket)" and a dashed arrow from the mobile app boxes labeled "FCM Push" — both arrows originating from the Notification/Real-time component described below.
2. **API layer** (one wide box: "GoKidAPI — ASP.NET Core 8"), containing three stacked internal bands: "Controllers (20)", "Services (feature-organized business logic)", "Cross-cutting: Auth (JWT/Identity), Validation, Response Envelope".
3. **Data layer** (one box: "AppDbContext / EF Core") with an arrow down to a cylinder labeled "SQL Server — GoKid Database".
4. **External services band** (four small boxes to the side of the API layer, each with a bidirectional arrow into the Services band): "Firebase Cloud Messaging", "Cloudinary", "AI Story Generation + TTS", "Voice Evaluation API".

A fifth small box, "NotificationHub (SignalR)", should sit inside the API layer box and connect outward to the Web Dashboard client box, distinct from the FCM arrow into the mobile clients.

### 1.8 Scope

**In scope (implemented and verified in source):**

- Unified authentication/authorization for five actor roles (Parent, Child, InstitutionAdmin, Supervisor, PlatformAdmin) via ASP.NET Core Identity + custom JWT issuance with refresh-token rotation.
- Parent self-registration with OTP email verification; child account creation by a parent via a system-generated registration code and QR code (password-less child login).
- Institution management: institution CRUD by a platform administrator, automatic institution-administrator account provisioning, class management, supervisor provisioning and class assignment, child enrollment into an institution and a class via registration code.
- Two task-assignment tracks: informal parent-assigned tasks (`ChildTask`) and structured, day-by-day institutional "Adventures" (`Adventure` → `WeeklyAdventure` → `ChildAdventureTask`), each with four task template types (Instant Reward, Text Question, Voice Question, Evidence Submission) and type-appropriate review/validation logic (self-grading, parent review, supervisor review, or AI evaluation).
- A points economy (`PointsTransaction` ledger), a non-regressive level-progression system (`Level`, `LevelProgressionService`), a platform-wide redeemable gift catalog (`Gift`/`ChildGift`), and parent-defined custom rewards (`Reward`) tied to a child-specific point target.
- Global and institution-scoped leaderboards (`RankingService`), and period-based personal performance statistics (`StatisticsService`).
- Three role-scoped, cached analytics dashboards (Platform, Institution, Supervisor).
- A persisted, dual-channel (push + real-time) notification system covering 20 distinct notification event types.
- AI-assisted narrative generation and text-to-speech narration for the Adventure storytelling feature.

**Explicitly out of scope / not covered by this documentation, because it is not observable from the backend source code alone:**

- The internal design, technology stack, and UX of the Parent mobile application, Child mobile application, and Institution web dashboard front-ends — these are separate client codebases not included in this analysis. Any front-end architecture chapter would require access to those repositories.
- Infrastructure/deployment topology (hosting provider, containerization, CI/CD pipeline, environment configuration, horizontal scaling strategy) — the API project includes a `PublishProfiles` folder suggesting IIS/Azure Web Deploy publishing, but no infrastructure-as-code or deployment-pipeline definitions were found in the analyzed project to document with confidence.
- Formal load, performance, or security-penetration testing results — none were present in the repository at the time of analysis.

**Known implementation gaps observed during analysis, relevant to a "Limitations" discussion:** several controllers (`TaskTemplateController`, `ClassController`, `CategoryController`, `TaskSubCategoryController`, `ChildController`, `ChildAdventureController`, `ParentTaskController`, `RewardsController`, `InstitutionSupervisorController`) either have their `[Authorize(Roles=...)]` attribute commented out or rely solely on service-layer claim inspection rather than a declarative authorization attribute. This is noted here factually, as it directly affects any "Security Architecture" or "Testing" chapter the author writes subsequently, and should be addressed (re-enabling and verifying the commented attributes) before the system is considered production-ready.

---

## Chapter 3 — Data Modeling

### 3.1 Data Modeling Approach

The GoKid data model is implemented with **Entity Framework Core (Code-First)** against Microsoft SQL Server, expressed through Fluent API configuration in `AppDbContext.OnModelCreating` rather than through data-annotation attributes alone, which allows precise per-relationship control over delete behavior, composite keys, and filtered unique indexes. Two model-wide conventions are applied uniformly across the schema and are worth documenting explicitly, since they are non-default EF Core behavior discovered directly in the source:

1. **Every enum-typed property, across every entity, is persisted as a string column** (`HasConversion<string>()`), applied generically via reflection over the entire model rather than being configured per-property. This trades a small amount of storage/index efficiency for materially improved database readability and safer schema evolution (adding a new enum member does not renumber existing stored values).
2. **Soft deletion is the default deletion strategy.** A shared `AuditableEntity` base class contributes `IsDeleted`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, and `UpdatedBy` to every entity that inherits it. EF Core global query filters (`HasQueryFilter(x => !x.IsDeleted)`) are configured for `Parent`, `Child`, `TaskCategory`, `TaskTemplateBase`, `ChildTask`, `PointsTransaction`, and `Level`; the remaining auditable entities (`Institution`, `Supervisor`, `InstitutionAdmin`, `Gift`, `Reward`, `ChildGift`, `Adventure`, `Class`) rely on an explicit `.Where(x => !x.IsDeleted)` predicate applied inside the corresponding service query, rather than a global filter. This inconsistency is noted as a discussion point for a "lessons learned" or "future work" section — a fully global filter across all auditable entities would be a safer and more maintainable convention.

The schema separates cleanly into five conceptual sub-models, each discussed as a distinct cluster below:

- **Identity & Actor Profiles** — `AppUser`/`AppRole` (ASP.NET Core Identity) plus `Parent`, `Child`, `Supervisor`, `InstitutionAdmin` satellite profile tables.
- **Institutional Structure** — `Institution`, `Class`, `ClassSupervisor`.
- **Adventure (Institutional Task) System** — `Adventure`, `AdventureTask`, `WeeklyAdventure`, `ChildAdventureProgress`, `ChildAdventureTask`.
- **General Task System** — `TaskCategory`, `TaskSubCategory`, `TaskTemplateBase`, `ChildTask`.
- **Gamification & Commerce** — `PointsTransaction`, `Level`, `Gift`, `ChildGift`, `Reward`, plus the cross-cutting `Notification` entity.

### 3.2 Logical ERD

The logical ERD abstracts away SQL Server column types and focuses on entities, attributes at a conceptual level, and relationship cardinality/ownership — suitable as the first diagram presented in a data-modeling chapter, before the physical ERD.

**Entity list with key conceptual attributes:**

| Entity | Key conceptual attributes |
|---|---|
| AppUser | Email, PasswordHash, DisplayName, AvatarUrl, UserType, FcmToken |
| Parent | ActiveChild (reference) |
| Child | Name, Age, Gender, RelationshipToParent, TotalPoints, HighestPoints, RegistrationCode |
| Supervisor | (belongs to one Institution; supervises many Classes) |
| InstitutionAdmin | (administers exactly one Institution) |
| Institution | Name, Code, contact/profile details |
| Class | Name |
| Adventure | Bilingual title/description/goal, WeekDuration, BonusPoints, Status |
| AdventureTask | DayNumber, Stars, story text/voice |
| WeeklyAdventure | StartDate, EndDate, Status |
| ChildAdventureProgress | EarnedStars, EarnedPoints, CompletedDaysCount, IsCompleted |
| ChildAdventureTask | Status, EarnedStars, EvidenceUrl, ReviewedBy/At |
| TaskCategory | Bilingual name, color, icon |
| TaskSubCategory | Bilingual name, icon |
| TaskTemplateBase | Bilingual title/description, TemplateType, Difficulty, BasePoints, type-specific fields |
| ChildTask | Source, Status, submission/review timestamps |
| PointsTransaction | Points (signed), Reason, SourceType |
| Level | Name, Order, MinPoints, Badge |
| Gift | Bilingual name/description, PointsCost, Type, Status |
| ChildGift | PointsSpent, PurchasedAt |
| Reward | Bilingual name/description, TargetPoints, Status |
| Notification | Type, Title, Body, IsRead |

**Logical relationships (entity-level, cardinality as implemented):**

| Relationship | Cardinality | Nature |
|---|---|---|
| AppUser – Parent | 1 : 1 | Identity extension |
| AppUser – Child | 1 : 1 | Identity extension |
| AppUser – InstitutionAdmin | 1 : 1 | Identity extension |
| AppUser – Supervisor | 1 : 1 | Identity extension (independent key) |
| Parent – Child (active) | 1 : 0..1 | "Currently active child" convenience reference |
| Parent(AppUser) – Child (owned) | 1 : * | Ownership; a parent may have multiple child records, though current client UX exposes one active child |
| Institution – InstitutionAdmin | 1 : 1 | Mutual ownership |
| Institution – Supervisor | 1 : * | |
| Institution – Class | 1 : * | |
| Institution – Child (enrolled) | 1 : * | |
| Institution – Adventure | 1 : * | |
| Class – Child | 1 : * | |
| Class – Supervisor | * : * | Via `ClassSupervisor` |
| Class – WeeklyAdventure | 1 : * | |
| Adventure – AdventureTask | 1 : * | Template day-tasks |
| Adventure – WeeklyAdventure | 1 : * | Assignment instances |
| AdventureTask – TaskTemplateBase | * : 1 | |
| WeeklyAdventure – ChildAdventureTask | 1 : * | |
| WeeklyAdventure – ChildAdventureProgress | 1 : * | |
| ChildAdventureProgress – ChildAdventureTask | 1 : * | Optional grouping |
| Child – ChildAdventureTask / ChildAdventureProgress / ChildTask / PointsTransaction / ChildGift / Reward | 1 : * (each) | |
| Child – Level | * : 1 (optional) | |
| TaskCategory – TaskSubCategory | 1 : * | |
| TaskSubCategory – TaskTemplateBase | 1 : * | |
| TaskTemplateBase – ChildTask / AdventureTask | 1 : * (each) | |
| Gift – ChildGift | 1 : * | |
| AppUser – Notification | 1 : * | |

**Suggested diagram — "Logical ERD" (Draw.io/Visio recreation guidance):**

Lay the diagram out in four visual clusters connected by relationship lines, arranged left-to-right:

1. **Identity cluster** (left): `AppUser` in the center with four entities around it (`Parent`, `Child`, `Supervisor`, `InstitutionAdmin`), each connected by a 1:1 line (double-bar-single-bar crow's foot notation), except the `Supervisor` connection, which should carry the same 1:1 notation but be visually annotated "independent surrogate key."
2. **Institutional cluster** (upper middle): `Institution` connected to `InstitutionAdmin` (1:1), `Supervisor` (1:*), `Class` (1:*), `Child` (1:*, labeled "enrolled"), `Adventure` (1:*).
3. **Adventure cluster** (right): `Adventure` → `AdventureTask` (1:*) → `TaskTemplateBase` (*:1, cross-link to the General Task cluster); `Adventure` → `WeeklyAdventure` (1:*) → `ChildAdventureTask` (1:*) and `ChildAdventureProgress` (1:*), with a secondary line `ChildAdventureProgress` → `ChildAdventureTask` (1:*, dashed to indicate optional).
4. **General Task / Gamification cluster** (lower middle/right): `TaskCategory` → `TaskSubCategory` (1:*) → `TaskTemplateBase` (1:*) → `ChildTask` (1:*); `Child` at the center of this cluster fanning out to `ChildTask`, `PointsTransaction`, `Level` (*:1), `ChildGift`, `Reward`; `Gift` → `ChildGift` (1:*).

Use a distinct fill color per cluster (e.g., blue for Identity, green for Institutional, orange for Adventure, purple for Gamification) to visually communicate the four sub-models at a glance — this is the single most useful visual aid for a reader unfamiliar with the domain.

### 3.3 Physical ERD

The physical ERD reflects the actual SQL Server tables as created by the EF Core migrations, including primary keys, foreign keys, nullability, and the constraints enforced in `AppDbContext.OnModelCreating`. Only the domain schema is detailed below; the ASP.NET Identity/Duende IdentityServer infrastructure tables (`AspNetRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserRoles`, `AspNetUserTokens`, `DeviceCodes`, `Keys`, `PersistedGrants`) exist in the database per the initial migration but are standard framework tables outside the custom domain model.

**Core domain tables, keys, and constraints:**

| Table | Primary Key | Foreign Keys | Notable constraints |
|---|---|---|---|
| AspNetUsers | Id (string) | — | Unique filtered index on `Email` where `Email IS NOT NULL` (children have no email) |
| Parents | Id (string, = AspNetUsers.Id) | Id → AspNetUsers.Id (cascade); ActiveChildId → Childrens.Id (restrict) | |
| Childrens | Id (string, = AspNetUsers.Id) | Id → AspNetUsers.Id (cascade); ParentId → AspNetUsers.Id (restrict); ClassId → Classes.Id; InstitutionId → Institutions.Id; LevelId → Levels.Id (restrict) | Unique filtered index on `RegistrationCode` where not null |
| InstitutionAdmins | Id (string, = AspNetUsers.Id) | Id → AspNetUsers.Id (cascade); InstitutionId → Institutions.Id (restrict) | |
| Supervisors | Id (string, own key) | AppUserId → AspNetUsers.Id; InstitutionId → Institutions.Id (restrict) | |
| Institutions | Id (string) | InstitutionAdminId → InstitutionAdmins.Id (1:1, restrict) | Unique `Code` (business key, e.g. "SCH-12345") |
| Classes | Id (string) | InstitutionId → Institutions.Id | |
| ClassSupervisors | (ClassId, SupervisorId) composite PK | ClassId → Classes.Id; SupervisorId → Supervisors.Id | Many-to-many join table |
| Adventures | Id (string) | InstitutionId → Institutions.Id | |
| AdventureTasks | Id (string) | AdventureId → Adventures.Id; TaskTemplateId → TaskTemplates.Id | |
| WeeklyAdventures | Id (string) | ClassId → Classes.Id; AdventureId → Adventures.Id | |
| ChildAdventureProgresses | Id (string) | ChildId → Childrens.Id; WeeklyAdventureId → WeeklyAdventures.Id | |
| ChildAdventureTasks | Id (string) | ChildId → Childrens.Id; AdventureTaskId → AdventureTasks.Id; WeeklyAdventureId → WeeklyAdventures.Id; ChildAdventureProgressId → ChildAdventureProgresses.Id (nullable) | |
| TaskCategories | Id (string) | — | IconUrl/IconPublicId nullable (post-initial migration fix) |
| SubCategories | Id (string) | CategoryId → TaskCategories.Id | |
| TaskTemplates | Id (string) | SubCategoryId → SubCategories.Id (restrict, nullable) | Single flat table for all 4 template types, discriminated by `TemplateType` string column |
| ChildTasks | Id (string) | ChildId → Childrens.Id; TaskTemplateId → TaskTemplates.Id | |
| PointsTransactions | Id (string) | ChildId → Childrens.Id | `Points` may be negative (spend) |
| Levels | Id (string) | — | `Order` intended unique (enforced at service layer) |
| Gifts | Id (string) | — | |
| ChildGifts | Id (string) | ChildId → Childrens.Id; GiftId → Gifts.Id | |
| Rewards | Id (string) | ParentId → AspNetUsers.Id; ChildId → Childrens.Id | |
| Notifications | Id (string) | UserId → AspNetUsers.Id | Does not inherit the audit base class |
| UserRefreshTokens | Id (Guid) | UserId → AspNetUsers.Id | |

**Delete-behavior summary (as explicitly configured in `OnModelCreating`):**

| Relationship | Delete behavior | Rationale |
|---|---|---|
| AppUser → Parent / Child / InstitutionAdmin | Cascade | These rows have no independent existence without their identity row |
| Parent → ActiveChild | Restrict | Prevents silent loss of the "active child" pointer |
| Child → Parent (owning AppUser) | Restrict | Preserves historical child records even if a parent account is removed |
| Institution → InstitutionAdmin | Restrict | An institution cannot be deleted while it still owns an administrator record |
| Institution → Supervisor | Restrict | Preserves supervisor records/institution history |
| TaskSubCategory → TaskTemplateBase | Restrict | Prevents orphaning task templates already used across the platform |
| Level → Child | Restrict | Prevents deleting a Level while children are currently assigned to it (also enforced defensively in `LevelService.Delete`) |

**Suggested diagram — "Physical ERD" (Draw.io/Visio recreation guidance):**

Use standard crow's-foot ERD notation with each table rendered as a rectangle listing: PK column first (underlined), then FK columns (marked "FK"), then remaining columns with SQL types (`nvarchar`, `int`, `bit`, `datetime2`, `decimal` as applicable — exact column types should be taken from the migration `Up()` methods in `Migrations/*_inital.cs` if a fully precise physical diagram is required, since this analysis reports the FK/PK/constraint structure but a reader producing the literal diagram should confirm exact SQL column types against that file for full fidelity). Group tables spatially exactly as the four logical clusters in Section 3.2, and additionally render the `IsDeleted`/`CreatedAt`/`CreatedBy`/`UpdatedAt`/`UpdatedBy` audit columns as a small shared "mixin" note attached to each auditable table rather than repeating five columns visually on every entity, to keep the diagram legible. Composite-key tables (`ClassSupervisors`) should have both PK columns underlined together with a bracket denoting the composite key.

**Note on completeness:** exact SQL data types, lengths, and default constraints (e.g., precise `nvarchar` length limits, `datetime2` precision) are defined in the migration `Up()` methods and EF model snapshot but were not individually transcribed column-by-column in this analysis; if the physical ERD is required to show literal SQL types for grading purposes, the migration files (`Migrations/20260630045142_inital.Designer.cs` and `AppDbContextModelSnapshot.cs`) should be consulted directly, as they are the authoritative source and no type should be guessed here.

---

## Chapter 6 — Conclusion and Future Work

### 6.1 Conclusion

This project set out to design and implement a backend platform capable of unifying three otherwise disconnected concerns in children's habit and educational-activity management: parental oversight, institutional structured activity, and game-based motivation. The resulting system, GoKidAPI, demonstrates that these concerns can be reconciled under a single, coherent data model without collapsing their distinct review and workflow semantics into one another — parent-assigned tasks and institution-driven Adventures remain structurally and operationally distinct subsystems, yet both terminate in the same points-and-levels engine, giving the child a single, continuous sense of progress regardless of which context produced it.

Architecturally, the project validates a layered, service-oriented monolith as a suitable choice for a domain of this relational density: the Institution → Class → Child → Adventure → Task graph benefits considerably from being governed by one `DbContext` capable of enforcing restrict-on-delete integrity and soft-deletion consistently, a guarantee that would be materially harder to maintain across independently-deployed services. The unified-identity-with-satellite-profile pattern (`AppUser` plus `Parent`/`Child`/`Supervisor`/`InstitutionAdmin`) proved to be an effective way to support five different actor types and two different login mechanisms (password-based for adults, registration-code-based for children) without duplicating authentication infrastructure per role.

The points-and-levels subsystem — a transaction ledger (`PointsTransaction`) feeding a spend-independent ranking metric (`HighestPoints`) and a strictly non-regressive level (`Level`) — represents the project's most deliberate design decision, and directly addresses the motivating concern that gamification mechanics should be economically consistent rather than cosmetic. Equally, the dual-channel notification architecture (persisted history, plus role-appropriate delivery via Firebase push or SignalR) reflects a considered response to the reality that mobile actors and web-dashboard actors require fundamentally different real-time delivery mechanisms.

At the same time, the analysis surfaced concrete gaps that should be transparently acknowledged in any academic evaluation of the system: several controllers currently rely on commented-out or absent role-based authorization attributes, delegating access control entirely to service-layer claim inspection — a pattern that is harder to audit than declarative `[Authorize]` attributes and should be remediated before any production deployment. Additionally, the soft-delete query-filter convention is inconsistently applied (present on some auditable entities, manually replicated in service queries for others), which is a maintainability risk worth resolving.

### 6.2 Future Work

The following directions are proposed as natural extensions of the current implementation, each grounded in a specific gap or opportunity identified during the source-code analysis:

1. **Authorization hardening.** Systematically re-enable and verify the commented-out `[Authorize(Roles=...)]` attributes across `TaskTemplateController`, `ClassController`, `CategoryController`, `TaskSubCategoryController`, `ChildController`, `ChildAdventureController`, `ParentTaskController`, `RewardsController`, and `InstitutionSupervisorController`, and add integration tests asserting that each endpoint rejects requests from unauthorized roles. This is the single highest-priority item, since it directly affects the system's production-readiness.
2. **Consistent global soft-delete enforcement.** Extend EF Core's global query filter (`HasQueryFilter`) to every entity inheriting `AuditableEntity`, removing the current split between globally-filtered entities (`Parent`, `Child`, `TaskCategory`, `TaskTemplateBase`, `ChildTask`, `PointsTransaction`, `Level`) and manually-filtered ones (`Institution`, `Supervisor`, `InstitutionAdmin`, `Gift`, `Reward`, `ChildGift`, `Adventure`, `Class`), eliminating a class of bugs where a service forgets to apply the manual filter.
3. **Multi-child parent support.** The domain model already supports a parent owning multiple `Child` records (`Child.ParentId` is a 1:* relationship), but the current product experience is constrained to a single `ActiveChildId` per parent. A natural extension is to expose full multi-child management (switching between children, aggregated multi-child dashboards) at the API and client level.
4. **Formal automated testing.** No unit or integration test project was identified during this analysis. Given the complexity of the points/level/notification convergence logic (`PointsService`, `LevelProgressionService`, `SupervisorService.ReviewChildTaskAsync`), this logic is a high-value candidate for unit testing, and the multi-role authorization surface is a high-value candidate for integration testing.
5. **Formal API documentation and versioning.** Introducing OpenAPI/Swagger annotations consistently across all controllers (several DTOs and endpoints currently rely on implicit conventions) and adopting an explicit API versioning strategy would materially improve maintainability as client applications evolve independently of the backend.
6. **Observability.** Structured logging via Serilog is present, but no centralized metrics or distributed tracing was found. Given the platform's caching layers (dashboard aggregation) and multiple external integrations (Firebase, Cloudinary, TTS/AI services), instrumenting request latency, cache hit-rate, and third-party call failure rates would materially aid operational visibility.
7. **Infrastructure and deployment documentation.** As noted in Section 1.8, this analysis was limited to the backend source code; a follow-up effort should document the actual hosting/deployment architecture (the presence of `PublishProfiles/site46121-WebDeploy.pubxml` suggests an Azure/IIS Web Deploy target) so that a complete deployment-architecture diagram can be produced with confidence rather than omitted.
8. **Analytics depth.** The existing dashboard services already aggregate a substantial range of metrics; a future iteration could extend `StatisticsService` and the dashboard layer toward predictive indicators (e.g., early identification of disengaged children based on declining task-completion trend) rather than purely retrospective reporting.

---

*End of document. Chapters 2, 4, and 5 (Literature Review / Related Work, Detailed Design, Implementation & Testing) were not requested in this pass and are not included.*
