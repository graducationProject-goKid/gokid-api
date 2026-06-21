using GoKidAPI.Data;
using GoKidAPI.Entity.Tasks;
using GoKidAPI.Enums.Tasks;

using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Jobs
{
    public class DailyTaskAssignmentJob
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DailyTaskAssignmentJob> _logger;

        public DailyTaskAssignmentJob(AppDbContext context, ILogger<DailyTaskAssignmentJob> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task AssignDailyTasksAsync()
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            _logger.LogInformation("Starting daily task assignment for {Date}", today);

            // جيب كل الأطفال
            var allChildren = await _context.Childrens
                .Select(c => c.Id)
                .ToListAsync();

            // جيب كل التمبليتس المتاحة مرة واحدة بدل ما تعمل query لكل طفل
            var allTemplates = await _context.TaskTemplates
                .Where(t => !t.IsDeleted)
                .Select(t => t.Id)
                .ToListAsync();

            if (allTemplates.Count == 0)
            {
                _logger.LogWarning("No task templates found. Skipping assignment.");
                return;
            }

            // جيب الأطفال اللي عندهم tasks النهارده بالفعل
            var childrenWithTasksToday = await _context.ChildTasks
                .Where(ct => ct.AssignedAt >= today && ct.AssignedAt < tomorrow
                             && ct.Source == TaskSource.SystemGeneral)
                .GroupBy(ct => ct.ChildId)
                .Select(g => new
                {
                    ChildId = g.Key,
                    ExistingTemplateIds = g.Select(ct => ct.TaskTemplateId).ToList()
                })
                .ToListAsync();

            var childrenWithTasksMap = childrenWithTasksToday
                .ToDictionary(x => x.ChildId, x => x.ExistingTemplateIds);

            var newTasks = new List<ChildTask>();
            var random = new Random();

            foreach (var childId in allChildren)
            {
                var existingTemplateIds = childrenWithTasksMap.ContainsKey(childId)
                    ? childrenWithTasksMap[childId]
                    : new List<string>();

                int needed = 5 - existingTemplateIds.Count;
                if (needed <= 0) continue;

                // اختار تمبليتس عشوائية مختلفة عن اللي موجودة
                var available = allTemplates
                    .Except(existingTemplateIds)
                    .OrderBy(_ => random.Next())
                    .Take(needed)
                    .ToList();

                foreach (var templateId in available)
                {
                    newTasks.Add(new ChildTask
                    {
                        Id = Guid.NewGuid().ToString(),
                        ChildId = childId,
                        TaskTemplateId = templateId,
                        AssignedAt = today,
                        Status = Enums.Tasks.TaskStatus.Pending,
                        CreatedBy = "System",
                        CreatedAt = DateTime.UtcNow,
                        Source = TaskSource.SystemGeneral,
                    });
                }
            }

            if (newTasks.Any())
            {
                await _context.ChildTasks.AddRangeAsync(newTasks);
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation(
                "Daily task assignment done. Assigned {Count} tasks to {Children} children.",
                newTasks.Count, allChildren.Count);
        }
    }
}
