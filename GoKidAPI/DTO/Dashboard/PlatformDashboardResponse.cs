namespace GoKidAPI.DTO.Dashboard
{
    public class PlatformDashboardResponse
    {
        public DateTime GeneratedAt { get; set; }
        public OverviewStats Overview { get; set; } = new();
        public UserStats Users { get; set; } = new();
        public InstitutionStats Institutions { get; set; } = new();
        public TaskStats Tasks { get; set; } = new();
        public AdventureStats Adventures { get; set; } = new();
        public PointsAndLevelsStats PointsAndLevels { get; set; } = new();
        public GiftStats Gifts { get; set; } = new();
        public RecentActivityStats RecentActivity { get; set; } = new();
    }

    // ─── Overview ────────────────────────────────────────────────────────────────

    public class OverviewStats
    {
        public int TotalUsers { get; set; }
        public int TotalInstitutions { get; set; }
        public int TotalChildren { get; set; }
        public int TotalTaskCompletions { get; set; }
        public int TotalAdventureCompletions { get; set; }
        public long TotalPointsAwarded { get; set; }
        public int NewUsersThisMonth { get; set; }
        public int NewUsersLastMonth { get; set; }
        public double MonthlyGrowthPercent { get; set; }
    }

    // ─── Users ───────────────────────────────────────────────────────────────────

    public class UserStats
    {
        public int TotalParents { get; set; }
        public int TotalChildren { get; set; }
        public int TotalSupervisors { get; set; }
        public int TotalInstitutionAdmins { get; set; }
        public int NewThisWeek { get; set; }
        public int NewThisMonth { get; set; }
        public List<MonthlyRegistrationCount> RegistrationTrend { get; set; } = new();
    }

    public class MonthlyRegistrationCount
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthLabel { get; set; } = null!;
        public int Count { get; set; }
    }

    // ─── Institutions ─────────────────────────────────────────────────────────────

    public class InstitutionStats
    {
        public int Total { get; set; }
        public int WithActiveAdventures { get; set; }
        public int WithNoChildren { get; set; }
        public double AverageChildrenPerInstitution { get; set; }
        public double AverageClassesPerInstitution { get; set; }
        public List<InstitutionSummaryEntry> TopByChildren { get; set; } = new();
    }

    public class InstitutionSummaryEntry
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int ChildrenCount { get; set; }
        public int ClassesCount { get; set; }
        public int ActiveAdventuresCount { get; set; }
    }

    // ─── Tasks ───────────────────────────────────────────────────────────────────

    public class TaskStats
    {
        public int TotalTemplates { get; set; }
        public TaskTypeBreakdown TemplatesByType { get; set; } = new();
        public int TotalAssignments { get; set; }
        public TaskStatusBreakdown AssignmentsByStatus { get; set; } = new();
        public double CompletionRate { get; set; }
        public int PendingReview { get; set; }
        public List<TopTaskTemplateEntry> TopTemplates { get; set; } = new();
    }

    public class TaskTypeBreakdown
    {
        public int InstantReward { get; set; }
        public int TextQuestion { get; set; }
        public int VoiceQuestion { get; set; }
        public int EvidenceSubmission { get; set; }
    }

    public class TaskStatusBreakdown
    {
        public int Pending { get; set; }
        public int InProgress { get; set; }
        public int ReviewRequested { get; set; }
        public int Completed { get; set; }
        public int Rejected { get; set; }
    }

    public class TopTaskTemplateEntry
    {
        public string Id { get; set; } = null!;
        public string TitleEn { get; set; } = null!;
        public string Type { get; set; } = null!;
        public int UsageCount { get; set; }
    }

    // ─── Adventures ──────────────────────────────────────────────────────────────

    public class AdventureStats
    {
        public int TotalAdventures { get; set; }
        public int ActiveAdventures { get; set; }
        public int InactiveAdventures { get; set; }
        public int TotalWeeklyAssignments { get; set; }
        public WeeklyAdventureStatusBreakdown WeeklyByStatus { get; set; } = new();
        public int TotalParticipatingChildren { get; set; }
        public double CompletionRate { get; set; }
        public double AverageStarsEarned { get; set; }
        public List<TopAdventureEntry> TopByParticipation { get; set; } = new();
    }

    public class WeeklyAdventureStatusBreakdown
    {
        public int Active { get; set; }
        public int Completed { get; set; }
        public int Expired { get; set; }
        public int Inactive { get; set; }
    }

    public class TopAdventureEntry
    {
        public string AdventureId { get; set; } = null!;
        public string TitleEn { get; set; } = null!;
        public int AssignedClassesCount { get; set; }
        public int ParticipatingChildrenCount { get; set; }
    }

    // ─── Points & Levels ─────────────────────────────────────────────────────────

    public class PointsAndLevelsStats
    {
        public long TotalPointsAwarded { get; set; }
        public long TotalPointsSpentOnGifts { get; set; }
        public double AveragePointsPerChild { get; set; }
        public int ChildrenWithZeroPoints { get; set; }
        public int ChildrenWithNoLevel { get; set; }
        public List<PointsSourceBreakdownEntry> BySource { get; set; } = new();
        public List<LevelDistributionEntry> LevelDistribution { get; set; } = new();
        public List<TopChildEntry> TopChildren { get; set; } = new();
    }

    public class PointsSourceBreakdownEntry
    {
        public string Source { get; set; } = null!;
        public long TotalPoints { get; set; }
        public int TransactionCount { get; set; }
    }

    public class LevelDistributionEntry
    {
        public string LevelId { get; set; } = null!;
        public string LevelName { get; set; } = null!;
        public int Order { get; set; }
        public int MinPoints { get; set; }
        public int ChildrenCount { get; set; }
        public double Percentage { get; set; }
    }

    public class TopChildEntry
    {
        public string ChildId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public int HighestPoints { get; set; }
        public string? LevelName { get; set; }
        public string? InstitutionName { get; set; }
    }

    // ─── Gifts ───────────────────────────────────────────────────────────────────

    public class GiftStats
    {
        public int TotalGifts { get; set; }
        public int ActiveGifts { get; set; }
        public int InactiveGifts { get; set; }
        public GiftTypeBreakdown ByType { get; set; } = new();
        public int TotalPurchases { get; set; }
        public long TotalPointsSpent { get; set; }
        public List<TopGiftEntry> TopGifts { get; set; } = new();
    }

    public class GiftTypeBreakdown
    {
        public int Badge { get; set; }
        public int Character { get; set; }
        public int Other { get; set; }
    }

    public class TopGiftEntry
    {
        public string GiftId { get; set; } = null!;
        public string NameEn { get; set; } = null!;
        public string Type { get; set; } = null!;
        public int PurchaseCount { get; set; }
        public long PointsSpent { get; set; }
    }

    // ─── Recent Activity ─────────────────────────────────────────────────────────

    public class RecentActivityStats
    {
        public int PeriodDays { get; set; } = 7;
        public int NewUsers { get; set; }
        public int TasksCompleted { get; set; }
        public int AdventuresCompleted { get; set; }
        public int LevelUps { get; set; }
        public int GiftsPurchased { get; set; }
        public int NotificationsSent { get; set; }
    }
}
