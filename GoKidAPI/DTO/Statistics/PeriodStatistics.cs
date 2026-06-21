namespace GoKidAPI.DTO.Statistics
{
    public class PeriodStatistics
    {
        public int EarnedPoints { get; set; }
        public int CompletedTasks { get; set; }
        public double AvgMinutes { get; set; } = 0;           // placeholder
        public int ConsecutiveDays { get; set; } = 0;         // placeholder
        public int TotalTasks { get; set; }
        public int RefusedTasks { get; set; }
        public int InReviewTasks { get; set; }
        public int CompletedTasksCount { get; set; }
    }
}
