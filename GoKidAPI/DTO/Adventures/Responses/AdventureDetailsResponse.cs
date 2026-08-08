using GoKidAPI.Enums.Adventures;

namespace GoKidAPI.DTO.Adventures.Responses
{
    public class AdventureDetailsResponse
    {
        public string Id { get; set; } = null!;
        public string TitleEn { get; set; } = null!;
        public string TitleAr { get; set; } = null!;

        public string DescriptionEn { get; set; } = null!;
        public string DescriptionAr { get; set; } = null!;

        public string GoalEn { get; set; } = null!;
        public string GoalAr { get; set; } = null!; 
        public string? DescriptionVoiceUrl { get; set; }
        public int WeekDuration { get; set; }
        public int BonusPoints { get; set; }
        public AdventureStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // for child app to know if the adventure is accessible or not (based on status and dates)
        public bool IsAccessible { get; set; } = true;
        public string? AccessMessage { get; set; }

        public List<AdventureTaskDetailResponse> Tasks { get; set; } = new();
    }

    public class AdventureTaskDetailResponse
    {
        public string Id { get; set; } = null!;
        public int DayNumber { get; set; }
        public string TitleAr { get; set; } = null!;
        public string TitleEn { get; set; } = null!;
        public string? StoryText { get; set; }
        public string? StoryVoiceUrl { get; set; }
        public int Stars { get; set; }
        public string templateType { get; set; } = null!;
    }
}
