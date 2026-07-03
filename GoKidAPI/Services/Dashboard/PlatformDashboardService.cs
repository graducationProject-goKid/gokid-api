using GoKidAPI.Data;
using GoKidAPI.DTO.Dashboard;
using GoKidAPI.Enums;
using GoKidAPI.Enums.Adventures;
using GoKidAPI.Enums.Gifts;
using GoKidAPI.Enums.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace GoKidAPI.Services.Dashboard
{
    public class PlatformDashboardService : IPlatformDashboardService
    {
        private const string CacheKey = "platform_dashboard_v1";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<PlatformDashboardService> _logger;

        public PlatformDashboardService(
            AppDbContext context,
            IMemoryCache cache,
            ILogger<PlatformDashboardService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public void InvalidateCache() => _cache.Remove(CacheKey);

        public async Task<PlatformDashboardResponse> GetDashboardAsync()
        {
            if (_cache.TryGetValue(CacheKey, out PlatformDashboardResponse? cached) && cached != null)
            {
                _logger.LogDebug("Platform dashboard served from cache");
                return cached;
            }

            _logger.LogInformation("Building platform dashboard...");
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var now = DateTime.UtcNow;
            var thisMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var lastMonthStart = thisMonthStart.AddMonths(-1);
            var weekStart = now.AddDays(-7);
            var sixMonthsAgo = now.AddMonths(-6);

            // ── Overview ─────────────────────────────────────────────────────────
            var totalUsers = await _context.AppUsers.CountAsync();
            var totalInstitutions = await _context.Institutions.CountAsync(i => !i.IsDeleted);
            var totalChildren = await _context.Childrens.CountAsync();
            var totalTaskCompletions = await _context.ChildTasks
                .CountAsync(ct => ct.Status == Enums.Tasks.TaskStatus.Completed);
            var totalAdventureCompletions = await _context.ChildAdventureProgresses
                .CountAsync(cap => cap.IsCompleted);
            var totalPointsAwarded = (long)(await _context.PointsTransactions
                .Where(pt => pt.Points > 0)
                .SumAsync(pt => (long?)pt.Points) ?? 0L);
            var newUsersThisMonth = await _context.AppUsers
                .CountAsync(u => u.CreatedAt >= thisMonthStart);
            var newUsersLastMonth = await _context.AppUsers
                .CountAsync(u => u.CreatedAt >= lastMonthStart && u.CreatedAt < thisMonthStart);

            double monthlyGrowth = newUsersLastMonth == 0
                ? (newUsersThisMonth > 0 ? 100.0 : 0.0)
                : Math.Round((newUsersThisMonth - newUsersLastMonth) / (double)newUsersLastMonth * 100, 1);

            // ── Users ─────────────────────────────────────────────────────────────
            var totalParents = await _context.AppUsers.CountAsync(u => u.UserType == UserType.Parent);
            var totalChildrenUsers = await _context.AppUsers.CountAsync(u => u.UserType == UserType.Child);
            var totalSupervisors = await _context.AppUsers.CountAsync(u => u.UserType == UserType.Supervisor);
            var totalInstitutionAdmins = await _context.AppUsers.CountAsync(u => u.UserType == UserType.InstitutionAdmin);
            var newUsersThisWeek = await _context.AppUsers.CountAsync(u => u.CreatedAt >= weekStart);

            var registrationRaw = await _context.AppUsers
                .AsNoTracking()
                .Where(u => u.CreatedAt >= sixMonthsAgo)
                .Select(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                .ToListAsync();

            var registrationTrend = registrationRaw
                .GroupBy(u => new { u.Year, u.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new MonthlyRegistrationCount
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    MonthLabel = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    Count = g.Count()
                })
                .ToList();

            // ── Institutions ──────────────────────────────────────────────────────
            var institutionEntities = await _context.Institutions
                .AsNoTracking()
                .Where(i => !i.IsDeleted)
                .Select(i => new { i.Id, i.Name })
                .ToListAsync();

            var institutionIds = institutionEntities.Select(i => i.Id).ToList();

            var childrenPerInst = await _context.Childrens
                .AsNoTracking()
                .Where(c => c.InstitutionId != null && institutionIds.Contains(c.InstitutionId!))
                .GroupBy(c => c.InstitutionId!)
                .Select(g => new { InstitutionId = g.Key, Count = g.Count() })
                .ToListAsync();

            var classesPerInst = await _context.Classes
                .AsNoTracking()
                .Where(c => !c.IsDeleted && institutionIds.Contains(c.InstitutionId))
                .GroupBy(c => c.InstitutionId)
                .Select(g => new { InstitutionId = g.Key, Count = g.Count() })
                .ToListAsync();

            var activeWaPerInst = await _context.WeeklyAdventures
                .AsNoTracking()
                .Where(wa => !wa.IsDeleted && wa.Status == WeeklyAdventureStatus.Active)
                .GroupBy(wa => wa.Class.InstitutionId)
                .Select(g => new { InstitutionId = g.Key, Count = g.Count() })
                .ToListAsync();

            var childrenInstDict = childrenPerInst.ToDictionary(x => x.InstitutionId, x => x.Count);
            var classesInstDict = classesPerInst.ToDictionary(x => x.InstitutionId, x => x.Count);
            var activeWaDict = activeWaPerInst.ToDictionary(x => x.InstitutionId, x => x.Count);

            var instData = institutionEntities.Select(i => new
            {
                i.Id,
                i.Name,
                ChildrenCount = childrenInstDict.GetValueOrDefault(i.Id, 0),
                ClassesCount = classesInstDict.GetValueOrDefault(i.Id, 0),
                ActiveAdventuresCount = activeWaDict.GetValueOrDefault(i.Id, 0)
            }).ToList();

            var topByChildren = instData
                .OrderByDescending(i => i.ChildrenCount)
                .Take(5)
                .Select(i => new InstitutionSummaryEntry
                {
                    Id = i.Id,
                    Name = i.Name,
                    ChildrenCount = i.ChildrenCount,
                    ClassesCount = i.ClassesCount,
                    ActiveAdventuresCount = i.ActiveAdventuresCount
                })
                .ToList();

            // ── Tasks ──────────────────────────────────────────────────────────────
            var totalTemplates = await _context.TaskTemplates.CountAsync();
            var templatesInstant = await _context.TaskTemplates.CountAsync(t => t.TemplateType == TaskTemplateType.InstantReward);
            var templatesText = await _context.TaskTemplates.CountAsync(t => t.TemplateType == TaskTemplateType.TextQuestion);
            var templatesVoice = await _context.TaskTemplates.CountAsync(t => t.TemplateType == TaskTemplateType.VoiceQuestion);
            var templatesEvidence = await _context.TaskTemplates.CountAsync(t => t.TemplateType == TaskTemplateType.EvidenceSubmission);

            var totalAssignments = await _context.ChildTasks.CountAsync();
            var tasksPending = await _context.ChildTasks.CountAsync(ct => ct.Status == Enums.Tasks.TaskStatus.Pending);
            var tasksInProgress = await _context.ChildTasks.CountAsync(ct => ct.Status == Enums.Tasks.TaskStatus.InProgress);
            var tasksReviewRequested = await _context.ChildTasks.CountAsync(ct => ct.Status == Enums.Tasks.TaskStatus.ReviewRequested);
            var tasksCompleted = await _context.ChildTasks.CountAsync(ct => ct.Status == Enums.Tasks.TaskStatus.Completed);
            var tasksRejected = await _context.ChildTasks.CountAsync(ct => ct.Status == Enums.Tasks.TaskStatus.Rejected);

            double taskCompletionRate = totalAssignments == 0 ? 0.0
                : Math.Round(tasksCompleted / (double)totalAssignments * 100, 1);

            // Top 5 most-used task templates
            var topTemplateRaw = await _context.ChildTasks
                .AsNoTracking()
                .GroupBy(ct => ct.TaskTemplateId)
                .Select(g => new { TemplateId = g.Key, UsageCount = g.Count() })
                .OrderByDescending(x => x.UsageCount)
                .Take(5)
                .ToListAsync();

            var topTemplateIds = topTemplateRaw.Select(x => x.TemplateId).ToList();
            var topTemplateDetails = await _context.TaskTemplates
                .AsNoTracking()
                .Where(t => topTemplateIds.Contains(t.Id))
                .Select(t => new { t.Id, t.TitleEn, t.TemplateType })
                .ToListAsync();

            var topTemplates = topTemplateRaw
                .Join(topTemplateDetails, r => r.TemplateId, d => d.Id,
                    (r, d) => new TopTaskTemplateEntry
                    {
                        Id = d.Id,
                        TitleEn = d.TitleEn,
                        Type = d.TemplateType.ToString(),
                        UsageCount = r.UsageCount
                    })
                .ToList();

            // ── Adventures ────────────────────────────────────────────────────────
            var totalAdventures = await _context.Adventures.CountAsync(a => !a.IsDeleted);
            var activeAdventures = await _context.Adventures
                .CountAsync(a => !a.IsDeleted && a.Status == AdventureStatus.Active);
            var inactiveAdventures = totalAdventures - activeAdventures;

            var totalWeeklyAssignments = await _context.WeeklyAdventures.CountAsync(wa => !wa.IsDeleted);
            var waActive = await _context.WeeklyAdventures.CountAsync(wa => !wa.IsDeleted && wa.Status == WeeklyAdventureStatus.Active);
            var waCompleted = await _context.WeeklyAdventures.CountAsync(wa => !wa.IsDeleted && wa.Status == WeeklyAdventureStatus.Completed);
            var waExpired = await _context.WeeklyAdventures.CountAsync(wa => !wa.IsDeleted && wa.Status == WeeklyAdventureStatus.Expired);
            var waInactive = await _context.WeeklyAdventures.CountAsync(wa => !wa.IsDeleted && wa.Status == WeeklyAdventureStatus.Inactive);

            var totalProgressRecords = await _context.ChildAdventureProgresses.AsNoTracking().CountAsync();
            var completedProgressRecords = await _context.ChildAdventureProgresses.AsNoTracking().CountAsync(p => p.IsCompleted);
            var participatingChildren = await _context.ChildAdventureProgresses
                .AsNoTracking()
                .Select(p => p.ChildId)
                .Distinct()
                .CountAsync();
            var avgStars = await _context.ChildAdventureProgresses
                .AsNoTracking()
                .AverageAsync(p => (double?)p.EarnedStars) ?? 0.0;

            double adventureCompletionRate = totalProgressRecords == 0 ? 0.0
                : Math.Round(completedProgressRecords / (double)totalProgressRecords * 100, 1);

            // Top 5 adventures by participating children count (through weekly assignments)
            var topAdventureRaw = await _context.WeeklyAdventures
                .AsNoTracking()
                .Where(wa => !wa.IsDeleted)
                .GroupBy(wa => new { wa.AdventureId, wa.Adventure.TitleEn })
                .Select(g => new
                {
                    g.Key.AdventureId,
                    g.Key.TitleEn,
                    AssignedClassesCount = g.Count(),
                    ParticipatingChildrenCount = g.SelectMany(wa => wa.Progresses).Select(p => p.ChildId).Distinct().Count()
                })
                .OrderByDescending(x => x.ParticipatingChildrenCount)
                .Take(5)
                .ToListAsync();

            var topAdventures = topAdventureRaw.Select(x => new TopAdventureEntry
            {
                AdventureId = x.AdventureId,
                TitleEn = x.TitleEn,
                AssignedClassesCount = x.AssignedClassesCount,
                ParticipatingChildrenCount = x.ParticipatingChildrenCount
            }).ToList();

            // ── Points & Levels ───────────────────────────────────────────────────
            var totalPointsSpentOnGifts = (long)(await _context.ChildGifts
                .AsNoTracking()
                .SumAsync(cg => (long?)cg.PointsSpent) ?? 0L);

            var avgPointsPerChild = await _context.Childrens
                .AsNoTracking()
                .AverageAsync(c => (double?)c.TotalPoints) ?? 0.0;

            var childrenWithZeroPoints = await _context.Childrens.CountAsync(c => c.TotalPoints == 0);
            var childrenWithNoLevel = await _context.Childrens.CountAsync(c => c.LevelId == null);

            // Points by source — group in memory to avoid enum string conversion edge cases
            var pointsTransactionRaw = await _context.PointsTransactions
                .AsNoTracking()
                .Where(pt => pt.Points > 0)
                .Select(pt => new { pt.SourceType, pt.Points })
                .ToListAsync();

            var bySource = pointsTransactionRaw
                .GroupBy(pt => pt.SourceType)
                .Select(g => new PointsSourceBreakdownEntry
                {
                    Source = g.Key.ToString(),
                    TotalPoints = g.Sum(x => (long)x.Points),
                    TransactionCount = g.Count()
                })
                .OrderByDescending(x => x.TotalPoints)
                .ToList();

            // Level distribution
            var allLevels = await _context.Levels
                .AsNoTracking()
                .OrderBy(l => l.Order)
                .Select(l => new { l.Id, l.Name, l.Order, l.MinPoints })
                .ToListAsync();

            var childrenPerLevel = await _context.Childrens
                .AsNoTracking()
                .Where(c => c.LevelId != null)
                .GroupBy(c => c.LevelId!)
                .Select(g => new { LevelId = g.Key, Count = g.Count() })
                .ToListAsync();

            var levelChildDict = childrenPerLevel.ToDictionary(x => x.LevelId, x => x.Count);
            var totalChildrenForLevelPct = childrenPerLevel.Sum(x => x.Count);

            var levelDistribution = allLevels.Select(l => new LevelDistributionEntry
            {
                LevelId = l.Id,
                LevelName = l.Name,
                Order = l.Order,
                MinPoints = l.MinPoints,
                ChildrenCount = levelChildDict.GetValueOrDefault(l.Id, 0),
                Percentage = totalChildrenForLevelPct == 0 ? 0.0
                    : Math.Round(levelChildDict.GetValueOrDefault(l.Id, 0) / (double)totalChildrenForLevelPct * 100, 1)
            }).ToList();

            // Top 10 children by highest points
            var topChildEntities = await _context.Childrens
                .AsNoTracking()
                .Include(c => c.Level)
                .Include(c => c.Institution)
                .OrderByDescending(c => c.HighestPoints)
                .Take(10)
                .ToListAsync();

            var topChildren = topChildEntities.Select(c => new TopChildEntry
            {
                ChildId = c.Id,
                Name = c.Name,
                AvatarUrl = c.AvatarUrl,
                HighestPoints = c.HighestPoints,
                LevelName = c.Level?.Name,
                InstitutionName = c.Institution?.Name
            }).ToList();

            // ── Gifts ─────────────────────────────────────────────────────────────
            var totalGifts = await _context.Gifts.AsNoTracking().CountAsync(g => !g.IsDeleted);
            var activeGifts = await _context.Gifts.AsNoTracking().CountAsync(g => !g.IsDeleted && g.Status == GiftStatus.Active);
            var inactiveGifts = totalGifts - activeGifts;

            var giftBadge = await _context.Gifts.AsNoTracking().CountAsync(g => !g.IsDeleted && g.Type == GiftType.Badge);
            var giftCharacter = await _context.Gifts.AsNoTracking().CountAsync(g => !g.IsDeleted && g.Type == GiftType.Character);
            var giftOther = await _context.Gifts.AsNoTracking().CountAsync(g => !g.IsDeleted && g.Type == GiftType.Other);

            var totalPurchases = await _context.ChildGifts.AsNoTracking().CountAsync();
            var totalPointsSpent = totalPointsSpentOnGifts; // reuse

            var topGiftRaw = await _context.ChildGifts
                .AsNoTracking()
                .GroupBy(cg => cg.GiftId)
                .Select(g => new { GiftId = g.Key, PurchaseCount = g.Count(), PointsSpent = g.Sum(x => (long)x.PointsSpent) })
                .OrderByDescending(x => x.PurchaseCount)
                .Take(5)
                .ToListAsync();

            var topGiftIds = topGiftRaw.Select(x => x.GiftId).ToList();
            var topGiftDetails = await _context.Gifts
                .AsNoTracking()
                .Where(g => topGiftIds.Contains(g.Id))
                .Select(g => new { g.Id, g.NameEn, g.Type })
                .ToListAsync();

            var topGifts = topGiftRaw
                .Join(topGiftDetails, r => r.GiftId, d => d.Id,
                    (r, d) => new TopGiftEntry
                    {
                        GiftId = d.Id,
                        NameEn = d.NameEn,
                        Type = d.Type.ToString(),
                        PurchaseCount = r.PurchaseCount,
                        PointsSpent = r.PointsSpent
                    })
                .ToList();

            // ── Recent Activity (last 7 days) ─────────────────────────────────────
            var recentNewUsers = await _context.AppUsers.CountAsync(u => u.CreatedAt >= weekStart);
            var recentTasksCompleted = await _context.ChildTasks
                .CountAsync(ct => ct.Status == Enums.Tasks.TaskStatus.Completed && ct.CompletedAt >= weekStart);
            var recentAdventuresCompleted = await _context.ChildAdventureProgresses
                .AsNoTracking()
                .CountAsync(p => p.IsCompleted && p.CompletedAt >= weekStart);
            var recentLevelUps = await _context.Notifications
                .AsNoTracking()
                .CountAsync(n => n.Type == NotificationType.LevelUp && n.CreatedAt >= weekStart);
            var recentGiftsPurchased = await _context.ChildGifts
                .AsNoTracking()
                .CountAsync(cg => cg.PurchasedAt >= weekStart);
            var recentNotifications = await _context.Notifications
                .AsNoTracking()
                .CountAsync(n => n.CreatedAt >= weekStart);

            // ── Assemble ──────────────────────────────────────────────────────────
            var response = new PlatformDashboardResponse
            {
                GeneratedAt = now,
                Overview = new OverviewStats
                {
                    TotalUsers = totalUsers,
                    TotalInstitutions = totalInstitutions,
                    TotalChildren = totalChildren,
                    TotalTaskCompletions = totalTaskCompletions,
                    TotalAdventureCompletions = totalAdventureCompletions,
                    TotalPointsAwarded = totalPointsAwarded,
                    NewUsersThisMonth = newUsersThisMonth,
                    NewUsersLastMonth = newUsersLastMonth,
                    MonthlyGrowthPercent = monthlyGrowth
                },
                Users = new UserStats
                {
                    TotalParents = totalParents,
                    TotalChildren = totalChildrenUsers,
                    TotalSupervisors = totalSupervisors,
                    TotalInstitutionAdmins = totalInstitutionAdmins,
                    NewThisWeek = newUsersThisWeek,
                    NewThisMonth = newUsersThisMonth,
                    RegistrationTrend = registrationTrend
                },
                Institutions = new InstitutionStats
                {
                    Total = institutionEntities.Count,
                    WithActiveAdventures = activeWaDict.Count,
                    WithNoChildren = instData.Count(i => i.ChildrenCount == 0),
                    AverageChildrenPerInstitution = institutionEntities.Count == 0 ? 0.0
                        : Math.Round(instData.Average(i => (double)i.ChildrenCount), 1),
                    AverageClassesPerInstitution = institutionEntities.Count == 0 ? 0.0
                        : Math.Round(instData.Average(i => (double)i.ClassesCount), 1),
                    TopByChildren = topByChildren
                },
                Tasks = new TaskStats
                {
                    TotalTemplates = totalTemplates,
                    TemplatesByType = new TaskTypeBreakdown
                    {
                        InstantReward = templatesInstant,
                        TextQuestion = templatesText,
                        VoiceQuestion = templatesVoice,
                        EvidenceSubmission = templatesEvidence
                    },
                    TotalAssignments = totalAssignments,
                    AssignmentsByStatus = new TaskStatusBreakdown
                    {
                        Pending = tasksPending,
                        InProgress = tasksInProgress,
                        ReviewRequested = tasksReviewRequested,
                        Completed = tasksCompleted,
                        Rejected = tasksRejected
                    },
                    CompletionRate = taskCompletionRate,
                    PendingReview = tasksReviewRequested,
                    TopTemplates = topTemplates
                },
                Adventures = new AdventureStats
                {
                    TotalAdventures = totalAdventures,
                    ActiveAdventures = activeAdventures,
                    InactiveAdventures = inactiveAdventures,
                    TotalWeeklyAssignments = totalWeeklyAssignments,
                    WeeklyByStatus = new WeeklyAdventureStatusBreakdown
                    {
                        Active = waActive,
                        Completed = waCompleted,
                        Expired = waExpired,
                        Inactive = waInactive
                    },
                    TotalParticipatingChildren = participatingChildren,
                    CompletionRate = adventureCompletionRate,
                    AverageStarsEarned = Math.Round(avgStars, 2),
                    TopByParticipation = topAdventures
                },
                PointsAndLevels = new PointsAndLevelsStats
                {
                    TotalPointsAwarded = totalPointsAwarded,
                    TotalPointsSpentOnGifts = totalPointsSpentOnGifts,
                    AveragePointsPerChild = Math.Round(avgPointsPerChild, 1),
                    ChildrenWithZeroPoints = childrenWithZeroPoints,
                    ChildrenWithNoLevel = childrenWithNoLevel,
                    BySource = bySource,
                    LevelDistribution = levelDistribution,
                    TopChildren = topChildren
                },
                Gifts = new GiftStats
                {
                    TotalGifts = totalGifts,
                    ActiveGifts = activeGifts,
                    InactiveGifts = inactiveGifts,
                    ByType = new GiftTypeBreakdown
                    {
                        Badge = giftBadge,
                        Character = giftCharacter,
                        Other = giftOther
                    },
                    TotalPurchases = totalPurchases,
                    TotalPointsSpent = totalPointsSpent,
                    TopGifts = topGifts
                },
                RecentActivity = new RecentActivityStats
                {
                    PeriodDays = 7,
                    NewUsers = recentNewUsers,
                    TasksCompleted = recentTasksCompleted,
                    AdventuresCompleted = recentAdventuresCompleted,
                    LevelUps = recentLevelUps,
                    GiftsPurchased = recentGiftsPurchased,
                    NotificationsSent = recentNotifications
                }
            };

            sw.Stop();
            _logger.LogInformation("Platform dashboard built in {Elapsed}ms", sw.ElapsedMilliseconds);

            _cache.Set(CacheKey, response, CacheDuration);
            return response;
        }
    }
}
