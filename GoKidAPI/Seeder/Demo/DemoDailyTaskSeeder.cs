using GoKidAPI.Data;
using GoKidAPI.Entity;
using GoKidAPI.Entity.Tasks;
using GoKidAPI.Enums;
using GoKidAPI.Enums.Tasks;

using Microsoft.EntityFrameworkCore;

using TaskStatus = GoKidAPI.Enums.Tasks.TaskStatus;

namespace GoKidAPI.Seeder.Demo
{
    // Generates historical "daily task" activity per child (mirrors DailyTaskAssignmentJob's
    // pattern of ~5 tasks/day, but sampled over active days instead of literally every day,
    // to keep row counts sane). Only creates PointsTransaction rows for Completed tasks -
    // Child.TotalPoints/HighestPoints/LevelId are finalized later by DemoPointsRollupSeeder.
    public static class DemoDailyTaskSeeder
    {
        private static (int Min, int Max) TaskCountRange(DemoDataContext.ActivityTier tier) => tier switch
        {
            DemoDataContext.ActivityTier.Beginner => (12, 25),
            DemoDataContext.ActivityTier.Casual => (35, 60),
            DemoDataContext.ActivityTier.Active => (65, 100),
            DemoDataContext.ActivityTier.StarPerformer => (95, 140),
            DemoDataContext.ActivityTier.Veteran => (55, 85),
            _ => (20, 40)
        };

        private static double BaseSuccessRate(DemoDataContext.ActivityTier tier) => tier switch
        {
            DemoDataContext.ActivityTier.Beginner => 0.50,
            DemoDataContext.ActivityTier.Casual => 0.65,
            DemoDataContext.ActivityTier.Active => 0.80,
            DemoDataContext.ActivityTier.StarPerformer => 0.92,
            DemoDataContext.ActivityTier.Veteran => 0.85, // was high while active
            _ => 0.6
        };

        public static async Task SeedAsync(AppDbContext context)
        {
            if (await context.ChildTasks.IgnoreQueryFilters().AnyAsync(ct => ct.CreatedBy == "DemoSeeder"))
                return;

            var now = DateTime.UtcNow;

            var children = await context.Childrens
                .Where(c => c.InstitutionId != null && c.Institution!.Code.StartsWith("SCH-DEMO"))
                .ToListAsync();

            var templates = await context.TaskTemplates.IgnoreQueryFilters().Where(t => !t.IsDeleted).ToListAsync();
            if (templates.Count == 0 || children.Count == 0) return;

            var rng = DemoDataContext.Rng;
            int childIndex = 0;

            foreach (var child in children)
            {
                childIndex++;
                var tier = DemoDataContext.TierForChild(child.Id);
                var (min, max) = TaskCountRange(tier);
                var totalTasks = rng.Next(min, max + 1);

                var windowStart = child.CreatedAt;
                var windowEnd = now;
                if (windowEnd <= windowStart) windowEnd = windowStart.AddDays(1);

                var newTasks = new List<ChildTask>();
                var newTransactions = new List<PointsTransaction>();

                for (int t = 0; t < totalTasks; t++)
                {
                    var template = DemoDataContext.PickRandom(templates);

                    DateTime assignedAt = tier == DemoDataContext.ActivityTier.Veteran
                        ? DemoDataContext.RandomDateWeighted(windowStart, windowEnd, 2.2) // pushed toward the past
                        : DemoDataContext.RandomDateWeighted(windowStart, windowEnd, 0.85);

                    var daysAgo = (now - assignedAt).TotalDays;
                    var isParentAssigned = rng.NextDouble() < 0.15;

                    var childTask = new ChildTask
                    {
                        ChildId = child.Id,
                        TaskTemplateId = template.Id,
                        Source = isParentAssigned ? TaskSource.Parent : TaskSource.SystemGeneral,
                        AssignedByParentId = isParentAssigned ? child.ParentId : null,
                        AssignedAt = assignedAt,
                        CreatedAt = assignedAt,
                        CreatedBy = "DemoSeeder"
                    };

                    // Effective success rate: veterans who "went dormant" resolve recent tasks far less often.
                    var successRate = tier == DemoDataContext.ActivityTier.Veteran && daysAgo < 25
                        ? 0.25
                        : BaseSuccessRate(tier);

                    if (daysAgo < 1)
                    {
                        // Just assigned today - still in flight.
                        var roll = rng.NextDouble();
                        childTask.Status = roll < 0.45 ? TaskStatus.Pending
                            : roll < 0.75 ? TaskStatus.InProgress
                            : TaskStatus.ReviewRequested;
                        if (childTask.Status != TaskStatus.Pending)
                            childTask.StartedAt = assignedAt.AddMinutes(rng.Next(5, 120));
                        if (childTask.Status == TaskStatus.ReviewRequested)
                            childTask.ReviewRequestedAt = assignedAt.AddMinutes(rng.Next(30, 200));
                    }
                    else
                    {
                        var roll = rng.NextDouble();
                        if (roll < successRate)
                        {
                            childTask.Status = TaskStatus.Completed;
                            childTask.StartedAt = assignedAt.AddMinutes(rng.Next(5, 90));
                            var completedAt = assignedAt.AddMinutes(rng.Next(20, 300));
                            if (completedAt > now) completedAt = now;

                            if (template.TemplateType == TaskTemplateType.EvidenceSubmission)
                            {
                                childTask.ReviewRequestedAt = childTask.StartedAt;
                                childTask.ApprovedAt = completedAt;
                            }
                            childTask.CompletedAt = completedAt;

                            newTransactions.Add(new PointsTransaction
                            {
                                ChildId = child.Id,
                                Points = template.BasePoints,
                                Reason = $"Completed '{template.TitleEn}'",
                                SourceType = PointsSourceType.ChildTask,
                                CreatedAt = completedAt,
                                CreatedBy = "DemoSeeder"
                            });
                        }
                        else if (roll < successRate + 0.08)
                        {
                            childTask.Status = TaskStatus.Rejected;
                            childTask.StartedAt = assignedAt.AddMinutes(rng.Next(5, 90));
                            childTask.ReviewRequestedAt = childTask.StartedAt;
                            childTask.RejectionReason = "Evidence didn't match the task requirements, please try again.";
                        }
                        else
                        {
                            // Abandoned/never resolved - matches real system behavior (no auto-expiry).
                            childTask.Status = rng.NextDouble() < 0.5 ? TaskStatus.Pending : TaskStatus.InProgress;
                            if (childTask.Status == TaskStatus.InProgress)
                                childTask.StartedAt = assignedAt.AddMinutes(rng.Next(5, 90));
                        }
                    }

                    newTasks.Add(childTask);
                }

                if (newTasks.Count > 0)
                {
                    await context.ChildTasks.AddRangeAsync(newTasks);
                    if (newTransactions.Count > 0)
                        await context.PointsTransactions.AddRangeAsync(newTransactions);
                }

                // Flush periodically to keep the change tracker small over ~260 children.
                if (childIndex % 15 == 0)
                    await context.SaveChangesAsync();
            }

            await context.SaveChangesAsync();
        }
    }
}
