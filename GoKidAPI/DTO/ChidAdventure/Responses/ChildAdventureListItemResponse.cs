using GoKidAPI.Enums.Adventures;

namespace GoKidAPI.DTO.ChidAdventure.Responses
{
    public class ChildAdventureListItemResponse
    {
        public string WeeklyAdventureId { get; set; } = null!;
        public string AdventureId { get; set; } = null!;
        public string TitleEn { get; set; } = null!;
        public string TitleAr { get; set; } = null!;
        public string? BannerImageUrl { get; set; }
        public string? DescriptionVoiceUrl { get; set; }
        public string DescriptionEn { get; set; }
        public string DescriptionAr { get; set; }
        public int TotalDays { get; set; }
        public int CurrentDay { get; set; }
        public int BonusPoints { get; set; }
        public WeeklyAdventureStatus Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Child Progress
        public int CompletedTasksCount { get; set; }
        public int TotalTasksCount { get; set; }
        public int EarnedStars { get; set; }
        public int EarnedPoints { get; set; }
        public bool IsCompleted { get; set; }

        public List<AdventureDayStatusResponse> DaysStatus { get; set; } = new();
    }
}
