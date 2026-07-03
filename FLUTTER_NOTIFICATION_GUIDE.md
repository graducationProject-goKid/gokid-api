# GoKid — Flutter Push Notification Integration Guide

> **Audience:** Flutter developer integrating backend business notifications into the mobile app.
> **Backend status:** Complete. Treat everything in this document as the source of truth.
> **Scope:** Parent and Child roles only. PlatformAdmin, InstitutionAdmin, and Supervisor receive notifications through the web dashboard (SignalR) and are not relevant to the Flutter app.

---

## Table of Contents

1. [Delivery Model](#1-delivery-model)
2. [FCM Payload Structure](#2-fcm-payload-structure)
3. [Notification Types Reference](#3-notification-types-reference)
4. [Handling Notification Taps](#4-handling-notification-taps)
5. [API Calls After Opening a Notification](#5-api-calls-after-opening-a-notification)
6. [FCM Token Registration](#6-fcm-token-registration)
7. [Edge Cases](#7-edge-cases)
8. [Checklist for Existing Flutter Notification Code](#8-checklist-for-existing-flutter-notification-code)

---

## 1. Delivery Model

The backend sends all Parent and Child notifications exclusively via **Firebase Cloud Messaging (FCM)**.

- SignalR is not used for mobile. Do not connect Parent or Child users to the notification SignalR hub.
- All three FCM app states are covered by the standard Firebase Messaging setup:

| App state | Firebase event |
|---|---|
| Foreground | `FirebaseMessaging.onMessage` |
| Background | `FirebaseMessaging.onBackgroundMessage` |
| Terminated | Launch from notification tap — read via `getInitialMessage()` |

Every notification is also persisted to the backend database regardless of delivery channel, so the in-app notification list (`GET /api/notifications`) always reflects all notifications the user has ever received.

---

## 2. FCM Payload Structure

Every notification the backend sends to a mobile user follows this exact structure. All fields are always present.

```json
{
  "notification": {
    "title": "Task Approved!",
    "body": "Your parent approved \"Brush Teeth\". You earned 20 points!"
  },
  "data": {
    "type": "TaskApproved",
    "notificationId": "3f2a1b4c-..."
  }
}
```

### Field definitions

| Field | Type | Description |
|---|---|---|
| `notification.title` | `String` | Human-readable notification title. Safe to show directly in the UI. |
| `notification.body` | `String` | Human-readable notification body. Safe to show directly in the UI. |
| `data.type` | `String` | The notification type identifier. Use this to determine what action to take. See [Section 3](#3-notification-types-reference) for all possible values. |
| `data.notificationId` | `String (UUID)` | The ID of the persisted `Notification` record in the backend database. Use this to call `PATCH /api/notifications/{notificationId}/read` after the user opens the notification. |

---

## 3. Notification Types Reference

### Parent notifications

---

#### `ReviewRequested`

| Field | Value |
|---|---|
| **When sent** | The Child submits photo/file evidence for a parent-assigned task |
| **Recipient** | Parent |
| **Example title** | `"New Submission"` |
| **Example body** | `"Youssef submitted evidence for \"Brush Teeth\"."` |

**What the Flutter app should do:**
- Navigate to the parent task list screen and refresh it so the new `ReviewRequested` status is visible.
- Call `PATCH /api/notifications/{notificationId}/read` after tap.

**Endpoint to use after tap:**
```
GET /api/parenttask          (refresh task list)
PATCH /api/notifications/{notificationId}/read
```

---

#### `GiftPurchased`

| Field | Value |
|---|---|
| **When sent** | The Child buys a gift from the platform gift catalog |
| **Recipient** | Parent |
| **Example title** | `"Gift Purchased"` |
| **Example body** | `"Youssef used 50 points to get \"Toy Car\"."` |

**What the Flutter app should do:**
- Navigate to the parent's "My Child Gifts" screen and refresh it.
- Call `PATCH /api/notifications/{notificationId}/read` after tap.

**Endpoint to use after tap:**
```
GET /api/gifts/my-child-gifts
PATCH /api/notifications/{notificationId}/read
```

---

### Child notifications

---

#### `TaskAssigned`

| Field | Value |
|---|---|
| **When sent** | The Parent assigns a task template to the child |
| **Recipient** | Child |
| **Example title** | `"New Task!"` |
| **Example body** | `"Your parent assigned you \"Brush Teeth\"."` |

**What the Flutter app should do:**
- Navigate to the child's task list screen and refresh it so the new task appears.
- Call `PATCH /api/notifications/{notificationId}/read` after tap.

**Endpoint to use after tap:**
```
GET /api/child              (refresh task list)
PATCH /api/notifications/{notificationId}/read
```

---

#### `TaskApproved`

Sent for both parent-approved tasks and supervisor-approved adventure tasks. Distinguish the context from the notification **body text** — parent approvals mention the task name, supervisor approvals mention the adventure name and day number.

| Field | Value |
|---|---|
| **When sent** | Parent approves a child's evidence task **or** Supervisor approves a child's adventure task |
| **Recipient** | Child |
| **Example title (parent)** | `"Task Approved!"` |
| **Example body (parent)** | `"Your parent approved \"Brush Teeth\". You earned 20 points!"` |
| **Example title (supervisor)** | `"Adventure Task Approved!"` |
| **Example body (supervisor)** | `"Day 3 of \"The Dragon's Quest\" approved. You earned 30 points!"` |

**What the Flutter app should do:**
- Refresh the child's task list (`GET /api/child`) and points balance (`GET /api/child/points`).
- Show a celebration/confetti overlay.
- If the child is currently on the adventure screen, refresh it too.
- Call `PATCH /api/notifications/{notificationId}/read` after tap.

**Endpoint to use after tap:**
```
GET /api/child              (refresh task list)
GET /api/child/points       (refresh points)
PATCH /api/notifications/{notificationId}/read
```

---

#### `TaskRejected`

Sent for both parent-rejected tasks and supervisor-rejected adventure tasks. Distinguish the context from the notification **body text** — parent rejections include the rejection reason and task name, supervisor rejections mention the adventure name and day number.

| Field | Value |
|---|---|
| **When sent** | Parent rejects a child's evidence task **or** Supervisor rejects a child's adventure task |
| **Recipient** | Child |
| **Example title (parent)** | `"Task Needs Changes"` |
| **Example body (parent)** | `"Your parent reviewed \"Brush Teeth\": Please show the full page."` |
| **Example title (supervisor)** | `"Try Again!"` |
| **Example body (supervisor)** | `"Your supervisor sent back day 3 of \"The Dragon's Quest\". Re-upload your evidence."` |

**What the Flutter app should do:**
- Refresh the child's task list so updated statuses are visible.
- **Parent rejection:** Do not offer a resubmit option — the backend marks parent-rejected tasks as `Rejected` permanently. The task is closed.
- **Supervisor rejection:** The rejected adventure task day reverts to `Pending`. Refresh the adventure screen — the re-upload button should appear for that day.
- Call `PATCH /api/notifications/{notificationId}/read` after tap.

**Endpoint to use after tap:**
```
GET /api/child              (refresh task list)
GET /api/childadventure     (refresh adventure list, if supervisor context)
PATCH /api/notifications/{notificationId}/read
```

---

#### `RewardGiven`

| Field | Value |
|---|---|
| **When sent** | The Parent gives a reward to the child |
| **Recipient** | Child |
| **Example title** | `"You Got a Reward!"` |
| **Example body** | `"Your parent gave you: \"Ice Cream Trip\". Well done!"` |

**What the Flutter app should do:**
- Navigate to the child's rewards screen and refresh it.
- Show a celebration overlay if the design calls for it.
- Call `PATCH /api/notifications/{notificationId}/read` after tap.

**Endpoint to use after tap:**
```
GET /api/rewards/my-rewards
PATCH /api/notifications/{notificationId}/read
```

---

#### `AdventureStarted`

| Field | Value |
|---|---|
| **When sent** | An InstitutionAdmin assigns an adventure to the child's class. Day 1 is immediately unlocked. |
| **Recipient** | All children enrolled in that class |
| **Example title** | `"New Adventure!"` |
| **Example body** | `"\"The Dragon's Quest\" has started. Day 1 is ready!"` |

**What the Flutter app should do:**
- Navigate to the child's adventure list screen and refresh it so the new adventure appears.
- Call `PATCH /api/notifications/{notificationId}/read` after tap.

**Endpoint to use after tap:**
```
GET /api/childadventure
PATCH /api/notifications/{notificationId}/read
```

---

#### `AdventureNewDay`

| Field | Value |
|---|---|
| **When sent** | A new day unlocks in an ongoing adventure (runs daily at midnight UTC, Day 2 onwards) |
| **Recipient** | All children enrolled in that class |
| **Example title** | `"Day 4 Unlocked!"` |
| **Example body** | `"Day 4 of \"The Dragon's Quest\" is ready. Don't miss it!"` |

**What the Flutter app should do:**
- Navigate to the adventure list screen and refresh it. The adventure screen computes today's unlocked day from the backend response — no client-side date logic needed.
- Call `PATCH /api/notifications/{notificationId}/read` after tap.

**Endpoint to use after tap:**
```
GET /api/childadventure      (refresh adventure list; day lock state is computed server-side)
PATCH /api/notifications/{notificationId}/read
```

**Note:** This notification arrives at midnight UTC. If the device was offline, it may be delivered hours later. Always rely on the backend response for day lock state — never use the notification's delivery timestamp to infer which day is unlocked.

---

#### `WeekBonus`

| Field | Value |
|---|---|
| **When sent** | The child completes all tasks in an adventure (triggered when a supervisor approves the last remaining task) |
| **Recipient** | Child |
| **Example title** | `"Adventure Complete!"` |
| **Example body** | `"You finished \"The Dragon's Quest\"! Bonus: +100 points. Amazing!"` |

**What the Flutter app should do:**
- Navigate to the adventure list screen, refresh it, and show the adventure in its completed state.
- Show a celebration/confetti overlay.
- Refresh the child's points balance (`GET /api/child/points`).
- Call `PATCH /api/notifications/{notificationId}/read` after tap.

**Endpoint to use after tap:**
```
GET /api/childadventure
GET /api/child/points
PATCH /api/notifications/{notificationId}/read
```

**Note:** On the final task approval, the child receives two notifications in quick succession: `TaskApproved` (for the last task) and `WeekBonus` (for the adventure completion). Handle each independently — do not deduplicate or suppress either.

---

### Complete type reference

| `type` | Recipient | Navigation target |
|---|---|---|
| `ReviewRequested` | Parent | Parent task list (refresh) |
| `GiftPurchased` | Parent | Child gifts list (refresh) |
| `TaskAssigned` | Child | Child task list (refresh) |
| `TaskApproved` | Child | Task list + points refresh. Adventure screen refresh if supervisor context (read body text to infer). |
| `TaskRejected` | Child | Task list refresh. Adventure screen refresh if supervisor context (read body text to infer). |
| `RewardGiven` | Child | Rewards screen (refresh) |
| `AdventureStarted` | Child | Adventure list (refresh) |
| `AdventureNewDay` | Child | Adventure list (refresh) |
| `WeekBonus` | Child | Adventure list + points refresh |

---

## 4. Handling Notification Taps

### General handler structure

The payload contains only `type` and `notificationId`. Navigation is based on `type` alone — no entity ID resolution needed.

```dart
void handleNotificationTap(RemoteMessage message) {
  final type = message.data['type'] as String?;
  final notificationId = message.data['notificationId'] as String?;

  if (type == null) return;

  switch (type) {
    // Parent
    case 'ReviewRequested':
      navigateToParentTaskList();       // refresh task list
      break;
    case 'GiftPurchased':
      navigateToChildGiftList();        // refresh gift list
      break;

    // Child
    case 'TaskAssigned':
      navigateToChildTaskList();        // refresh task list
      break;
    case 'TaskApproved':
      navigateToChildTaskList();        // refresh task list + points
      refreshChildPoints();
      break;
    case 'TaskRejected':
      navigateToChildTaskList();        // refresh task list
      break;
    case 'RewardGiven':
      navigateToChildRewards();         // refresh rewards
      break;
    case 'AdventureStarted':
    case 'AdventureNewDay':
      navigateToAdventureList();        // refresh adventure list
      break;
    case 'WeekBonus':
      navigateToAdventureList();        // refresh adventure list + points
      refreshChildPoints();
      showCompletionOverlay();
      break;

    default:
      navigateToNotificationList();     // fallback for unknown future types
      break;
  }

  if (notificationId != null) {
    markNotificationAsRead(notificationId);
  }

  refreshUnreadCount();
}
```

Register this handler in all three app states:

```dart
// Foreground — show banner, refresh badge (do NOT navigate)
FirebaseMessaging.onMessage.listen((message) {
  showInAppNotificationBanner(message);
  refreshUnreadCount();
});

// Background tap — navigate
FirebaseMessaging.onMessageOpenedApp.listen((message) {
  handleNotificationTap(message);
});

// Terminated tap — navigate after app init
final initial = await FirebaseMessaging.instance.getInitialMessage();
if (initial != null) {
  handleNotificationTap(initial);
}
```

### `TaskApproved` and `TaskRejected` — context inference

Since there is no `entityId`, distinguish parent vs. supervisor context from the **notification body text** if the UI needs to react differently:

- Body contains `"Your parent"` → parent-assigned task context → refresh task list.
- Body contains `"Adventure Task"` or `"supervisor"` → adventure context → also refresh the adventure list.

For tap handling, navigating to the task list screen and refreshing it is sufficient in both cases. The backend task status (`Completed` vs `Rejected`) is returned by the list endpoint and drives the UI state.

> **Parent rejection vs supervisor rejection:** Parent-rejected tasks (`TaskRejected` with body mentioning `"Your parent"`) are permanently closed — do not show a resubmit button. Supervisor-rejected adventure tasks (`TaskRejected` with body mentioning `"supervisor"`) revert to `Pending` — the resubmit button should appear when the adventure screen refreshes.

---

## 5. API Calls After Opening a Notification

### Mark as read

Call this immediately after the user taps a notification and navigation begins. Do not wait for the screen to load.

```
PATCH /api/notifications/{notificationId}/read
Authorization: Bearer {token}
```

Response: `200 OK`

### Refresh unread count

Call this after any of the following events:
- App resumes from background
- A foreground FCM message arrives
- After marking a notification as read
- After marking all notifications as read

```
GET /api/notifications/unread-count
Authorization: Bearer {token}
```

Response:
```json
{
  "data": 3,
  "message": "Unread count retrieved successfully."
}
```

Use the `data` field to update the notification badge on the navigation bar or app icon.

### Refresh points balance

Call after any notification that awards points to the child (`TaskApproved`, `WeekBonus`):

```
GET /api/child/points
Authorization: Bearer {token}
```

---

## 6. FCM Token Registration

The backend silently skips Firebase delivery if no FCM token is stored for the user. Register the token immediately after login and whenever it refreshes.

### On login

After a successful login response, register the token:

```dart
final token = await FirebaseMessaging.instance.getToken();
if (token != null) {
  await api.updateFcmToken(token);
}
```

```
PUT /api/account/fcm-token
Authorization: Bearer {token}
Content-Type: application/json

{
  "fcmToken": "fVH8n..."
}
```

### On token refresh

```dart
FirebaseMessaging.instance.onTokenRefresh.listen((newToken) {
  api.updateFcmToken(newToken);
});
```

Register this listener at app startup, before login. A stale token causes push notifications to be silently dropped by Firebase without any backend error.

---

## 7. Edge Cases

### Unknown `type` value
Guard against future notification types that the current app version does not yet handle. The `default` branch in the switch statement should route to the notification list:

```dart
default:
  navigateToNotificationList();
  break;
```

### Two notifications for the same event (`TaskApproved` + `WeekBonus`)
When a Supervisor approves the child's final adventure task, the backend sends `TaskApproved` and `WeekBonus` in quick succession. Both arrive as separate FCM messages. Handle each independently — do not deduplicate or suppress either. The `WeekBonus` handler is where the completion overlay and points refresh should fire.

### `AdventureNewDay` arrives at midnight UTC
This notification is sent by a backend scheduled job at midnight UTC. If the device was offline, it may be delivered hours later. Always rely on the backend response for day lock state — never infer the current adventure day from the notification's delivery timestamp or the device clock.

### Notification received while user is already on the target screen
If the user is already on the adventure or task screen when a foreground notification arrives, refresh the screen content silently — do not navigate. The foreground `onMessage` handler is the right place for silent refreshes. Only the background/terminated tap handler should trigger navigation.

### User not logged in
If a notification tap is handled in the terminated state and the session has expired, redirect to the login screen first and store the pending `type` and `notificationId` to resume after login.

### FCM token is null at login
On first install, `getToken()` may return `null` if notification permissions have not been granted. Request permission before calling `getToken()`, and register the token only after the user grants permission:

```dart
final settings = await FirebaseMessaging.instance.requestPermission();
if (settings.authorizationStatus == AuthorizationStatus.authorized) {
  final token = await FirebaseMessaging.instance.getToken();
  if (token != null) await api.updateFcmToken(token);
}
```

---

## 8. Checklist for Existing Flutter Notification Code

Review the existing Flutter notification implementation against the following items. Update any that do not yet match the backend behavior described in this guide.

- [ ] **`onMessage` handler** — registered at app startup. Refreshes unread count badge and optionally shows an in-app banner. Does NOT navigate.
- [ ] **`onMessageOpenedApp` handler** — registered at app startup. Calls `handleNotificationTap` for background-to-foreground taps.
- [ ] **`getInitialMessage()` check** — called once at startup after `Firebase.initializeApp()` to handle terminated-state taps.
- [ ] **FCM token registration on login** — token is fetched and sent to `PUT /api/account/fcm-token` after every successful login.
- [ ] **FCM token refresh listener** — `onTokenRefresh` registered at startup to keep the backend token current.
- [ ] **`type` switch statement** — handles all 9 type values (`ReviewRequested`, `GiftPurchased`, `TaskAssigned`, `TaskApproved`, `TaskRejected`, `RewardGiven`, `AdventureStarted`, `AdventureNewDay`, `WeekBonus`) plus a `default` fallback to the notification list.
- [ ] **Navigation uses `type` only** — no code references `entityId` or attempts to resolve an entity before navigating. Navigation targets are screens (lists), not individual entity detail views.
- [ ] **`notificationId` mark-as-read call** — `PATCH /api/notifications/{notificationId}/read` called on every notification tap.
- [ ] **Unread count refresh** — `GET /api/notifications/unread-count` called on app resume and after any notification interaction.
- [ ] **Points balance refresh** — `GET /api/child/points` refreshed after `TaskApproved` and `WeekBonus` notifications.
- [ ] **Unknown `type` fallback** — unrecognised `type` values route to the notification list rather than crashing or silently doing nothing.
- [ ] **Dual notification handling** — `TaskApproved` and `WeekBonus` on final adventure task approval are handled independently, not deduplicated.
- [ ] **No SignalR connection for Parent/Child** — the app does not open a SignalR hub connection for Parent or Child users. If an existing SignalR integration exists for notifications, it should be removed or gated to web-only roles.
- [ ] **`entityId` removed from any existing payload parsing code** — if the app previously read `message.data['entityId']`, that code should be removed or it will silently return `null` (which is safe but should be cleaned up).
