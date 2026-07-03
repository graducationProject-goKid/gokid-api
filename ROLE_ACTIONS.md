# GoKid — Role Actions Report

> Implementation-based analysis of what each role actually does in the system.

---

## Table of Contents

- [PlatformAdmin](#platformadmin)
- [InstitutionAdmin](#institutionadmin)
- [InstitutionSupervisor](#institutionsupervisor)
- [Parent](#parent)
- [Child](#child)
- [How the Roles Connect](#how-the-roles-connect)
- [Points Flow Summary](#points-flow-summary)

---

## PlatformAdmin

The system operator. Owns the global content library (gifts and task templates) that every institution and every child in the platform shares.

### Gift Management

| Action | Details |
|---|---|
| Create a gift | Uploads gift image to Cloudinary, sets a `PointsCost` and metadata, makes it available to all children globally |
| Update a gift | Edits title, description, image, or `PointsCost` |
| Delete a gift | Soft delete only; existing child purchases are not affected |
| Toggle gift status | Inactive gifts disappear from the child's browsable catalog immediately |
| View all gifts | Paginated full catalog view across the platform |

### Task Template & Category Management

The PlatformAdmin is the **sole owner** of the shared task library that Parents and InstitutionAdmins consume.

**Task Templates — four types:**

| Type | How it works |
|---|---|
| **InstantReward** | Child completes the task and points are awarded immediately — no review step |
| **TextQuestion** | Child types a text answer; auto-verified by the system |
| **VoiceQuestion** | Child records a voice answer; graded in real-time by an external AI API (Faster Whisper); word-level feedback returned |
| **EvidenceSubmission** | Child uploads a photo or file; placed in `ReviewRequested` status; requires human review by parent or supervisor before points are awarded |

| Action | Details |
|---|---|
| Create a task template | Defines type, title (Ar/En), description, base points, difficulty, icon, max voice attempts (for VoiceQuestion type) |
| Update a task template | Edits any template field |
| Delete a task template | Soft delete only |
| Create / Update / Delete categories | Top-level groupings for organizing templates |
| Create / Update / Delete subcategories | Second-level groupings under categories; used by parents when browsing templates to assign |

### Cross-Role Effects

- Every gift the PlatformAdmin creates or activates is immediately purchasable by any **Child** with enough points, regardless of institution.
- Task templates created by PlatformAdmin are the shared library that **Parents** pull from when assigning tasks, and that **InstitutionAdmins** pull from when building adventure task lists.
- Categories and subcategories are the organizational structure visible to parents and admins when browsing templates.

---

## InstitutionAdmin

Owns everything inside a single institution — staff, classes, children, and adventure content. Consumes the PlatformAdmin's template library to build adventures.

### Account

| Action | Details |
|---|---|
| Login / Logout | Standard email + password auth |
| Change password | Via current password verification |
| Forgot / Reset password | OTP sent to email |
| Update FCM token | Enables push notifications on device |

### Supervisor Management

| Action | Details |
|---|---|
| Create a supervisor account | Creates `AppUser` + `Supervisor` entity, links to institution, assigns "Supervisor" role; supervisor can immediately log in |
| View all supervisors | Paginated list with search by name/email; includes count of supervised classes per supervisor |

### Class Management

| Action | Details |
|---|---|
| Create a class | Adds a class to the institution |
| Update a class | Edits class name or details |
| Delete a class | Soft delete only |
| View class details | Includes counts: children, supervisors, active adventures |
| View all classes | Paginated with search and sorting |
| Assign supervisor to class | Creates a `ClassSupervisor` join row; validates supervisor belongs to same institution |
| Remove supervisor from class | Soft-deletes the `ClassSupervisor` row |

### Child Enrollment

| Action | Details |
|---|---|
| Enroll child into institution | Looks up child by 6-character registration code; sets `Child.InstitutionId`; prevents double-enrollment |
| Enroll child into class | Child must already be in the institution; sets `Child.ClassId`; a child can be in only one class at a time — **this is the moment the child becomes eligible for the class's adventures** |
| Remove child from institution | Nullifies both `InstitutionId` and `ClassId` |
| Remove child from class only | Nullifies `ClassId` only; child stays in institution |
| View all institution children | Paginated list with name, points, avatar, class name; filterable by class or search term |

### Adventure Management

| Action | Details |
|---|---|
| Create an adventure | Defines title, description, banner image, bonus points, and duration. For each day/task: links a `TaskTemplate` (from PlatformAdmin's library), assigns a day number, story text, and optionally a voice file. If no voice file provided, a **background TTS job** (Hangfire) is queued to generate it automatically |
| Generate AI story | Calls an external LLM to write Intro, Outro, and per-day narrative. Queues a background TTS job to convert generated text to audio |
| Assign adventure to class | Creates a `WeeklyAdventure` row binding the adventure to a class with start/end dates. **Triggers a background job that auto-assigns Day-1 tasks to every child currently enrolled in that class** |
| Update an adventure | Edits title, description, voice files, bonus points |
| Toggle adventure status | Changing to Inactive cascades to all related `WeeklyAdventures` |
| Delete an adventure | Soft delete only |
| View all adventures | Paginated list for their institution |
| View class assignments | Shows which classes a weekly adventure has been assigned to |

### Notifications

Receives and manages their own notifications (standard notification endpoints, any authenticated user).

### Cross-Role Effects

- Assigning an adventure to a class → background job creates Day-1 `ChildAdventureTask` rows for every enrolled child, making tasks visible to them immediately.
- Enrolling a child into a class → child becomes eligible for the class's active adventures.
- Generating a story → all children in classes with that adventure see updated story content and new voice files once the background job completes.
- The admin uses task templates created by PlatformAdmin — they do not create templates themselves.

---

## InstitutionSupervisor

Assigned to one or more classes. Primary responsibility is reviewing and grading children's adventure task submissions. Every approval decision directly affects a child's points and ranking.

### Account

| Action | Details |
|---|---|
| Login / Logout | Standard email + password auth |
| Change password | Via current password |
| Forgot / Reset password | OTP sent to email |

### Dashboard (Read-only)

| Action | Details |
|---|---|
| View my adventures | Lists all active `WeeklyAdventures` for supervised classes, with count of submissions pending review |
| View my classes | Lists all assigned classes with children count and active adventure count |

### Task Review — Core Responsibility

| Action | Details |
|---|---|
| View child submissions | Paginated list of `ChildAdventureTasks` for a weekly adventure; filterable by status; includes child name, task name, evidence URL, review status |
| **Approve a submission** | Sets `Status = Completed`, `IsApproved = true`. Awards `template.BasePoints` to child (`TotalPoints` + `HighestPoints` updated). Sets `EarnedStars = 3`. Updates `ChildAdventureProgress` (+1 completed day, +stars, +points). Creates `PointsTransaction`. **If all tasks in the adventure are now complete → awards `BonusPoints`, sets `WeekBonusAwarded = true` and `IsCompleted = true`** |
| **Reject a submission** | Sets `Status = Pending` (child can resubmit). Clears `EvidenceUrl` to force a fresh upload. No points awarded |

### Progress Monitoring (Read-only)

| Action | Details |
|---|---|
| View children's progress in adventure | Per-child breakdown of completed/pending/missed tasks, earned stars, and earned points for a class; sorted by completed tasks descending |
| View a child's full adventure history | Per-task detail: day number, status, evidence URL, review outcome, earned stars, submission time |

### Notifications

Standard notification endpoints.

### Cross-Role Effects

- Every approval increases the child's `TotalPoints` and potentially `HighestPoints`, immediately affecting the child's position in global and institution rankings.
- Completing an adventure (all tasks approved) triggers the bonus points award — the largest single point event available to a child.
- Rejection resets the child's submission status to Pending and clears their evidence, giving them a second attempt.

---

## Parent

Creates and manages their child (MVP: one active child at a time). Assigns tasks from the PlatformAdmin's library, reviews evidence submissions, sets motivational rewards, and monitors child progress.

### Account & Child Setup

| Action | Details |
|---|---|
| Register | Creates `AppUser` + `Parent` entity; sends email OTP for verification |
| Verify OTP / Resend OTP | Activates the account |
| Login / Logout / Refresh token | Standard auth lifecycle |
| Change / Forgot / Reset password | Standard password lifecycle |
| Update FCM token | Enables push notifications on device |
| View own profile | Returns parent name, email, avatar, and active child summary (name, age, gender, points, class, institution) |
| **Create child account** | Validates no active child already exists (MVP constraint). Creates an `AppUser` for the child (no password, no email, synthetic username). Creates the `Child` profile row sharing the same ID. Generates a unique 6-character alphanumeric **registration code** using `RandomNumberGenerator`. Generates a **QR code** (Base64 PNG) encoding the code. Returns both — the parent shares them with the child to enable login, and with the InstitutionAdmin to enroll the child |

### Task Assignment & Review

| Action | Details |
|---|---|
| Assign a task to child | Selects a `TaskTemplate` from PlatformAdmin's library; optionally sets a due date. Prevents assigning the same template twice if still active. Creates `ChildTask` with `Source = Parent` |
| View all child tasks | Paginated list of all tasks (from all sources, not just parent-assigned); shows status, due date, evidence, and rejection reason |
| View task details | Full breakdown of a specific task |
| **Approve an evidence task** | Sets `Status = Completed`; awards `template.BasePoints` to child; creates `PointsTransaction` |
| **Reject an evidence task** | Sets `Status = Rejected`; stores rejection reason; child cannot resubmit a rejected parent task |

### Rewards Management

| Action | Details |
|---|---|
| Create a reward | Defines a motivational goal for the child (e.g. "Ice cream trip") |
| Delete a reward | Removes the reward |
| View all created rewards | Full list of rewards set for the child |
| Give a reward to child | Marks the reward as granted at a milestone |

### Monitoring (Read-only)

| Action | Details |
|---|---|
| View child's purchased gifts | Full list of gifts the child has bought with their points |
| View child statistics | Comprehensive stats for a selected period (completion rates, points earned, adventure progress) |
| View global ranking | Sees where their child stands against all children on the platform |
| View institution ranking | Sees where their child stands within the institution |

### Notifications

Standard notification endpoints.

### Cross-Role Effects

- Parent creating a child → generates the registration code that the **InstitutionAdmin** uses to enroll the child into an institution.
- Parent assigning a task → task appears immediately in the child's "Parent Assigned" tab.
- Parent approving an evidence task → child gains points → child's `HighestPoints` may update → ranking changes.

---

## Child

Logs in via the 6-character registration code (no email, no password). Completes tasks, participates in daily adventures, earns points, spends them on gifts, and tracks their progress on the leaderboard.

### Account

| Action | Details |
|---|---|
| Login | Uses the 6-character alphanumeric registration code; the system looks up `Child.RegistrationCode`, fetches the child's `AppUser`, and issues a standard JWT with the "Child" role |
| Refresh token / Logout | Standard token lifecycle; refresh tokens are fully persisted and invalidatable |
| View own profile | Name, nickname, age, gender, avatar, total points, class name, institution name |
| Update FCM token | Enables push notifications on device |

### Tasks (Parent-Assigned)

| Action | Details |
|---|---|
| View parent-assigned tasks | All `ChildTask` rows with `Source = Parent`; includes status, due date, evidence URL, rejection reason, and points value |
| **Submit — InstantReward** | Marked complete immediately; points awarded on the spot; no review step |
| **Submit — VoiceQuestion** | Uploads voice recording to Cloudinary → sent to AI API (Faster Whisper) → returns score (Excellent / Good / Fair / Poor) and word-level feedback. **Excellent or Good → points awarded immediately**. Poor → attempt counted; if max attempts reached → task rejected; otherwise → child can try again |
| **Submit — EvidenceSubmission** | Uploads a photo or file; status changes to `ReviewRequested`; **points are NOT awarded yet** — awaits parent approval |

### Adventures (Institution-Assigned)

Each adventure runs for a set number of days (weeks). A child can only access today's task — past days show as Missed if skipped, future days are Locked.

| Action | Details |
|---|---|
| View my adventures | Lists all weekly adventures for the child's class. Per adventure shows: current day, total days, per-day status (Locked / Unlocked / Completed / Missed), earned stars, earned points, completion status |
| View adventure details | Full task breakdown; locked tasks reveal no content. Unlocked/past tasks show story text, story voice narration, and submission status |
| View adventure tasks | Same per-day status list, task-focused view |
| **Submit — InstantReward** | Instant points via `PointsService`; no review |
| **Submit — VoiceQuestion** | AI grading same as above; instant points if Excellent or Good |
| **Submit — EvidenceSubmission** | Uploads evidence; status = `Completed` with `IsApproved = null`; **awaits supervisor review** before points are awarded |

> **Day locking rule:** A child can only submit the task for the current day. Submitting on a future day or a past day is rejected by the service.

### Points & Gifts

| Action | Details |
|---|---|
| View current points | `child.TotalPoints` — the spendable balance; decreases when gifts are purchased |
| Browse available gifts | Active platform gifts (PlatformAdmin-created) the child hasn't already purchased; sorted by `PointsCost` ascending |
| **Purchase a gift** | Validates `TotalPoints >= PointsCost`. Deducts from `TotalPoints` only. **`HighestPoints` is never decreased** — preserving ranking position. Creates `ChildGift` row and a negative `PointsTransaction` |
| View my gifts | All gifts the child has purchased |
| View my rewards | Rewards given to the child by their parent |

### Progress & Rankings (Read-only)

| Action | Details |
|---|---|
| View personal statistics | Task completion rates, points history, adventure progress for a selected period |
| View global ranking | Leaderboard of all children ordered by `HighestPoints`; includes own rank even if outside the top displayed results |
| View institution ranking | Same leaderboard filtered to children in the same institution |

### Notifications

Standard notification endpoints.

### Cross-Role Effects

- Completing an InstantReward or passing a VoiceQuestion → points awarded immediately → `HighestPoints` updated if exceeded → ranking changes in real time.
- Submitting an EvidenceSubmission → **Parent** or **Supervisor** sees a pending review item.
- Completing all tasks in an adventure → supervisor's final approval triggers the adventure bonus points.
- Buying a gift → `TotalPoints` drops but `HighestPoints` and ranking are unaffected.

---

## How the Roles Connect

```
PLATFORMADMIN
├── Creates gifts            → available to all Children globally
├── Creates task templates   → used by Parents to assign tasks
│                            → used by InstitutionAdmin to build adventures
└── Creates categories       → organizational structure for templates

INSTITUTIONADMIN
├── Creates supervisors      → assigns them to classes
├── Enrolls children (by registration code) → assigns them to classes
│         ↓
│   Child becomes eligible for class adventures
│
├── Creates adventures (using PlatformAdmin templates)
└── Assigns adventures to classes
         ↓ (background job auto-assigns Day-1 tasks to all enrolled children)

INSTITUTIONSUPERVISOR
└── Reviews child adventure task submissions
         ↓
    Approve → child gains points + stars + adventure progress
    Reject  → child resubmits (evidence cleared)
    All tasks complete → adventure bonus points awarded

PARENT
├── Registers + creates child → generates login code (6 chars) + QR code
│         ↓ (parent shares code with InstitutionAdmin to enroll child)
│         ↓ (parent gives code/QR to child to log in)
│
├── Assigns tasks to child (from PlatformAdmin template library)
└── Reviews child evidence submissions
         ↓
    Approve → child gains points
    Reject  → task marked rejected, reason stored

CHILD
├── Logs in with 6-char registration code
├── Does parent-assigned tasks (instant / voice / evidence)
├── Participates in institution adventures (day-by-day, locked until each day arrives)
├── Earns points → TotalPoints + HighestPoints (ranking metric, never decreases)
└── Spends TotalPoints on PlatformAdmin gifts
```

---

## Points Flow Summary

| Points Source | Who triggers it | Who awards it | When |
|---|---|---|---|
| InstantReward task | Child submits | Automatic | Immediately on submission |
| VoiceQuestion task (Excellent / Good) | Child submits | Automatic (AI grade) | Immediately on submission |
| EvidenceSubmission — parent task | Child submits → Parent approves | Parent approval | On parent review |
| EvidenceSubmission — adventure task | Child submits → Supervisor approves | Supervisor approval | On supervisor review |
| Adventure completion bonus | All tasks in adventure approved | Automatic on final approval | When last task is approved |
| Gift purchase | Child buys | Negative (deduction from TotalPoints) | Immediately on purchase |

> **Key invariant:** `HighestPoints` only ever increases. Gift purchases reduce `TotalPoints` but never `HighestPoints`. The global and institution rankings are always based on `HighestPoints`, so spending on gifts never hurts a child's rank.
