namespace GoKidAPI.DTO.Adventures.Requests
{
    public class UpdateAdventureRequest
    {
        public string? TitleEn { get; set; }
        public string? TitleAr { get; set; }

        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }

        public string? GoalEn { get; set; }
        public string? GoalAr { get; set; }
        public IFormFile? DescriptionVoiceFile { get; set; }
        public int? WeekDuration { get; set; }
        public int? BonusPoints { get; set; }
    }
}
