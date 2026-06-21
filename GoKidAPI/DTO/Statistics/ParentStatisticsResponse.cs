namespace GoKidAPI.DTO.Statistics
{
    public class ParentStatisticsResponse
    {
        public string ChildId { get; set; } = null!;
        public string ChildName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public int TotalPoints { get; set; }

        public PeriodStatistics ThisWeek { get; set; } = new();
        public PeriodStatistics ThisMonth { get; set; } = new();
        public PeriodStatistics AllTime { get; set; } = new();

        public PerformanceSummary Performance { get; set; } = new();
    }
}
