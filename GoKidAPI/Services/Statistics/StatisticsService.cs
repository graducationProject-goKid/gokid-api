using GoKidAPI.Data;
using GoKidAPI.DTO.Statistics;
using GoKidAPI.Entity.Tasks;
using GoKidAPI.Enums;
using GoKidAPI.Shared;

using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Services.Statistics
{
    public class StatisticsService : IStatisticsService
    {
        private readonly AppDbContext _context;
        private readonly ResponseHandler _response;
        private readonly ILogger<StatisticsService> _logger;

        public StatisticsService(
            AppDbContext context,
            ResponseHandler response,
            ILogger<StatisticsService> logger)
        {
            _context = context;
            _response = response;
            _logger = logger;
        }

        public async Task<Response<ParentStatisticsResponse>> GetParentStatisticsAsync(
    string userId,
    string userRole,
    StatisticsPeriod period = StatisticsPeriod.ThisWeek)
        {
            var childId = await ResolveChildIdAsync(userId, userRole);
            if (childId == null)
                return _response.NotFound<ParentStatisticsResponse>(
                    userRole == "Parent" ? "No child linked to this parent" : "Child not found");

            var child = await _context.Childrens
                .FirstOrDefaultAsync(c => c.Id == childId && !c.IsDeleted);

            if (child == null)
                return _response.NotFound<ParentStatisticsResponse>("Child not found");

            var baseQuery = _context.ChildTasks
                .Where(ct => ct.ChildId == child.Id && !ct.IsDeleted);

            var now = DateTime.UtcNow;
            var thisWeekStart = now.Date.AddDays(-(int)now.DayOfWeek);
            var monthStart = new DateTime(now.Year, now.Month, 1);

            var stats = new ParentStatisticsResponse
            {
                ChildId = child.Id,
                ChildName = child.Name,
                AvatarUrl = child.AvatarUrl,
                TotalPoints = child.TotalPoints,
                ChildCode = child.RegistrationCode,
                CurrentLevel = "1" // Placeholder for current level logic
            };

            stats.ThisWeek = await CalculatePeriodStatistics(baseQuery, thisWeekStart, now);
            stats.ThisMonth = await CalculatePeriodStatistics(baseQuery, monthStart, now);
            stats.AllTime = await CalculatePeriodStatistics(baseQuery, DateTime.MinValue, now);

            if (period == StatisticsPeriod.ThisWeek)
            {
                var previousWeekStart = thisWeekStart.AddDays(-7);
                var previousWeekStats = await CalculatePeriodStatistics(baseQuery, previousWeekStart, thisWeekStart);

                stats.Performance = new PerformanceSummary
                {
                    ImprovementPercentage = previousWeekStats.EarnedPoints == 0
                        ? 100
                        : (double)(stats.ThisWeek.EarnedPoints - previousWeekStats.EarnedPoints)
                            / previousWeekStats.EarnedPoints * 100,

                    CurrentPeriod = new PeriodComparison
                    {
                        EarnedPoints = stats.ThisWeek.EarnedPoints,
                        CompletedTasks = stats.ThisWeek.CompletedTasks
                    },

                    PreviousPeriod = new PeriodComparison
                    {
                        EarnedPoints = previousWeekStats.EarnedPoints,
                        CompletedTasks = previousWeekStats.CompletedTasks
                    }
                };
            }

            return _response.Success(stats, "Statistics retrieved successfully");
        }

        // ========== Private Helpers ==========

        private async Task<string?> ResolveChildIdAsync(string userId, string userRole)
        {
            if (userRole == "Parent")
            {
                var child = await _context.Childrens
                    .FirstOrDefaultAsync(c => c.ParentId == userId && !c.IsDeleted);
                return child?.Id;
            }

            var exists = await _context.Childrens
                .AnyAsync(c => c.Id == userId && !c.IsDeleted);
            return exists ? userId : null;
        }


        private async Task<PeriodStatistics> CalculatePeriodStatistics(
            IQueryable<ChildTask> query,
            DateTime start,
            DateTime end)
        {
            var filtered = query
                .Where(ct => ct.AssignedAt >= start && ct.AssignedAt <= end);

            var completedTasksQuery = filtered
                .Where(ct => ct.Status == Enums.Tasks.TaskStatus.Completed);

            var earnedPoints = await completedTasksQuery
                .SumAsync(ct => (int?)ct.Template.BasePoints) ?? 0;

            var completedTasks = await completedTasksQuery.CountAsync();

            var totalTasks = await filtered.CountAsync();

            var refusedTasks = await filtered.CountAsync(ct =>
                ct.Status == Enums.Tasks.TaskStatus.Rejected);

            var inReviewTasks = await filtered.CountAsync(ct =>
                ct.Status == Enums.Tasks.TaskStatus.ReviewRequested);

            return new PeriodStatistics
            {
                EarnedPoints = earnedPoints,
                CompletedTasks = completedTasks,
                TotalTasks = totalTasks,
                RefusedTasks = refusedTasks,
                InReviewTasks = inReviewTasks
            };
        }
    }
}
