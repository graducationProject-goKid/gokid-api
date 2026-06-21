namespace GoKidAPI.DTO.AdventureStory
{
    public class AdventureStoryDetailsResponse
    {
        public string AdventureId { get; set; } = null!;
        public string Title { get; set; } = null!;

        public StoryPart Intro { get; set; } = new();
        public List<StoryDay> Days { get; set; } = new();
        public StoryPart Outro { get; set; } = new();
    }

    public class StoryPart
    {
        public string Title { get; set; } = null!;
        public string Story { get; set; } = null!;
        public string? VoiceUrl { get; set; }
    }

    public class StoryDay
    {
        public int DayNumber { get; set; }
        public string AdventureTaskId { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Story { get; set; } = null!;
        public string? VoiceUrl { get; set; }
    }
}
