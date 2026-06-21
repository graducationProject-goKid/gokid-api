using GoKidAPI.Enums.Adventures;

namespace GoKidAPI.DTO.Adventures.Responses
{
    public class AdventureListItemResponse
    {
        public string Id { get; set; } = null!;
        public string TitleEn { get; set; } = null!;
        public string TitleAr { get; set; } = null!;

        public string DescriptionEn { get; set; } = null!;
        public string DescriptionAr { get; set; } = null!;
        public string? DescriptionVoiceUrl { get; set; }
        public int WeekDuration { get; set; }
        public int BonusPoints { get; set; }
        public AdventureStatus Status { get; set; }
        public int TasksCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
