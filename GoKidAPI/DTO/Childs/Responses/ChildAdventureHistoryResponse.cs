using GoKidAPI.DTO.Levels.Responses;

namespace GoKidAPI.DTO.Childs.Responses
{
    public class ChildAdventureHistoryResponse
    {
        public string ChildId { get; set; } = null!;
        public string ChildName { get; set; } = null!;
        public string? ChildAvatarUrl { get; set; }
        public LevelInfo? Level { get; set; }

        // Summary
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int PendingTasks { get; set; }
        public int MissedTasks { get; set; }
        public int EarnedStars { get; set; }
        public int EarnedPoints { get; set; }
        public bool IsAdventureCompleted { get; set; }

        // Task Details
        public List<ChildAdventureTaskDetailResponse> Tasks { get; set; } = new();
    }
}
