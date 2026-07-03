using GoKidAPI.DTO.Levels.Responses;

namespace GoKidAPI.DTO.Childs.Responses
{
    public class ClassChildrenListResponse
    {
        public string ChildId { get; set; } = null!;
        public string ChildName { get; set; } = null!;
        public string? ChildAvatarUrl { get; set; }
        public int Age { get; set; }
        public int SubmittedTasksCount { get; set; }
        public int CompletedTasksCount { get; set; }
        public int TotalTasksCount { get; set; }
        public int EarnedStars { get; set; }
        public int EarnedPoints { get; set; }
        public bool IsAdventureCompleted { get; set; }
        public LevelInfo? Level { get; set; }
    }
}
