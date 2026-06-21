namespace GoKidAPI.DTO.Statistics
{
    public class PerformanceSummary
    {
        public double ImprovementPercentage { get; set; }     // +25% or -10%
        public string ImprovementText { get; set; } = "";     // "Better than last period"
        public PeriodComparison CurrentPeriod { get; set; } = new();
        public PeriodComparison PreviousPeriod { get; set; } = new();
    }
}
