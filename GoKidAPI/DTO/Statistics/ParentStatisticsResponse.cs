using GoKidAPI.DTO.Levels.Responses;

namespace GoKidAPI.DTO.Statistics
{
    public class ParentStatisticsResponse
    {
        public string ChildId { get; set; } = null!;
        public string ChildName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public int TotalPoints { get; set; }
        public LevelInfo? Level { get; set; }
        public string? ChildCode { get; set; } = null!;

        public PeriodStatistics ThisWeek { get; set; } = new();
        public PeriodStatistics ThisMonth { get; set; } = new();
        public PeriodStatistics AllTime { get; set; } = new();

        public PerformanceSummary Performance { get; set; } = new();
    }
}
