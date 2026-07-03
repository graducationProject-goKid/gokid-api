using GoKidAPI.Data;
using GoKidAPI.DTO.Dashboard;
using GoKidAPI.Enums;
using GoKidAPI.Enums.Adventures;
using GoKidAPI.Enums.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace GoKidAPI.Services.Dashboard
{
    // Intermediate projection types — private to this service
    file sealed class ProgressRow { public string WAId { get; set; } = ""; public bool IsCompleted { get; set; } public int Stars { get; set; } }
    file sealed class TaskRow { public string ChildId { get; set; } = ""; public string? ClassId { get; set; } public Enums.Tasks.TaskStatus Status { get; set; } public DateTime? CompletedAt { get; set; } public DateTime? ReviewRequestedAt { get; set; } public DateTime? UpdatedAt { get; set; } }

    public class InstitutionDashboardService : IInstitutionDashboardService
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
        private static string CacheKey(string institutionId) => $"inst_dashboard_{institutionId}";

        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<InstitutionDashboardService> _logger;

        public InstitutionDashboardService(AppDbContext context, IMemoryCache cache, ILogger<InstitutionDashboardService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public void InvalidateCache(string institutionId) => _cache.Remove(CacheKey(institutionId));

        public async Task<InstitutionDashboardResponse?> GetDashboardAsync(string adminUserId)
        {
            var admin = await _context.InstitutionAdmins
                .AsNoTracking()
                .Include(a => a.Institution)
                .FirstOrDefaultAsync(a => a.Id == adminUserId && !a.IsDeleted);

            if (admin?.Institution == null || admin.InstitutionId == null)
                return null;

            var institutionId = admin.InstitutionId;
            var key = CacheKey(institutionId);

            if (_cache.TryGetValue(key, out InstitutionDashboardResponse? cached) && cached != null)
            {
                _logger.LogDebug("Institution dashboard served from cache for {InstitutionId}", institutionId);
                return cached;
            }

            _logger.LogInformation("Building institution dashboard for {InstitutionId}", institutionId);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var now = DateTime.UtcNow;
            var weekStart = now.AddDays(-7);

            // ── Base IDs ──────────────────────────────────────────────────────────
            var classIds = await _context.Classes
                .AsNoTracking()
                .Where(c => c.InstitutionId == institutionId && !c.IsDeleted)
                .Select(c => c.Id)
                .ToListAsync();

            var childIds = await _context.Childrens
                .AsNoTracking()
                .Where(c => c.InstitutionId == institutionId)
                .Select(c => c.Id)
                .ToListAsync();

            // ── Overview ──────────────────────────────────────────────────────────
            var totalClasses = classIds.Count;
            var totalChildren = childIds.Count;
            var totalSupervisors = await _context.Supervisors.AsNoTracking()
                .CountAsync(s => s.InstitutionId == institutionId && !s.IsDeleted);
            var childrenInClasses = await _context.Childrens.AsNoTracking()
                .CountAsync(c => c.InstitutionId == institutionId && c.ClassId != null);
            var activeWeeklyAdventures = classIds.Count == 0 ? 0
                : await _context.WeeklyAdventures.AsNoTracking()
                    .CountAsync(wa => !wa.IsDeleted && classIds.Contains(wa.ClassId) && wa.Status == WeeklyAdventureStatus.Active);
            var tasksAwaitingReview = childIds.Count == 0 ? 0
                : await _context.ChildTasks
                    .CountAsync(ct => childIds.Contains(ct.ChildId) && ct.Status == Enums.Tasks.TaskStatus.ReviewRequested);

            // ── Class entities + per-class stats ──────────────────────────────────
            var classEntities = await _context.Classes.AsNoTracking()
                .Where(c => c.InstitutionId == institutionId && !c.IsDeleted)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            var childrenPerClassRaw = await _context.Childrens.AsNoTracking()
                .Where(c => c.InstitutionId == institutionId && c.ClassId != null)
                .GroupBy(c => c.ClassId!)
                .Select(g => new { ClassId = g.Key, Count = g.Count() })
                .ToListAsync();
            var childrenClassDict = childrenPerClassRaw.ToDictionary(x => x.ClassId, x => x.Count);

            var supPerClassRaw = classIds.Count == 0
                ? new List<(string ClassId, int Count)>()
                : (await _context.ClassSupervisors.AsNoTracking()
                    .Where(cs => !cs.IsDeleted && classIds.Contains(cs.ClassId))
                    .GroupBy(cs => cs.ClassId)
                    .Select(g => new { ClassId = g.Key, Count = g.Count() })
                    .ToListAsync())
                    .Select(x => (x.ClassId, x.Count)).ToList();
            var supClassDict = supPerClassRaw.ToDictionary(x => x.ClassId, x => x.Count);

            // Load all task rows for children in this institution
            List<TaskRow> allTaskRows;
            if (childIds.Count == 0)
            {
                allTaskRows = new();
            }
            else
            {
                allTaskRows = (await _context.ChildTasks.AsNoTracking()
                    .Where(ct => childIds.Contains(ct.ChildId))
                    .Select(ct => new { ct.ChildId, ct.Child.ClassId, ct.Status, ct.CompletedAt, ct.ReviewRequestedAt, ct.UpdatedAt })
                    .ToListAsync())
                    .Select(x => new TaskRow { ChildId = x.ChildId, ClassId = x.ClassId, Status = x.Status, CompletedAt = x.CompletedAt, ReviewRequestedAt = x.ReviewRequestedAt, UpdatedAt = x.UpdatedAt })
                    .ToList();
            }

            var tasksByClassDict = allTaskRows
                .Where(t => t.ClassId != null)
                .GroupBy(t => t.ClassId!)
                .ToDictionary(g => g.Key, g => g.ToList());

            var classesWithActiveAdventureSet = classIds.Count == 0
                ? new HashSet<string>()
                : (await _context.WeeklyAdventures.AsNoTracking()
                    .Where(wa => !wa.IsDeleted && classIds.Contains(wa.ClassId) && wa.Status == WeeklyAdventureStatus.Active)
                    .Select(wa => wa.ClassId).Distinct().ToListAsync()).ToHashSet();

            var classBreakdown = classEntities.Select(c =>
            {
                var rows = tasksByClassDict.GetValueOrDefault(c.Id, new List<TaskRow>());
                var total = rows.Count;
                var completed = rows.Count(r => r.Status == Enums.Tasks.TaskStatus.Completed);
                var pending = rows.Count(r => r.Status == Enums.Tasks.TaskStatus.ReviewRequested);
                return new InstClassEntry
                {
                    ClassId = c.Id,
                    ClassName = c.Name,
                    ChildrenCount = childrenClassDict.GetValueOrDefault(c.Id, 0),
                    SupervisorCount = supClassDict.GetValueOrDefault(c.Id, 0),
                    PendingReviewCount = pending,
                    CompletedTasksCount = completed,
                    TotalTasksCount = total,
                    TaskCompletionRate = total == 0 ? 0.0 : Math.Round(completed / (double)total * 100, 1),
                    HasActiveAdventure = classesWithActiveAdventureSet.Contains(c.Id)
                };
            }).OrderByDescending(c => c.TaskCompletionRate).ToList();

            // ── Supervisors ───────────────────────────────────────────────────────
            var supervisorEntities = await _context.Supervisors.AsNoTracking()
                .Include(s => s.AppUser)
                .Where(s => s.InstitutionId == institutionId && !s.IsDeleted)
                .ToListAsync();

            var allSupervisorClassesRaw = classIds.Count == 0
                ? new List<(string SupervisorId, string ClassId)>()
                : (await _context.ClassSupervisors.AsNoTracking()
                    .Where(cs => !cs.IsDeleted && classIds.Contains(cs.ClassId))
                    .Select(cs => new { cs.SupervisorId, cs.ClassId })
                    .ToListAsync())
                    .Select(x => (x.SupervisorId, x.ClassId)).ToList();

            var classesPerSupervisorDict = allSupervisorClassesRaw
                .GroupBy(x => x.SupervisorId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ClassId).ToList());

            var supervisorBreakdown = supervisorEntities.Select(s =>
            {
                var mySupervisedClassIds = classesPerSupervisorDict.GetValueOrDefault(s.Id, new List<string>());
                var myChildren = mySupervisedClassIds.Sum(cid => childrenClassDict.GetValueOrDefault(cid, 0));
                var myPending = mySupervisedClassIds
                    .Where(cid => tasksByClassDict.ContainsKey(cid))
                    .Sum(cid => tasksByClassDict[cid].Count(r => r.Status == Enums.Tasks.TaskStatus.ReviewRequested));
                return new InstSupervisorEntry
                {
                    SupervisorId = s.Id,
                    Name = s.AppUser?.DisplayName ?? s.AppUser?.UserName ?? "Unknown",
                    AssignedClassesCount = mySupervisedClassIds.Count,
                    SupervisedChildrenCount = myChildren,
                    PendingReviewCount = myPending
                };
            }).OrderByDescending(s => s.PendingReviewCount).ToList();

            var supervisorsWithClasses = supervisorBreakdown.Count(s => s.AssignedClassesCount > 0);

            // ── Children ──────────────────────────────────────────────────────────
            var childrenWithZeroPoints = await _context.Childrens.AsNoTracking()
                .CountAsync(c => c.InstitutionId == institutionId && c.TotalPoints == 0);
            var childrenWithNoLevel = await _context.Childrens.AsNoTracking()
                .CountAsync(c => c.InstitutionId == institutionId && c.LevelId == null);
            var avgPointsPerChild = childIds.Count == 0 ? 0.0
                : await _context.Childrens.AsNoTracking()
                    .Where(c => c.InstitutionId == institutionId)
                    .AverageAsync(c => (double?)c.TotalPoints) ?? 0.0;

            var childrenWithCompletedTask = childIds.Count == 0 ? 0
                : allTaskRows.Select(r => r.ChildId).Distinct()
                    .Count(cid => allTaskRows.Any(r => r.ChildId == cid && r.Status == Enums.Tasks.TaskStatus.Completed));

            var allLevels = await _context.Levels.AsNoTracking()
                .OrderBy(l => l.Order)
                .Select(l => new { l.Id, l.Name, l.Order, l.MinPoints })
                .ToListAsync();

            var childLevelGroupsRaw = childIds.Count == 0
                ? new List<(string LevelId, int Count)>()
                : (await _context.Childrens.AsNoTracking()
                    .Where(c => c.InstitutionId == institutionId && c.LevelId != null)
                    .GroupBy(c => c.LevelId!)
                    .Select(g => new { LevelId = g.Key, Count = g.Count() })
                    .ToListAsync())
                    .Select(x => (x.LevelId, x.Count)).ToList();

            var levelChildDict = childLevelGroupsRaw.ToDictionary(x => x.LevelId, x => x.Count);
            var leveledChildrenTotal = childLevelGroupsRaw.Sum(x => x.Count);

            var levelDistribution = allLevels.Select(l => new LevelDistributionEntry
            {
                LevelId = l.Id,
                LevelName = l.Name,
                Order = l.Order,
                MinPoints = l.MinPoints,
                ChildrenCount = levelChildDict.GetValueOrDefault(l.Id, 0),
                Percentage = leveledChildrenTotal == 0 ? 0.0
                    : Math.Round(levelChildDict.GetValueOrDefault(l.Id, 0) / (double)leveledChildrenTotal * 100, 1)
            }).ToList();

            var topChildEntities = await _context.Childrens.AsNoTracking()
                .Include(c => c.Level).Include(c => c.Class)
                .Where(c => c.InstitutionId == institutionId)
                .OrderByDescending(c => c.HighestPoints).Take(5)
                .ToListAsync();
            var topChildEntries = topChildEntities.Select(c => new InstTopChildEntry
            {
                ChildId = c.Id, Name = c.Name, ClassName = c.Class?.Name,
                HighestPoints = c.HighestPoints, LevelName = c.Level?.Name
            }).ToList();

            // ── Adventures ────────────────────────────────────────────────────────
            var adventureEntities = await _context.Adventures.AsNoTracking()
                .Where(a => a.InstitutionId == institutionId && !a.IsDeleted)
                .Select(a => new { a.Id, a.TitleEn, a.Status })
                .ToListAsync();

            List<(string Id, string AdventureId, WeeklyAdventureStatus Status)> waList;
            if (classIds.Count == 0)
            {
                waList = new();
            }
            else
            {
                waList = (await _context.WeeklyAdventures.AsNoTracking()
                    .Where(wa => !wa.IsDeleted && classIds.Contains(wa.ClassId))
                    .Select(wa => new { wa.Id, wa.AdventureId, wa.Status })
                    .ToListAsync())
                    .Select(x => (x.Id, x.AdventureId, x.Status)).ToList();
            }

            var waIds = waList.Select(wa => wa.Id).ToList();

            List<ProgressRow> progressData;
            if (waIds.Count == 0)
            {
                progressData = new();
            }
            else
            {
                progressData = (await _context.ChildAdventureProgresses.AsNoTracking()
                    .Where(p => waIds.Contains(p.WeeklyAdventureId))
                    .Select(p => new { p.WeeklyAdventureId, p.IsCompleted, p.EarnedStars })
                    .ToListAsync())
                    .Select(x => new ProgressRow { WAId = x.WeeklyAdventureId, IsCompleted = x.IsCompleted, Stars = x.EarnedStars })
                    .ToList();
            }

            var progressByWa = progressData.GroupBy(p => p.WAId)
                .ToDictionary(g => g.Key, g => g.ToList());
            var waByAdventure = waList.GroupBy(wa => wa.AdventureId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

            var adventureBreakdown = adventureEntities.Select(a =>
            {
                var myWaIds = waByAdventure.GetValueOrDefault(a.Id, new List<string>());
                var allProg = myWaIds.SelectMany(wid => progressByWa.GetValueOrDefault(wid, new List<ProgressRow>())).ToList();
                var total = allProg.Count;
                var completed = allProg.Count(p => p.IsCompleted);
                var avgStars = total == 0 ? 0.0 : allProg.Average(p => (double)p.Stars);
                return new InstAdventureEntry
                {
                    AdventureId = a.Id, TitleEn = a.TitleEn, Status = a.Status.ToString(),
                    TimesAssigned = myWaIds.Count, ParticipatingChildren = total,
                    CompletionRate = total == 0 ? 0.0 : Math.Round(completed / (double)total * 100, 1),
                    AverageStars = Math.Round(avgStars, 2)
                };
            }).OrderByDescending(a => a.CompletionRate).ToList();

            var totalParticipating = progressData.Count;
            var totalCompletedProg = progressData.Count(p => p.IsCompleted);
            var overallAdventureCompletionRate = totalParticipating == 0 ? 0.0
                : Math.Round(totalCompletedProg / (double)totalParticipating * 100, 1);
            var avgStarsOverall = totalParticipating == 0 ? 0.0
                : Math.Round(progressData.Average(p => (double)p.Stars), 2);

            // ── Tasks ─────────────────────────────────────────────────────────────
            var instTotalTasks = allTaskRows.Count;
            var instCompleted = allTaskRows.Count(r => r.Status == Enums.Tasks.TaskStatus.Completed);
            var instPending = allTaskRows.Count(r => r.Status == Enums.Tasks.TaskStatus.Pending);
            var instInProgress = allTaskRows.Count(r => r.Status == Enums.Tasks.TaskStatus.InProgress);
            var instRejected = allTaskRows.Count(r => r.Status == Enums.Tasks.TaskStatus.Rejected);
            double taskCompletionRate = instTotalTasks == 0 ? 0.0 : Math.Round(instCompleted / (double)instTotalTasks * 100, 1);
            double avgTasksPerChild = totalChildren == 0 ? 0.0 : Math.Round(instCompleted / (double)totalChildren, 1);

            var oldestPendingAt = allTaskRows
                .Where(r => r.Status == Enums.Tasks.TaskStatus.ReviewRequested && r.ReviewRequestedAt.HasValue)
                .Select(r => r.ReviewRequestedAt).Min();
            int oldestReviewAgeHours = oldestPendingAt.HasValue ? (int)(now - oldestPendingAt.Value).TotalHours : 0;

            // ── Points & Levels ───────────────────────────────────────────────────
            var totalPointsEarned = childIds.Count == 0 ? 0L
                : (long)(await _context.PointsTransactions.AsNoTracking()
                    .Where(pt => pt.Points > 0 && childIds.Contains(pt.ChildId))
                    .SumAsync(pt => (long?)pt.Points) ?? 0L);
            var totalPointsSpentOnGifts = childIds.Count == 0 ? 0L
                : (long)(await _context.ChildGifts.AsNoTracking()
                    .Where(cg => childIds.Contains(cg.ChildId))
                    .SumAsync(cg => (long?)cg.PointsSpent) ?? 0L);

            // ── Recent Activity ───────────────────────────────────────────────────
            var newChildrenEnrolled = await _context.Childrens.AsNoTracking()
                .CountAsync(c => c.InstitutionId == institutionId && c.CreatedAt >= weekStart);
            var tasksCompletedRecently = allTaskRows.Count(r => r.Status == Enums.Tasks.TaskStatus.Completed && r.CompletedAt >= weekStart);
            var adventureProgressCompletedRecently = waIds.Count == 0 ? 0
                : await _context.ChildAdventureProgresses.AsNoTracking()
                    .CountAsync(p => waIds.Contains(p.WeeklyAdventureId) && p.IsCompleted && p.CompletedAt >= weekStart);
            var levelUpsRecently = childIds.Count == 0 ? 0
                : await _context.Notifications.AsNoTracking()
                    .CountAsync(n => n.Type == NotificationType.LevelUp && n.CreatedAt >= weekStart && childIds.Contains(n.UserId));
            var giftsPurchasedRecently = childIds.Count == 0 ? 0
                : await _context.ChildGifts.AsNoTracking()
                    .CountAsync(cg => childIds.Contains(cg.ChildId) && cg.PurchasedAt >= weekStart);
            var tasksReviewedRecently = allTaskRows.Count(r =>
                (r.Status == Enums.Tasks.TaskStatus.Completed || r.Status == Enums.Tasks.TaskStatus.Rejected)
                && r.UpdatedAt >= weekStart);

            // ── Assemble ──────────────────────────────────────────────────────────
            var response = new InstitutionDashboardResponse
            {
                GeneratedAt = now,
                InstitutionId = institutionId,
                InstitutionName = admin.Institution.Name,
                InstitutionCreatedAt = admin.Institution.CreatedAt,
                Overview = new InstOverviewStats
                {
                    TotalClasses = totalClasses, TotalChildren = totalChildren,
                    TotalSupervisors = totalSupervisors, ChildrenInClasses = childrenInClasses,
                    ChildrenWithNoClass = totalChildren - childrenInClasses,
                    ActiveWeeklyAdventures = activeWeeklyAdventures,
                    TasksAwaitingReview = tasksAwaitingReview
                },
                Classes = new InstClassStats
                {
                    TotalClasses = totalClasses,
                    ClassesWithSupervisor = supClassDict.Count(kv => kv.Value > 0),
                    ClassesWithNoSupervisor = totalClasses - supClassDict.Count(kv => kv.Value > 0),
                    ClassesWithActiveAdventure = classesWithActiveAdventureSet.Count,
                    AverageChildrenPerClass = totalClasses == 0 ? 0.0 : Math.Round(childrenInClasses / (double)totalClasses, 1),
                    ClassBreakdown = classBreakdown
                },
                Supervisors = new InstSupervisorStats
                {
                    TotalSupervisors = totalSupervisors,
                    SupervisorsWithClasses = supervisorsWithClasses,
                    SupervisorsWithNoClass = totalSupervisors - supervisorsWithClasses,
                    TotalPendingReviews = tasksAwaitingReview,
                    SupervisorBreakdown = supervisorBreakdown
                },
                Children = new InstChildrenStats
                {
                    TotalEnrolled = totalChildren, EnrolledInClass = childrenInClasses,
                    NotInAnyClass = totalChildren - childrenInClasses,
                    WithZeroCompletedTasks = totalChildren - childrenWithCompletedTask,
                    WithZeroPoints = childrenWithZeroPoints, WithNoLevel = childrenWithNoLevel,
                    AveragePointsPerChild = Math.Round(avgPointsPerChild, 1),
                    LevelDistribution = levelDistribution, TopChildren = topChildEntries
                },
                Adventures = new InstAdventureStats
                {
                    TotalAdventures = adventureEntities.Count,
                    ActiveAdventures = adventureEntities.Count(a => a.Status == AdventureStatus.Active),
                    InactiveAdventures = adventureEntities.Count(a => a.Status == AdventureStatus.Inactive),
                    TotalWeeklyAssignments = waList.Count,
                    ActiveWeeklyAssignments = waList.Count(wa => wa.Status == WeeklyAdventureStatus.Active),
                    TotalParticipatingChildren = totalParticipating,
                    OverallCompletionRate = overallAdventureCompletionRate,
                    AverageStarsEarned = avgStarsOverall,
                    AdventureBreakdown = adventureBreakdown
                },
                Tasks = new InstTaskStats
                {
                    TotalAssigned = instTotalTasks,
                    StatusBreakdown = new TaskStatusBreakdown
                    {
                        Pending = instPending, InProgress = instInProgress,
                        ReviewRequested = tasksAwaitingReview, Completed = instCompleted, Rejected = instRejected
                    },
                    OverallCompletionRate = taskCompletionRate,
                    AverageTasksCompletedPerChild = avgTasksPerChild,
                    OldestPendingReviewAgeHours = oldestReviewAgeHours
                },
                PointsAndLevels = new InstPointsStats
                {
                    TotalPointsEarned = totalPointsEarned,
                    TotalPointsSpentOnGifts = totalPointsSpentOnGifts,
                    AveragePointsPerChild = Math.Round(avgPointsPerChild, 1),
                    ChildrenWithZeroPoints = childrenWithZeroPoints,
                    ChildrenWithNoLevel = childrenWithNoLevel,
                    LevelDistribution = levelDistribution
                },
                RecentActivity = new InstRecentActivityStats
                {
                    PeriodDays = 7, NewChildrenEnrolled = newChildrenEnrolled,
                    TasksCompleted = tasksCompletedRecently,
                    AdventureProgressCompleted = adventureProgressCompletedRecently,
                    LevelUps = levelUpsRecently, GiftsPurchased = giftsPurchasedRecently,
                    TasksReviewed = tasksReviewedRecently
                }
            };

            sw.Stop();
            _logger.LogInformation("Institution dashboard built in {Elapsed}ms for {InstitutionId}", sw.ElapsedMilliseconds, institutionId);
            _cache.Set(key, response, CacheDuration);
            return response;
        }
    }
}
