// Jobs/AdventureAssignmentJob.cs
using GoKidAPI.Data;
using GoKidAPI.Entity.Institiution;
using GoKidAPI.Enums;
using GoKidAPI.Enums.Adventures;
using GoKidAPI.Services.Notifications;

using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Jobs
{
    public class AdventureAssignmentJob
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AdventureAssignmentJob> _logger;
        private readonly INotificationService _notificationService;

        public AdventureAssignmentJob(
            AppDbContext context,
            ILogger<AdventureAssignmentJob> logger,
            INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
        }

        /// <summary>
        /// بيتشغل لما الـ Admin يعمل Assign Adventure to Class
        /// بيكرير ChildAdventureTask لكل أطفال الكلاس لليوم الأول بس
        /// </summary>
        public async Task AssignDayOneTasksAsync(string weeklyAdventureId)
        {
            var weeklyAdventure = await _context.WeeklyAdventures
                .Include(wa => wa.Adventure)
                    .ThenInclude(a => a.Tasks)
                .Include(wa => wa.Class)
                    .ThenInclude(c => c.Children)
                .FirstOrDefaultAsync(wa => wa.Id == weeklyAdventureId && !wa.IsDeleted);

            if (weeklyAdventure == null)
            {
                _logger.LogWarning("WeeklyAdventure {Id} not found", weeklyAdventureId);
                return;
            }

            // جيب تاسك اليوم الأول
            var dayOneTask = weeklyAdventure.Adventure.Tasks
                .FirstOrDefault(t => t.DayNumber == 1);

            if (dayOneTask == null)
            {
                _logger.LogWarning("No Day 1 task found for Adventure {Id}", weeklyAdventure.AdventureId);
                return;
            }

            var children = weeklyAdventure.Class.Children
                .Where(c => !c.IsDeleted)
                .ToList();

            var newTasks = new List<ChildAdventureTask>();

            foreach (var child in children)
            {
                // تحقق إنها مش موجودة قبل كده
                var alreadyExists = await _context.ChildAdventureTasks
                    .AnyAsync(cat => cat.ChildId == child.Id
                                  && cat.AdventureTaskId == dayOneTask.Id
                                  && cat.WeeklyAdventureId == weeklyAdventureId);

                if (alreadyExists) continue;

                newTasks.Add(new ChildAdventureTask
                {
                    Id = Guid.NewGuid().ToString(),
                    ChildId = child.Id,
                    AdventureTaskId = dayOneTask.Id,
                    WeeklyAdventureId = weeklyAdventureId,
                    Status = AdventureChildTaskStatus.Pending,
                    CreatedBy = "System"
                });
            }

            if (newTasks.Any())
            {
                await _context.ChildAdventureTasks.AddRangeAsync(newTasks);
                await _context.SaveChangesAsync();
            }

            // Notify every child in the class that the adventure has started
            foreach (var child in children)
            {
                await _notificationService.SendAsync(
                    child.Id,
                    NotificationType.AdventureStarted,
                    "New Adventure!",
                    $"\"{weeklyAdventure.Adventure.TitleEn}\" has started. Day 1 is ready!",
                    weeklyAdventure.Id);
            }

            _logger.LogInformation(
                "Day 1 tasks assigned: {Count} tasks for WeeklyAdventure {Id}",
                newTasks.Count, weeklyAdventureId);
        }

        /// <summary>
        /// بيتشغل كل يوم الساعة 12 الليل
        /// 1. يقفل تاسكات اميس اللي ملحقوش (Missed) بنقاط أقل
        /// 2. يفتح تاسكات النهارده
        /// </summary>
        public async Task ProcessDailyAdventureTasksAsync()
        {
            var today = DateTime.UtcNow.Date;
            var yesterday = today.AddDays(-1);

            // جيب كل الـ WeeklyAdventures المفعلة
            var activeWeeklyAdventures = await _context.WeeklyAdventures
                .Include(wa => wa.Adventure)
                    .ThenInclude(a => a.Tasks)
                .Include(wa => wa.Class)
                    .ThenInclude(c => c.Children)
                .Where(wa => wa.Status == WeeklyAdventureStatus.Active
                          && wa.StartDate <= today
                          && wa.EndDate >= today
                          && !wa.IsDeleted)
                .ToListAsync();

            foreach (var weeklyAdventure in activeWeeklyAdventures)
            {
                await ProcessWeeklyAdventureAsync(weeklyAdventure, today);
            }
        }

        private async Task ProcessWeeklyAdventureAsync(
            WeeklyAdventure weeklyAdventure,
            DateTime today)
        {
            var currentDayNumber = (today - weeklyAdventure.StartDate.Date).Days + 1;
            var previousDayNumber = currentDayNumber - 1;

            if (previousDayNumber < 1) return;

            var previousDayTask = weeklyAdventure.Adventure.Tasks
                .FirstOrDefault(t => t.DayNumber == previousDayNumber);

            if (previousDayTask == null) return;

            // جيب الأطفال في الكلاس
            var childIds = weeklyAdventure.Class.Children
                .Where(c => !c.IsDeleted)
                .Select(c => c.Id)
                .ToList();

            // جيب الأطفال اللي عملوا Submit
            var submittedChildIds = await _context.ChildAdventureTasks
                .Where(cat => cat.AdventureTaskId == previousDayTask.Id
                           && cat.WeeklyAdventureId == weeklyAdventure.Id
                           && !cat.IsDeleted)
                .Select(cat => cat.ChildId)
                .ToListAsync();

            // الأطفال اللي ما submitوش → كريت ليهم ChildAdventureTask بـ Missed
            var missedChildIds = childIds.Except(submittedChildIds).ToList();

            var missedTasks = missedChildIds.Select(childId => new ChildAdventureTask
            {
                Id = Guid.NewGuid().ToString(),
                ChildId = childId,
                AdventureTaskId = previousDayTask.Id,
                WeeklyAdventureId = weeklyAdventure.Id,
                Status = AdventureChildTaskStatus.Missed,
                EarnedStars = 1, // نجمة واحدة بدل 3
                CreatedBy = "System",
                UpdatedBy = "System"
            }).ToList();

            if (missedTasks.Any())
                await _context.ChildAdventureTasks.AddRangeAsync(missedTasks);

            await _context.SaveChangesAsync();

            await NotifyClassSupervisorsAsync(
                weeklyAdventure.ClassId,
                NotificationType.AdventureDayCompleted,
                "Day Completed",
                $"Day {previousDayNumber} of '{weeklyAdventure.Adventure.TitleEn}' has ended. Review your children's task results.",
                weeklyAdventure.Id);

            _logger.LogInformation(
                "Missed tasks created: {Count} for WeeklyAdventure {Id}",
                missedTasks.Count, weeklyAdventure.Id);

            // Notify all active children that today's new day is unlocked (Day 2+)
            var totalDays = weeklyAdventure.Adventure.Tasks.Count;
            if (currentDayNumber >= 2 && currentDayNumber <= totalDays)
            {
                foreach (var childId in childIds)
                {
                    await _notificationService.SendAsync(
                        childId,
                        NotificationType.AdventureNewDay,
                        $"Day {currentDayNumber} Unlocked!",
                        $"Day {currentDayNumber} of \"{weeklyAdventure.Adventure.TitleEn}\" is ready. Don't miss it!",
                        weeklyAdventure.Id);
                }

                await NotifyClassSupervisorsAsync(
                    weeklyAdventure.ClassId,
                    NotificationType.DailyAdventureTasksAssigned,
                    "Daily Tasks Assigned",
                    $"Day {currentDayNumber} tasks for '{weeklyAdventure.Adventure.TitleEn}' have been assigned to the children in class '{weeklyAdventure.Class.Name}'.",
                    weeklyAdventure.Id);
            }
        }

        // Helper method to notify all supervisors of a class about a specific event
        private async Task NotifyClassSupervisorsAsync(
            string classId,
            NotificationType type,
            string title,
            string body,
            string? relatedEntityId = null)
        {
            var supervisorUserIds = await _context.ClassSupervisors
                .Where(cs => cs.ClassId == classId && !cs.IsDeleted)
                .Select(cs => cs.Supervisor.AppUserId)
                .ToListAsync();

            foreach (var supervisorUserId in supervisorUserIds)
            {
                await _notificationService.SendAsync(
                    supervisorUserId,
                    type,
                    title,
                    body,
                    relatedEntityId);
            }
        }
    }
}