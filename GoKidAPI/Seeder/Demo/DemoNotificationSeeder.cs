using GoKidAPI.Data;
using GoKidAPI.Entity;
using GoKidAPI.Enums;
using GoKidAPI.Enums.Adventures;

using Microsoft.EntityFrameworkCore;

using TaskStatus = GoKidAPI.Enums.Tasks.TaskStatus;

namespace GoKidAPI.Seeder.Demo
{
    // Generates Notification rows correlated to the events created by the other Demo* seeders
    // (task review outcomes, adventure milestones, gift redemptions, level-ups, class enrollment),
    // timestamped to match the underlying event and with a realistic read/unread mix.
    public static class DemoNotificationSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            if (await AlreadySeeded(context)) return;

            // Marker row so re-runs can cheaply detect that demo notifications already exist
            // (Notification has no CreatedBy/audit field to tag like the other Demo* entities).
            context.Notifications.Add(new Notification
            {
                UserId = (await context.AppUsers.Select(u => u.Id).FirstAsync()),
                Type = NotificationType.PointsEarned,
                Title = "__DemoSeederMarker__",
                Body = "Internal marker row used to detect demo notification seeding.",
                CreatedAt = DateTime.UtcNow,
                IsRead = true
            });
            await context.SaveChangesAsync();

            var now = DateTime.UtcNow;
            var rng = DemoDataContext.Rng;
            var notifications = new List<Notification>();

            // 1) Task review outcomes -> parent
            var recentCutoff = now.AddDays(-6);
            var taskEvents = await context.ChildTasks.IgnoreQueryFilters()
                .Where(ct => ct.CreatedBy == "DemoSeeder" &&
                    (ct.Status == TaskStatus.Completed ||
                     ct.Status == TaskStatus.Rejected ||
                     (ct.Status == TaskStatus.ReviewRequested && ct.ReviewRequestedAt >= recentCutoff)))
                .Select(ct => new
                {
                    ct.Status,
                    ct.ApprovedAt,
                    ct.CompletedAt,
                    ct.ReviewRequestedAt,
                    TemplateTitle = ct.Template.TitleEn,
                    ParentId = ct.Child.ParentId,
                    ChildName = ct.Child.Name
                })
                .ToListAsync();

            foreach (var e in taskEvents)
            {
                if (string.IsNullOrEmpty(e.ParentId)) continue;

                if (e.Status == TaskStatus.Completed)
                {
                    var at = e.ApprovedAt ?? e.CompletedAt ?? now;
                    notifications.Add(BuildNotification(e.ParentId, NotificationType.TaskApproved,
                        "Task Approved", $"{e.ChildName} completed \"{e.TemplateTitle}\" and earned points!", at));
                }
                else if (e.Status == TaskStatus.Rejected)
                {
                    var at = e.ReviewRequestedAt ?? now;
                    notifications.Add(BuildNotification(e.ParentId, NotificationType.TaskRejected,
                        "Task Needs Another Try", $"\"{e.TemplateTitle}\" was rejected for {e.ChildName}. Check the feedback.", at));
                }
                else
                {
                    var at = e.ReviewRequestedAt ?? now;
                    notifications.Add(BuildNotification(e.ParentId, NotificationType.ReviewRequested,
                        "Review Needed", $"{e.ChildName} submitted \"{e.TemplateTitle}\" for your review.", at));
                }

                if (notifications.Count >= 2000)
                {
                    await FlushAsync(context, notifications);
                }
            }
            await FlushAsync(context, notifications);

            // 2) Adventure milestones -> child
            var weeklyAdventures = await context.WeeklyAdventures.IgnoreQueryFilters()
                .Where(w => w.CreatedBy == "DemoSeeder" && w.Status != WeeklyAdventureStatus.Inactive)
                .Select(w => new { w.Id, w.StartDate, w.Adventure.TitleEn, w.ClassId })
                .ToListAsync();

            var progresses = await context.ChildAdventureProgresses.IgnoreQueryFilters()
                .Where(p => p.CreatedBy == "DemoSeeder")
                .Select(p => new { p.ChildId, p.WeeklyAdventureId, p.IsCompleted, p.CompletedAt })
                .ToListAsync();
            var progressByWeekly = progresses
                .Where(p => p.IsCompleted)
                .ToLookup(p => p.WeeklyAdventureId);

            var classChildren = await context.Childrens.IgnoreQueryFilters()
                .Where(c => c.ClassId != null)
                .Select(c => new { c.Id, c.ClassId })
                .ToListAsync();
            var childrenByClass = classChildren.ToLookup(c => c.ClassId);

            foreach (var wa in weeklyAdventures)
            {
                foreach (var child in childrenByClass[wa.ClassId])
                {
                    notifications.Add(BuildNotification(child.Id, NotificationType.AdventureStarted,
                        "New Adventure!", $"\"{wa.TitleEn}\" has started. Day 1 is ready!", wa.StartDate, wa.Id));
                }

                foreach (var completed in progressByWeekly[wa.Id])
                {
                    notifications.Add(BuildNotification(completed.ChildId, NotificationType.WeekBonus,
                        "Adventure Completed!", $"You finished \"{wa.TitleEn}\" and earned a bonus!",
                        completed.CompletedAt ?? now, wa.Id));
                }

                if (notifications.Count >= 2000)
                    await FlushAsync(context, notifications);
            }
            await FlushAsync(context, notifications);

