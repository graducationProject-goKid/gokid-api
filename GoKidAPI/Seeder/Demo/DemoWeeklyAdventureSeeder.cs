using GoKidAPI.Data;
using GoKidAPI.Entity;
using GoKidAPI.Entity.Institiution;
using GoKidAPI.Enums;
using GoKidAPI.Enums.Adventures;

using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Seeder.Demo
{
    // Assigns Adventures to classes as WeeklyAdventure instances across the last ~3 months,
    // generating each child's day-by-day ChildAdventureTask submissions and the weekly
    // ChildAdventureProgress rollup (mirroring AdventureAssignmentJob/SupervisorService logic).
    public static class DemoWeeklyAdventureSeeder
    {
        private static double CompletionRate(DemoDataContext.ActivityTier tier) => tier switch
        {
            DemoDataContext.ActivityTier.Beginner => 0.45,
            DemoDataContext.ActivityTier.Casual => 0.60,
            DemoDataContext.ActivityTier.Active => 0.80,
            DemoDataContext.ActivityTier.StarPerformer => 0.95,
            DemoDataContext.ActivityTier.Veteran => 0.70,
            _ => 0.6
        };

        public static async Task SeedAsync(AppDbContext context)
        {
            if (await context.WeeklyAdventures.IgnoreQueryFilters().AnyAsync(w => w.CreatedBy == "DemoSeeder"))
                return;

            var now = DateTime.UtcNow;
            var rng = DemoDataContext.Rng;

            var adventuresByInstitution = await context.Adventures
                .Include(a => a.Tasks).ThenInclude(t => t.TaskTemplate)
                .Where(a => a.CreatedBy == "DemoSeeder")
                .ToListAsync();

            var classes = await context.Classes
                .Where(c => c.Institution.Code.StartsWith("SCH-DEMO"))
                .Include(c => c.Children)
                .ToListAsync();

            int classCounter = 0;

            foreach (var cls in classes)
            {
                classCounter++;
                var availableAdventures = adventuresByInstitution.Where(a => a.InstitutionId == cls.InstitutionId).ToList();
                if (availableAdventures.Count == 0 || cls.Children.Count == 0) continue;

                var children = cls.Children.Where(c => !c.IsDeleted).ToList();

                var windows = BuildWindows(now, rng);

                foreach (var window in windows)
                {
                    var adventure = DemoDataContext.PickRandom(availableAdventures);

                    var weeklyAdventure = new WeeklyAdventure
                    {
                        ClassId = cls.Id,
                        AdventureId = adventure.Id,
                        StartDate = window.Start,
                        EndDate = window.Start.AddDays(adventure.WeekDuration),
                        Status = window.Status,
                        CreatedAt = window.Start,
                        CreatedBy = "DemoSeeder"
                    };
                    context.WeeklyAdventures.Add(weeklyAdventure);

                    if (window.Status == WeeklyAdventureStatus.Inactive)
                        continue; // not started yet - no child submissions

                    var currentDayNumber = window.Status == WeeklyAdventureStatus.Completed
                        ? adventure.WeekDuration
                        : Math.Clamp((int)(now - window.Start).TotalDays + 1, 1, adventure.WeekDuration);

                    var orderedTasks = adventure.Tasks.OrderBy(t => t.DayNumber).ToList();

                    foreach (var child in children)
                    {
                        var tier = DemoDataContext.TierForChild(child.Id);
                        var successRate = CompletionRate(tier);

                        var progress = new ChildAdventureProgress
                        {
                            ChildId = child.Id,
                            WeeklyAdventureId = weeklyAdventure.Id,
                            CreatedAt = window.Start,
                            CreatedBy = "DemoSeeder"
                        };

                        int earnedStars = 0, earnedPoints = 0, completedDays = 0;
                        var childTasks = new List<ChildAdventureTask>();
                        DateTime? lastCompletionDate = null;

                        for (int day = 1; day <= currentDayNumber; day++)
                        {
                            var adventureTask = orderedTasks.FirstOrDefault(t => t.DayNumber == day);
                            if (adventureTask == null) continue;

                            var isCurrentUnresolvedDay = window.Status == WeeklyAdventureStatus.Active && day == currentDayNumber;
                            var dayDate = window.Start.AddDays(day - 1);

                            var cat = new ChildAdventureTask
                            {
                                ChildId = child.Id,
                                AdventureTaskId = adventureTask.Id,
                                WeeklyAdventureId = weeklyAdventure.Id,
                                ChildAdventureProgressId = progress.Id,
                                CreatedAt = dayDate,
                                CreatedBy = "DemoSeeder"
                            };

                            if (isCurrentUnresolvedDay && rng.NextDouble() > successRate)
                            {
                                cat.Status = AdventureChildTaskStatus.Pending;
                            }
                            else if (rng.NextDouble() < successRate)
                            {
                                cat.Status = AdventureChildTaskStatus.Completed;
                                cat.EarnedStars = adventureTask.Stars;
                                cat.IsApproved = true;
                                cat.ReviewedBy = "DemoSeeder";
                                cat.EvidenceUrl = DemoDataContext.Placeholder("evidence", "300x200");
                                cat.SubmittedAt = dayDate.AddHours(rng.Next(1, 10));
                                cat.ReviewedAt = cat.SubmittedAt.Value.AddHours(rng.Next(1, 20));
                                cat.CompletedAt = cat.ReviewedAt;

                                earnedStars += adventureTask.Stars;
                                earnedPoints += adventureTask.TaskTemplate.BasePoints;
                                completedDays++;
                                lastCompletionDate = cat.CompletedAt;

                                context.PointsTransactions.Add(new PointsTransaction
                                {
                                    ChildId = child.Id,
                                    Points = adventureTask.TaskTemplate.BasePoints,
                                    Reason = $"Completed Day {day} of '{adventure.TitleEn}'",
                                    SourceType = PointsSourceType.AdventureTask,
                                    CreatedAt = cat.CompletedAt.Value,
                                    CreatedBy = "DemoSeeder"
                                });
                            }
                            else
                            {
                                cat.Status = AdventureChildTaskStatus.Missed;
                                cat.EarnedStars = 1;
                                earnedStars += 1;
                            }

                            childTasks.Add(cat);
                        }

                        progress.EarnedStars = earnedStars;
                        progress.EarnedPoints = earnedPoints;
                        progress.CompletedDaysCount = completedDays;

                        if (completedDays >= adventure.WeekDuration && window.Status == WeeklyAdventureStatus.Completed)
                        {
                            progress.WeekBonusAwarded = true;
                            progress.IsCompleted = true;
                            progress.CompletedAt = lastCompletionDate ?? window.Start.AddDays(adventure.WeekDuration);

                            context.PointsTransactions.Add(new PointsTransaction
                            {
                                ChildId = child.Id,
                                Points = adventure.BonusPoints,
                                Reason = $"Weekly bonus for completing '{adventure.TitleEn}'",
                                SourceType = PointsSourceType.WeeklyAdventureBonus,
                                CreatedAt = progress.CompletedAt.Value,
                                CreatedBy = "DemoSeeder"
                            });
                        }

                        context.ChildAdventureProgresses.Add(progress);
                        context.ChildAdventureTasks.AddRange(childTasks);
                    }
                }

                if (classCounter % 5 == 0)
                    await context.SaveChangesAsync();
            }

            await context.SaveChangesAsync();
        }

        private record Window(DateTime Start, WeeklyAdventureStatus Status);

        private static List<Window> BuildWindows(DateTime now, Random rng)
        {
            var windows = new List<Window>
            {
                new(now.AddDays(-75), WeeklyAdventureStatus.Completed),
                new(now.AddDays(-32), WeeklyAdventureStatus.Completed),
                new(now.AddDays(-rng.Next(2, 6)), WeeklyAdventureStatus.Active)
            };

            if (rng.NextDouble() < 0.3)
                windows.Add(new Window(now.AddDays(rng.Next(2, 6)), WeeklyAdventureStatus.Inactive));

            return windows;
        }
    }
}
