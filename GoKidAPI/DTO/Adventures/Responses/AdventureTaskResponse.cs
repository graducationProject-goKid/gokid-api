namespace GoKidAPI.DTO.Adventures.Responses
{
    public class AdventureTaskResponse
    {
        public string Id { get; set; } = null!;
        public int DayNumber { get; set; }
        public string TitleAr { get; set; } = null!;
        public string TitleEn { get; set; } = null!;
        public string? StoryText { get; set; }
        public string? StoryVoiceUrl { get; set; }
        public int Stars { get; set; }
    }
}