            // 3) Gift purchases -> parent
            var giftPurchases = await context.ChildGifts.IgnoreQueryFilters()
                .Where(cg => cg.CreatedBy == "DemoSeeder")
                .Select(cg => new { cg.PurchasedAt, GiftName = cg.Gift.NameEn, ParentId = cg.Child.ParentId, ChildName = cg.Child.Name })
                .ToListAsync();

            foreach (var g in giftPurchases)
            {
                if (string.IsNullOrEmpty(g.ParentId)) continue;
                notifications.Add(BuildNotification(g.ParentId, NotificationType.GiftPurchased,
                    "Gift Redeemed", $"{g.ChildName} redeemed \"{g.GiftName}\" with their points.", g.PurchasedAt));
            }
            await FlushAsync(context, notifications);

            // 4) Rewards given -> parent + child
            var givenRewards = await context.Rewards.IgnoreQueryFilters()
                .Where(r => r.CreatedBy == "DemoSeeder" && r.GivenAt != null)
                .Select(r => new { r.GivenAt, r.ParentId, r.ChildId, r.NameEn })
                .ToListAsync();

            foreach (var r in givenRewards)
            {
                notifications.Add(BuildNotification(r.ParentId, NotificationType.RewardGiven,
                    "Reward Given", $"You gave the \"{r.NameEn}\" reward.", r.GivenAt!.Value));
                notifications.Add(BuildNotification(r.ChildId, NotificationType.RewardGiven,
                    "You Got a Reward!", $"You received the \"{r.NameEn}\" reward. Great job!", r.GivenAt!.Value));
            }
            await FlushAsync(context, notifications);

            // 5) Level ups -> child + parent
            var levelledChildren = await context.Childrens.IgnoreQueryFilters()
                .Where(c => c.InstitutionId != null && c.Institution!.Code.StartsWith("SCH-DEMO") && c.LevelId != null)
                .Select(c => new { c.Id, c.ParentId, c.Name, LevelName = c.Level!.Name, LevelOrder = c.Level!.Order, c.CreatedAt })
                .ToListAsync();

            foreach (var c in levelledChildren)
            {
                if (c.LevelOrder <= 1) continue;
                var at = DemoDataContext.RandomDateWeighted(c.CreatedAt, now, 0.8);
                notifications.Add(BuildNotification(c.Id, NotificationType.LevelUp,
                    "Level Up!", $"Congratulations! You reached level \"{c.LevelName}\".", at));
                if (!string.IsNullOrEmpty(c.ParentId))
                    notifications.Add(BuildNotification(c.ParentId, NotificationType.LevelUp,
                        "Level Up!", $"{c.Name} reached level \"{c.LevelName}\"!", at));

                if (notifications.Count >= 2000)
                    await FlushAsync(context, notifications);
            }
            await FlushAsync(context, notifications);

            // 6) Class enrollment -> supervisors
            var supervisorsByClass = await context.ClassSupervisors.IgnoreQueryFilters()
                .Select(cs => new { cs.ClassId, SupervisorUserId = cs.Supervisor.Id })
                .ToListAsync();
            var supervisorLookup = supervisorsByClass.ToLookup(s => s.ClassId);

            foreach (var child in classChildren)
            {
                if (child.ClassId == null) continue;
                foreach (var sup in supervisorLookup[child.ClassId])
                {
                    notifications.Add(BuildNotification(sup.SupervisorUserId, NotificationType.ChildEnrolledToClass,
                        "New Student Enrolled", "A new child has been enrolled in your class.", now.AddDays(-rng.Next(1, 90))));
                }

                if (notifications.Count >= 2000)
                    await FlushAsync(context, notifications);
            }
            await FlushAsync(context, notifications);
        }

        private static async Task<bool> AlreadySeeded(AppDbContext context)
        {
            // Notification has no CreatedBy/marker field, so use a one-off sentinel row instead.
            return await context.Notifications.IgnoreQueryFilters().AnyAsync(n => n.Title == "__DemoSeederMarker__");
        }

        private static Notification BuildNotification(string userId, NotificationType type, string title, string body, DateTime at, string? relatedId = null)
        {
            var isOld = (DateTime.UtcNow - at).TotalDays > 14;
            return new Notification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Body = body,
                RelatedEntityId = relatedId,
                CreatedAt = at,
                IsRead = isOld ? DemoDataContext.Rng.NextDouble() < 0.9 : DemoDataContext.Rng.NextDouble() < 0.4
            };
        }

        private static async Task FlushAsync(AppDbContext context, List<Notification> notifications)
        {
            if (notifications.Count == 0) return;

            await context.Notifications.AddRangeAsync(notifications);
            await context.SaveChangesAsync();
            notifications.Clear();
        }
    }
}
