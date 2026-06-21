using GoKidAPI.DTO.Adventures.Responses;
using GoKidAPI.Enums.Adventures;

namespace GoKidAPI.DTO.ChidAdventure.Responses
{
    public class ChildAdventureDetailsResponse
    {
        public string WeeklyAdventureId { get; set; } = null!;
        public string AdventureId { get; set; } = null!;
        public string TitleEn { get; set; } = null!;
        public string TitleAr { get; set; } = null!;
        public string? BannerImageUrl { get; set; }
        public string DescriptionEn { get; set; } = null!;
        public string DescriptionAr { get; set; } = null!;
        public string GoalEn { get; set; } = null!;
        public string GoalAr { get; set; } = null!;
        public string? DescriptionVoiceUrl { get; set; }
        public int BonusPoints { get; set; }
        public int TotalDays { get; set; }
        public int CurrentDay { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public WeeklyAdventureStatus Status { get; set; }

        // Child Progress Summary
        public int CompletedTasksCount { get; set; }
        public int EarnedStars { get; set; }
        public int EarnedPoints { get; set; }
        public bool IsCompleted { get; set; }

        // Tasks
        public List<ChildAdventureTaskItemResponse> Tasks { get; set; } = new();
    }
}
