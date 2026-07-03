namespace GoKidAPI.DTO.Dashboard
{
    public class InstitutionDashboardResponse
    {
        public DateTime GeneratedAt { get; set; }
        public string InstitutionId { get; set; } = null!;
        public string InstitutionName { get; set; } = null!;
        public DateTime InstitutionCreatedAt { get; set; }
        public InstOverviewStats Overview { get; set; } = new();
        public InstClassStats Classes { get; set; } = new();
        public InstSupervisorStats Supervisors { get; set; } = new();
        public InstChildrenStats Children { get; set; } = new();
        public InstAdventureStats Adventures { get; set; } = new();
        public InstTaskStats Tasks { get; set; } = new();
        public InstPointsStats PointsAndLevels { get; set; } = new();
        public InstRecentActivityStats RecentActivity { get; set; } = new();
    }

    // ─── Overview ─────────────────────────────────────────────────────────────────

    public class InstOverviewStats
    {
        public int TotalClasses { get; set; }
        public int TotalChildren { get; set; }
        public int TotalSupervisors { get; set; }
        public int ChildrenInClasses { get; set; }
        public int ChildrenWithNoClass { get; set; }
        public int ActiveWeeklyAdventures { get; set; }
        public int TasksAwaitingReview { get; set; }
    }

    // ─── Classes ──────────────────────────────────────────────────────────────────

    public class InstClassStats
    {
        public int TotalClasses { get; set; }
        public int ClassesWithSupervisor { get; set; }
        public int ClassesWithNoSupervisor { get; set; }
        public int ClassesWithActiveAdventure { get; set; }
        public double AverageChildrenPerClass { get; set; }
        public List<InstClassEntry> ClassBreakdown { get; set; } = new();
    }

    public class InstClassEntry
    {
        public string ClassId { get; set; } = null!;
        public string ClassName { get; set; } = null!;
        public int ChildrenCount { get; set; }
        public int SupervisorCount { get; set; }
        public int PendingReviewCount { get; set; }
        public int CompletedTasksCount { get; set; }
        public int TotalTasksCount { get; set; }
        public double TaskCompletionRate { get; set; }
        public bool HasActiveAdventure { get; set; }
    }

    // ─── Supervisors ──────────────────────────────────────────────────────────────

    public class InstSupervisorStats
    {
        public int TotalSupervisors { get; set; }
        public int SupervisorsWithClasses { get; set; }
        public int SupervisorsWithNoClass { get; set; }
        public int TotalPendingReviews { get; set; }
        public List<InstSupervisorEntry> SupervisorBreakdown { get; set; } = new();
    }

    public class InstSupervisorEntry
    {
        public string SupervisorId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int AssignedClassesCount { get; set; }
        public int SupervisedChildrenCount { get; set; }
        public int PendingReviewCount { get; set; }
    }

    // ─── Children ─────────────────────────────────────────────────────────────────

    public class InstChildrenStats
    {
        public int TotalEnrolled { get; set; }
        public int EnrolledInClass { get; set; }
        public int NotInAnyClass { get; set; }
        public int WithZeroCompletedTasks { get; set; }
        public int WithZeroPoints { get; set; }
        public int WithNoLevel { get; set; }
        public double AveragePointsPerChild { get; set; }
        public List<LevelDistributionEntry> LevelDistribution { get; set; } = new();
        public List<InstTopChildEntry> TopChildren { get; set; } = new();
    }

    public class InstTopChildEntry
    {
        public string ChildId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? ClassName { get; set; }
        public int HighestPoints { get; set; }
        public string? LevelName { get; set; }
    }

    // ─── Adventures ───────────────────────────────────────────────────────────────

    public class InstAdventureStats
    {
        public int TotalAdventures { get; set; }
        public int ActiveAdventures { get; set; }
        public int InactiveAdventures { get; set; }
        public int TotalWeeklyAssignments { get; set; }
        public int ActiveWeeklyAssignments { get; set; }
        public int TotalParticipatingChildren { get; set; }
        public double OverallCompletionRate { get; set; }
        public double AverageStarsEarned { get; set; }
        public List<InstAdventureEntry> AdventureBreakdown { get; set; } = new();
    }

    public class InstAdventureEntry
    {
        public string AdventureId { get; set; } = null!;
        public string TitleEn { get; set; } = null!;
        public string Status { get; set; } = null!;
        public int TimesAssigned { get; set; }
        public int ParticipatingChildren { get; set; }
        public double CompletionRate { get; set; }
        public double AverageStars { get; set; }
    }

    // ─── Tasks ────────────────────────────────────────────────────────────────────

    public class InstTaskStats
    {
        public int TotalAssigned { get; set; }
        public TaskStatusBreakdown StatusBreakdown { get; set; } = new();
        public double OverallCompletionRate { get; set; }
        public double AverageTasksCompletedPerChild { get; set; }
        public int OldestPendingReviewAgeHours { get; set; }
    }

    // ─── Points & Levels ──────────────────────────────────────────────────────────

    public class InstPointsStats
    {
        public long TotalPointsEarned { get; set; }
        public long TotalPointsSpentOnGifts { get; set; }
        public double AveragePointsPerChild { get; set; }
        public int ChildrenWithZeroPoints { get; set; }
        public int ChildrenWithNoLevel { get; set; }
        public List<LevelDistributionEntry> LevelDistribution { get; set; } = new();
    }

    // ─── Recent Activity ──────────────────────────────────────────────────────────

    public class InstRecentActivityStats
    {
        public int PeriodDays { get; set; } = 7;
        public int NewChildrenEnrolled { get; set; }
        public int TasksCompleted { get; set; }
        public int AdventureProgressCompleted { get; set; }
        public int LevelUps { get; set; }
        public int GiftsPurchased { get; set; }
        public int TasksReviewed { get; set; }
    }
}
