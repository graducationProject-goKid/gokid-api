namespace GoKidAPI.DTO.Dashboard
{
    public class SupervisorDashboardResponse
    {
        public DateTime GeneratedAt { get; set; }
        public string SupervisorId { get; set; } = null!;
        public string SupervisorName { get; set; } = null!;
        public SupervisorOverviewStats Overview { get; set; } = new();
        public SupervisorClassStats Classes { get; set; } = new();
        public SupervisorPendingReviewStats PendingReviews { get; set; } = new();
        public SupervisorChildrenProgressStats ChildrenProgress { get; set; } = new();
        public SupervisorAdventureStats Adventures { get; set; } = new();
        public SupervisorTaskPerformanceStats TaskPerformance { get; set; } = new();
        public SupervisorRecentActivityStats RecentActivity { get; set; } = new();
    }

    // ─── Overview ─────────────────────────────────────────────────────────────────

    public class SupervisorOverviewStats
    {
        public int TotalClassesSupervised { get; set; }
        public int TotalChildrenSupervised { get; set; }
        public int TasksAwaitingMyReview { get; set; }
        public int OldestPendingReviewHours { get; set; }
        public int ActiveAdventuresInMyClasses { get; set; }
        public int TasksCompletedThisWeek { get; set; }
        public int LevelUpsThisWeek { get; set; }
    }

    // ─── Classes ──────────────────────────────────────────────────────────────────

    public class SupervisorClassStats
    {
        public int TotalClasses { get; set; }
        public int ClassesWithActiveAdventure { get; set; }
        public int ClassesWithPendingReviews { get; set; }
        public List<SupervisorClassEntry> ClassBreakdown { get; set; } = new();
    }

    public class SupervisorClassEntry
    {
        public string ClassId { get; set; } = null!;
        public string ClassName { get; set; } = null!;
        public string InstitutionName { get; set; } = null!;
        public int ChildrenCount { get; set; }
        public int PendingReviewCount { get; set; }
        public double TaskCompletionRate { get; set; }
        public bool HasActiveAdventure { get; set; }
        public double AdventureCompletionRate { get; set; }
    }

    // ─── Pending Reviews ──────────────────────────────────────────────────────────

    public class SupervisorPendingReviewStats
    {
        public int TotalPending { get; set; }
        public int OldestPendingHoursAgo { get; set; }
        public TaskTypeBreakdown ByTaskType { get; set; } = new();
        public List<PendingReviewByClassEntry> ByClass { get; set; } = new();
        public List<RecentlySubmittedTaskEntry> RecentlySubmitted { get; set; } = new();
    }

    public class PendingReviewByClassEntry
    {
        public string ClassId { get; set; } = null!;
        public string ClassName { get; set; } = null!;
        public int PendingCount { get; set; }
    }

    public class RecentlySubmittedTaskEntry
    {
        public string ChildTaskId { get; set; } = null!;
        public string ChildName { get; set; } = null!;
        public string ClassName { get; set; } = null!;
        public string TaskTitle { get; set; } = null!;
        public string TaskType { get; set; } = null!;
        public DateTime? SubmittedAt { get; set; }
    }

    // ─── Children Progress ────────────────────────────────────────────────────────

    public class SupervisorChildrenProgressStats
    {
        public int TotalChildren { get; set; }
        public int ChildrenWithZeroCompletedTasks { get; set; }
        public int ChildrenWithZeroPoints { get; set; }
        public double AverageCompletionRate { get; set; }
        public double AveragePointsPerChild { get; set; }
        public List<LevelDistributionEntry> LevelDistribution { get; set; } = new();
        public List<ChildPerformanceEntry> TopPerformers { get; set; } = new();
        public List<ChildPerformanceEntry> NeedsAttention { get; set; } = new();
    }

    public class ChildPerformanceEntry
    {
        public string ChildId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public string? ClassName { get; set; }
        public int CompletedTasksCount { get; set; }
        public int TotalPoints { get; set; }
        public string? LevelName { get; set; }
        public int DaysSinceLastActivity { get; set; }
    }

    // ─── Adventures ───────────────────────────────────────────────────────────────

    public class SupervisorAdventureStats
    {
        public int TotalWeeklyAssignments { get; set; }
        public int ActiveWeeklyAssignments { get; set; }
        public int CompletedWeeklyAssignments { get; set; }
        public int ExpiredWeeklyAssignments { get; set; }
        public int TotalParticipatingChildren { get; set; }
        public double OverallCompletionRate { get; set; }
        public double AverageStarsEarned { get; set; }
        public int AdventureTasksPendingReview { get; set; }
        public List<SupervisorCurrentAdventureEntry> CurrentAdventures { get; set; } = new();
    }

    public class SupervisorCurrentAdventureEntry
    {
        public string WeeklyAdventureId { get; set; } = null!;
        public string AdventureTitle { get; set; } = null!;
        public string ClassId { get; set; } = null!;
        public string ClassName { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DaysRemaining { get; set; }
        public int ParticipatingChildren { get; set; }
        public int CompletedChildren { get; set; }
        public double CompletionRate { get; set; }
        public double AverageStars { get; set; }
    }

    // ─── Task Performance ─────────────────────────────────────────────────────────

    public class SupervisorTaskPerformanceStats
    {
        public int TotalAssigned { get; set; }
        public TaskStatusBreakdown StatusBreakdown { get; set; } = new();
        public double CompletionRate { get; set; }
        public double AverageTasksCompletedPerChild { get; set; }
        public TaskTypeBreakdown TaskTypeBreakdown { get; set; } = new();
    }

    // ─── Recent Activity ──────────────────────────────────────────────────────────

    public class SupervisorRecentActivityStats
    {
        public int PeriodDays { get; set; } = 7;
        public int TasksCompleted { get; set; }
        public int TasksReviewedInMyClasses { get; set; }
        public int TasksRejected { get; set; }
        public int AdventureProgressCompleted { get; set; }
        public int LevelUpsInMyClasses { get; set; }
        public int NewChildrenInMyClasses { get; set; }
    }
}
