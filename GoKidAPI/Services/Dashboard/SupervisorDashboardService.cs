using GoKidAPI.Data;
using GoKidAPI.DTO.Dashboard;
using GoKidAPI.Enums;
using GoKidAPI.Enums.Adventures;
using GoKidAPI.Enums.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace GoKidAPI.Services.Dashboard
{
    // Intermediate projection types
    file sealed class SupTaskRow
    {
        public string ChildId { get; set; } = "";
        public string? ClassId { get; set; }
        public Enums.Tasks.TaskStatus Status { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? ReviewRequestedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string TemplateType { get; set; } = "";
    }

    file sealed class SupProgressRow
    {
        public string WAId { get; set; } = "";
        public string ChildId { get; set; } = "";
        public bool IsCompleted { get; set; }
        public int Stars { get; set; }
    }

    file sealed class SupWaRow
    {
        public string Id { get; set; } = "";
        public string ClassId { get; set; } = "";
        public string AdventureId { get; set; } = "";
        public string TitleEn { get; set; } = "";
        public string ClassName { get; set; } = "";
        public WeeklyAdventureStatus Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class SupervisorDashboardService : ISupervisorDashboardService
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(3);
        private static string CacheKey(string supervisorId) => $"sup_dashboard_{supervisorId}";

        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SupervisorDashboardService> _logger;

        public SupervisorDashboardService(AppDbContext context, IMemoryCache cache, ILogger<SupervisorDashboardService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public void InvalidateCache(string supervisorId) => _cache.Remove(CacheKey(supervisorId));

        public async Task<SupervisorDashboardResponse?> GetDashboardAsync(string supervisorUserId)
        {
            var supervisor = await _context.Supervisors.AsNoTracking()
                .Include(s => s.AppUser)
                .FirstOrDefaultAsync(s => s.AppUserId == supervisorUserId && !s.IsDeleted);

            if (supervisor == null) return null;

            var key = CacheKey(supervisor.Id);
            if (_cache.TryGetValue(key, out SupervisorDashboardResponse? cached) && cached != null)
            {
                _logger.LogDebug("Supervisor dashboard served from cache for {SupervisorId}", supervisor.Id);
                return cached;
            }

            _logger.LogInformation("Building supervisor dashboard for {SupervisorId}", supervisor.Id);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var now = DateTime.UtcNow;
            var weekStart = now.AddDays(-7);
            var supervisorName = supervisor.AppUser?.DisplayName ?? supervisor.AppUser?.UserName ?? "Supervisor";

            // ── Base IDs ───────────────────────────────────────────────────────────
            var classIds = await _context.ClassSupervisors.AsNoTracking()
                .Where(cs => cs.SupervisorId == supervisor.Id && !cs.IsDeleted)
                .Select(cs => cs.ClassId)
                .ToListAsync();

            if (classIds.Count == 0)
                return BuildEmptyDashboard(supervisor.Id, supervisorName, now);

            var classEntities = await _context.Classes.AsNoTracking()
                .Where(c => classIds.Contains(c.Id) && !c.IsDeleted)
                .Select(c => new { c.Id, c.Name, InstitutionName = c.Institution.Name })
                .ToListAsync();

            var childIds = await _context.Childrens.AsNoTracking()
                .Where(c => c.ClassId != null && classIds.Contains(c.ClassId!))
                .Select(c => c.Id)
                .ToListAsync();

            // ── Bulk data loads ────────────────────────────────────────────────────
            List<SupTaskRow> allTasks;
            if (childIds.Count == 0)
            {
                allTasks = new();
            }
            else
            {
                allTasks = (await _context.ChildTasks.AsNoTracking()
                    .Where(ct => childIds.Contains(ct.ChildId))
                    .Select(ct => new
                    {
                        ct.ChildId, ClassId = ct.Child.ClassId, ct.Status,
                        ct.CompletedAt, ct.ReviewRequestedAt, ct.UpdatedAt,
                        TemplateType = ct.Template.TemplateType.ToString()
                    })
                    .ToListAsync())
                    .Select(x => new SupTaskRow
                    {
                        ChildId = x.ChildId, ClassId = x.ClassId, Status = x.Status,
                        CompletedAt = x.CompletedAt, ReviewRequestedAt = x.ReviewRequestedAt,
                        UpdatedAt = x.UpdatedAt, TemplateType = x.TemplateType
                    })
                    .ToList();
            }

            List<SupWaRow> waEntities;
            if (classIds.Count == 0)
            {
                waEntities = new();
            }
            else
            {
                waEntities = (await _context.WeeklyAdventures.AsNoTracking()
                    .Where(wa => !wa.IsDeleted && classIds.Contains(wa.ClassId))
                    .Select(wa => new
                    {
                        wa.Id, wa.ClassId, wa.AdventureId, TitleEn = wa.Adventure.TitleEn,
                        ClassName = wa.Class.Name, wa.Status, wa.StartDate, wa.EndDate
                    })
                    .ToListAsync())
                    .Select(x => new SupWaRow
                    {
                        Id = x.Id, ClassId = x.ClassId, AdventureId = x.AdventureId,
                        TitleEn = x.TitleEn, ClassName = x.ClassName, Status = x.Status,
                        StartDate = x.StartDate, EndDate = x.EndDate
                    })
                    .ToList();
            }

            var waIds = waEntities.Select(wa => wa.Id).ToList();

            List<SupProgressRow> allProgress;
            if (waIds.Count == 0)
            {
                allProgress = new();
            }
            else
            {
                allProgress = (await _context.ChildAdventureProgresses.AsNoTracking()
                    .Where(p => waIds.Contains(p.WeeklyAdventureId))
                    .Select(p => new { p.WeeklyAdventureId, p.ChildId, p.IsCompleted, p.EarnedStars })
                    .ToListAsync())
                    .Select(x => new SupProgressRow { WAId = x.WeeklyAdventureId, ChildId = x.ChildId, IsCompleted = x.IsCompleted, Stars = x.EarnedStars })
                    .ToList();
            }

            // Pre-computed dictionaries
            var tasksByClass = allTasks.Where(t => t.ClassId != null)
                .GroupBy(t => t.ClassId!).ToDictionary(g => g.Key, g => g.ToList());
            var tasksByChild = allTasks.GroupBy(t => t.ChildId).ToDictionary(g => g.Key, g => g.ToList());
            var progressByWa = allProgress.GroupBy(p => p.WAId).ToDictionary(g => g.Key, g => g.ToList());
            var waByClass = waEntities.GroupBy(wa => wa.ClassId).ToDictionary(g => g.Key, g => g.ToList());
            var activeWaClassSet = waEntities.Where(wa => wa.Status == WeeklyAdventureStatus.Active).Select(wa => wa.ClassId).ToHashSet();

            // ── Overview ───────────────────────────────────────────────────────────
            var totalChildrenSupervised = childIds.Count;
            var tasksAwaitingReview = allTasks.Count(t => t.Status == Enums.Tasks.TaskStatus.ReviewRequested);
            var oldestPendingDate = allTasks
                .Where(t => t.Status == Enums.Tasks.TaskStatus.ReviewRequested && t.ReviewRequestedAt.HasValue)
                .Select(t => t.ReviewRequestedAt).Min();
            int oldestPendingHours = oldestPendingDate.HasValue ? (int)(now - oldestPendingDate.Value).TotalHours : 0;
            var activeAdventuresInClasses = waEntities.Count(wa => wa.Status == WeeklyAdventureStatus.Active);
            var tasksCompletedThisWeek = allTasks.Count(t => t.Status == Enums.Tasks.TaskStatus.Completed && t.CompletedAt >= weekStart);
            var levelUpsThisWeek = childIds.Count == 0 ? 0
                : await _context.Notifications.AsNoTracking()
                    .CountAsync(n => n.Type == NotificationType.LevelUp && n.CreatedAt >= weekStart && childIds.Contains(n.UserId));

            // ── Classes ────────────────────────────────────────────────────────────
            var childrenPerClassRaw = childIds.Count == 0
                ? new Dictionary<string, int>()
                : (await _context.Childrens.AsNoTracking()
                    .Where(c => c.ClassId != null && classIds.Contains(c.ClassId!))
                    .GroupBy(c => c.ClassId!)
                    .Select(g => new { ClassId = g.Key, Count = g.Count() })
                    .ToListAsync())
                    .ToDictionary(x => x.ClassId, x => x.Count);

            var classBreakdown = classEntities.Select(c =>
            {
                var rows = tasksByClass.GetValueOrDefault(c.Id, new List<SupTaskRow>());
                var total = rows.Count;
                var completed = rows.Count(r => r.Status == Enums.Tasks.TaskStatus.Completed);
                var pending = rows.Count(r => r.Status == Enums.Tasks.TaskStatus.ReviewRequested);
                var myWaIds = waByClass.GetValueOrDefault(c.Id, new List<SupWaRow>()).Select(wa => wa.Id).ToList();
                var myProgress = myWaIds.SelectMany(wid => progressByWa.GetValueOrDefault(wid, new List<SupProgressRow>())).ToList();
                var progTotal = myProgress.Count;
                var progCompleted = myProgress.Count(p => p.IsCompleted);
                return new SupervisorClassEntry
                {
                    ClassId = c.Id,
                    ClassName = c.Name,
                    InstitutionName = c.InstitutionName,
                    ChildrenCount = childrenPerClassRaw.GetValueOrDefault(c.Id, 0),
                    PendingReviewCount = pending,
                    TaskCompletionRate = total == 0 ? 0.0 : Math.Round(completed / (double)total * 100, 1),
                    HasActiveAdventure = activeWaClassSet.Contains(c.Id),
                    AdventureCompletionRate = progTotal == 0 ? 0.0 : Math.Round(progCompleted / (double)progTotal * 100, 1)
                };
            }).OrderByDescending(c => c.PendingReviewCount).ToList();

            // ── Pending Reviews ────────────────────────────────────────────────────
            var pendingRows = allTasks.Where(t => t.Status == Enums.Tasks.TaskStatus.ReviewRequested).ToList();
            var pendingByClass = pendingRows.Where(t => t.ClassId != null)
                .GroupBy(t => t.ClassId!)
                .Select(g => new PendingReviewByClassEntry
                {
                    ClassId = g.Key,
                    ClassName = classEntities.FirstOrDefault(c => c.Id == g.Key)?.Name ?? g.Key,
                    PendingCount = g.Count()
                })
                .OrderByDescending(x => x.PendingCount).ToList();

            var pendingByType = pendingRows.GroupBy(t => t.TemplateType).ToDictionary(g => g.Key, g => g.Count());

            List<RecentlySubmittedTaskEntry> recentlySubmitted;
            if (childIds.Count == 0)
            {
                recentlySubmitted = new();
            }
            else
            {
                recentlySubmitted = (await _context.ChildTasks.AsNoTracking()
                    .Where(ct => childIds.Contains(ct.ChildId) && ct.Status == Enums.Tasks.TaskStatus.ReviewRequested)
                    .OrderBy(ct => ct.ReviewRequestedAt)
                    .Take(5)
                    .Select(ct => new
                    {
                        ct.Id, ChildName = ct.Child.Name, ClassName = ct.Child.Class!.Name,
                        TaskTitle = ct.Template.TitleEn, TaskType = ct.Template.TemplateType.ToString(),
                        ct.ReviewRequestedAt
                    })
                    .ToListAsync())
                    .Select(x => new RecentlySubmittedTaskEntry
                    {
                        ChildTaskId = x.Id, ChildName = x.ChildName, ClassName = x.ClassName ?? "",
                        TaskTitle = x.TaskTitle, TaskType = x.TaskType, SubmittedAt = x.ReviewRequestedAt
                    }).ToList();
            }

            // ── Children Progress ──────────────────────────────────────────────────
            var childrenWithZeroPoints = childIds.Count == 0 ? 0
                : await _context.Childrens.AsNoTracking().CountAsync(c => childIds.Contains(c.Id) && c.TotalPoints == 0);
            var avgPointsPerChild = childIds.Count == 0 ? 0.0
                : await _context.Childrens.AsNoTracking()
                    .Where(c => childIds.Contains(c.Id)).AverageAsync(c => (double?)c.TotalPoints) ?? 0.0;

            var childrenWithZeroCompleted = childIds.Count(cid =>
                !tasksByChild.ContainsKey(cid) || !tasksByChild[cid].Any(t => t.Status == Enums.Tasks.TaskStatus.Completed));

            var perChildRates = childIds.Select(cid =>
            {
                var rows = tasksByChild.GetValueOrDefault(cid, new List<SupTaskRow>());
                return rows.Count == 0 ? 0.0 : rows.Count(r => r.Status == Enums.Tasks.TaskStatus.Completed) / (double)rows.Count * 100;
            }).ToList();
            var avgCompletionRate = perChildRates.Count == 0 ? 0.0 : Math.Round(perChildRates.Average(), 1);

            var allLevels = await _context.Levels.AsNoTracking().OrderBy(l => l.Order)
                .Select(l => new { l.Id, l.Name, l.Order, l.MinPoints }).ToListAsync();
            var childLevelGroups = childIds.Count == 0
                ? new Dictionary<string, int>()
                : (await _context.Childrens.AsNoTracking()
                    .Where(c => childIds.Contains(c.Id) && c.LevelId != null)
                    .GroupBy(c => c.LevelId!)
                    .Select(g => new { LevelId = g.Key, Count = g.Count() })
                    .ToListAsync())
                    .ToDictionary(x => x.LevelId, x => x.Count);
            var leveledTotal = childLevelGroups.Values.Sum();
            var levelDistribution = allLevels.Select(l => new LevelDistributionEntry
            {
                LevelId = l.Id, LevelName = l.Name, Order = l.Order, MinPoints = l.MinPoints,
                ChildrenCount = childLevelGroups.GetValueOrDefault(l.Id, 0),
                Percentage = leveledTotal == 0 ? 0.0 : Math.Round(childLevelGroups.GetValueOrDefault(l.Id, 0) / (double)leveledTotal * 100, 1)
            }).ToList();

            var childDetailEntities = childIds.Count == 0
                ? new List<GoKidAPI.Entity.Account.Users.Child>()
                : await _context.Childrens.AsNoTracking()
                    .Include(c => c.Level).Include(c => c.Class)
                    .Where(c => childIds.Contains(c.Id))
                    .ToListAsync();

            var childPerformanceList = childDetailEntities.Select(c =>
            {
                var rows = tasksByChild.GetValueOrDefault(c.Id, new List<SupTaskRow>());
                var completedCount = rows.Count(r => r.Status == Enums.Tasks.TaskStatus.Completed);
                var lastActivity = rows.Count == 0 ? (DateTime?)null
                    : rows.Select(r => r.UpdatedAt ?? r.CompletedAt).Where(d => d.HasValue).Select(d => d!.Value).DefaultIfEmpty(DateTime.MinValue).Max();
                var daysSince = lastActivity.HasValue && lastActivity.Value != DateTime.MinValue ? (int)(now - lastActivity.Value).TotalDays : 999;
                return new ChildPerformanceEntry
                {
                    ChildId = c.Id, Name = c.Name, AvatarUrl = c.AvatarUrl,
                    ClassName = c.Class?.Name, CompletedTasksCount = completedCount,
                    TotalPoints = c.TotalPoints, LevelName = c.Level?.Name, DaysSinceLastActivity = daysSince
                };
            }).ToList();

            var topPerformers = childPerformanceList
                .OrderByDescending(c => c.CompletedTasksCount).ThenByDescending(c => c.TotalPoints).Take(5).ToList();
            var needsAttention = childPerformanceList
                .OrderBy(c => c.CompletedTasksCount).ThenByDescending(c => c.DaysSinceLastActivity).Take(5).ToList();

            // ── Adventures ─────────────────────────────────────────────────────────
            var totalWa = waEntities.Count;
            var activeWa = waEntities.Count(wa => wa.Status == WeeklyAdventureStatus.Active);
            var completedWa = waEntities.Count(wa => wa.Status == WeeklyAdventureStatus.Completed);
            var expiredWa = waEntities.Count(wa => wa.Status == WeeklyAdventureStatus.Expired);
            var totalProgressCount = allProgress.Count;
            var completedProgressCount = allProgress.Count(p => p.IsCompleted);
            var participatingChildren = allProgress.Select(p => p.ChildId).Distinct().Count();
            var avgStars = totalProgressCount == 0 ? 0.0 : Math.Round(allProgress.Average(p => (double)p.Stars), 2);
            double adventureCompletionRate = totalProgressCount == 0 ? 0.0
                : Math.Round(completedProgressCount / (double)totalProgressCount * 100, 1);

            var adventureTasksPendingReview = waIds.Count == 0 ? 0
                : await _context.ChildAdventureTasks.AsNoTracking()
                    .CountAsync(cat => !cat.IsDeleted && waIds.Contains(cat.WeeklyAdventureId) && cat.IsApproved == null && cat.EvidenceUrl != null);

            var currentAdventures = waEntities.Where(wa => wa.Status == WeeklyAdventureStatus.Active).Select(wa =>
            {
                var waProgress = progressByWa.GetValueOrDefault(wa.Id, new List<SupProgressRow>());
                var waTotal = waProgress.Count;
                var waCompleted = waProgress.Count(p => p.IsCompleted);
                return new SupervisorCurrentAdventureEntry
                {
                    WeeklyAdventureId = wa.Id, AdventureTitle = wa.TitleEn,
                    ClassId = wa.ClassId, ClassName = wa.ClassName,
                    StartDate = wa.StartDate, EndDate = wa.EndDate,
                    DaysRemaining = Math.Max(0, (int)(wa.EndDate - now).TotalDays),
                    ParticipatingChildren = waTotal, CompletedChildren = waCompleted,
                    CompletionRate = waTotal == 0 ? 0.0 : Math.Round(waCompleted / (double)waTotal * 100, 1),
                    AverageStars = waTotal == 0 ? 0.0 : Math.Round(waProgress.Average(p => (double)p.Stars), 2)
                };
            }).OrderBy(a => a.DaysRemaining).ToList();

            // ── Task Performance ───────────────────────────────────────────────────
            var totalTasks = allTasks.Count;
            var completedTasks = allTasks.Count(t => t.Status == Enums.Tasks.TaskStatus.Completed);
            var pendingTasks = allTasks.Count(t => t.Status == Enums.Tasks.TaskStatus.Pending);
            var inProgressTasks = allTasks.Count(t => t.Status == Enums.Tasks.TaskStatus.InProgress);
            var rejectedTasks = allTasks.Count(t => t.Status == Enums.Tasks.TaskStatus.Rejected);
            double taskCompletionRate = totalTasks == 0 ? 0.0 : Math.Round(completedTasks / (double)totalTasks * 100, 1);
            double avgTasksPerChild = totalChildrenSupervised == 0 ? 0.0 : Math.Round(completedTasks / (double)totalChildrenSupervised, 1);
            var typeGroups = allTasks.GroupBy(t => t.TemplateType).ToDictionary(g => g.Key, g => g.Count());

            // ── Recent Activity ────────────────────────────────────────────────────
            var recentCompleted = allTasks.Count(t => t.Status == Enums.Tasks.TaskStatus.Completed && t.CompletedAt >= weekStart);
            var recentReviewed = allTasks.Count(t =>
                (t.Status == Enums.Tasks.TaskStatus.Completed || t.Status == Enums.Tasks.TaskStatus.Rejected) && t.UpdatedAt >= weekStart);
            var recentRejected = allTasks.Count(t => t.Status == Enums.Tasks.TaskStatus.Rejected && t.UpdatedAt >= weekStart);
            var recentAdventureCompleted = waIds.Count == 0 ? 0
                : await _context.ChildAdventureProgresses.AsNoTracking()
                    .CountAsync(p => waIds.Contains(p.WeeklyAdventureId) && p.IsCompleted && p.CompletedAt >= weekStart);
            var recentNewChildren = childIds.Count == 0 ? 0
                : await _context.Childrens.AsNoTracking().CountAsync(c => childIds.Contains(c.Id) && c.CreatedAt >= weekStart);

            // ── Assemble ───────────────────────────────────────────────────────────
            var response = new SupervisorDashboardResponse
            {
                GeneratedAt = now,
                SupervisorId = supervisor.Id,
                SupervisorName = supervisorName,
                Overview = new SupervisorOverviewStats
                {
                    TotalClassesSupervised = classIds.Count,
                    TotalChildrenSupervised = totalChildrenSupervised,
                    TasksAwaitingMyReview = tasksAwaitingReview,
                    OldestPendingReviewHours = oldestPendingHours,
                    ActiveAdventuresInMyClasses = activeAdventuresInClasses,
                    TasksCompletedThisWeek = tasksCompletedThisWeek,
                    LevelUpsThisWeek = levelUpsThisWeek
                },
                Classes = new SupervisorClassStats
                {
                    TotalClasses = classIds.Count,
                    ClassesWithActiveAdventure = activeWaClassSet.Count,
                    ClassesWithPendingReviews = classBreakdown.Count(c => c.PendingReviewCount > 0),
                    ClassBreakdown = classBreakdown
                },
                PendingReviews = new SupervisorPendingReviewStats
                {
                    TotalPending = tasksAwaitingReview,
                    OldestPendingHoursAgo = oldestPendingHours,
                    ByTaskType = new TaskTypeBreakdown
                    {
                        InstantReward = pendingByType.GetValueOrDefault(TaskTemplateType.InstantReward.ToString(), 0),
                        TextQuestion = pendingByType.GetValueOrDefault(TaskTemplateType.TextQuestion.ToString(), 0),
                        VoiceQuestion = pendingByType.GetValueOrDefault(TaskTemplateType.VoiceQuestion.ToString(), 0),
                        EvidenceSubmission = pendingByType.GetValueOrDefault(TaskTemplateType.EvidenceSubmission.ToString(), 0)
                    },
                    ByClass = pendingByClass,
                    RecentlySubmitted = recentlySubmitted
                },
                ChildrenProgress = new SupervisorChildrenProgressStats
                {
                    TotalChildren = totalChildrenSupervised,
                    ChildrenWithZeroCompletedTasks = childrenWithZeroCompleted,
                    ChildrenWithZeroPoints = childrenWithZeroPoints,
                    AverageCompletionRate = avgCompletionRate,
                    AveragePointsPerChild = Math.Round(avgPointsPerChild, 1),
                    LevelDistribution = levelDistribution,
                    TopPerformers = topPerformers,
                    NeedsAttention = needsAttention
                },
                Adventures = new SupervisorAdventureStats
                {
                    TotalWeeklyAssignments = totalWa,
                    ActiveWeeklyAssignments = activeWa,
                    CompletedWeeklyAssignments = completedWa,
                    ExpiredWeeklyAssignments = expiredWa,
                    TotalParticipatingChildren = participatingChildren,
                    OverallCompletionRate = adventureCompletionRate,
                    AverageStarsEarned = avgStars,
                    AdventureTasksPendingReview = adventureTasksPendingReview,
                    CurrentAdventures = currentAdventures
                },
                TaskPerformance = new SupervisorTaskPerformanceStats
                {
                    TotalAssigned = totalTasks,
                    StatusBreakdown = new TaskStatusBreakdown
                    {
                        Pending = pendingTasks, InProgress = inProgressTasks,
                        ReviewRequested = tasksAwaitingReview, Completed = completedTasks, Rejected = rejectedTasks
                    },
                    CompletionRate = taskCompletionRate,
                    AverageTasksCompletedPerChild = avgTasksPerChild,
                    TaskTypeBreakdown = new TaskTypeBreakdown
                    {
                        InstantReward = typeGroups.GetValueOrDefault(TaskTemplateType.InstantReward.ToString(), 0),
                        TextQuestion = typeGroups.GetValueOrDefault(TaskTemplateType.TextQuestion.ToString(), 0),
                        VoiceQuestion = typeGroups.GetValueOrDefault(TaskTemplateType.VoiceQuestion.ToString(), 0),
                        EvidenceSubmission = typeGroups.GetValueOrDefault(TaskTemplateType.EvidenceSubmission.ToString(), 0)
                    }
                },
                RecentActivity = new SupervisorRecentActivityStats
                {
                    PeriodDays = 7, TasksCompleted = recentCompleted,
                    TasksReviewedInMyClasses = recentReviewed, TasksRejected = recentRejected,
                    AdventureProgressCompleted = recentAdventureCompleted,
                    LevelUpsInMyClasses = levelUpsThisWeek, NewChildrenInMyClasses = recentNewChildren
                }
            };

            sw.Stop();
            _logger.LogInformation("Supervisor dashboard built in {Elapsed}ms for {SupervisorId}", sw.ElapsedMilliseconds, supervisor.Id);
            _cache.Set(key, response, CacheDuration);
            return response;
        }

        private static SupervisorDashboardResponse BuildEmptyDashboard(string supervisorId, string name, DateTime now) =>
            new SupervisorDashboardResponse
            {
                GeneratedAt = now, SupervisorId = supervisorId, SupervisorName = name,
                Overview = new(), Classes = new(), PendingReviews = new(),
                ChildrenProgress = new(), Adventures = new(), TaskPerformance = new(), RecentActivity = new()
            };
    }
}
