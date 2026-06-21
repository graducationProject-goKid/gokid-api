namespace GoKidAPI.DTO.Adventures.Responses
{
    public class GenerateStoryResponse
    {
        public string AdventureId { get; set; } = null!;
        public StoryIntroOutro Intro { get; set; } = null!;
        public StoryIntroOutro Outro { get; set; } = null!;
        public List<StoryDayItem> Days { get; set; } = new();
        public bool VoiceProcessingInBackground { get; set; }
    }
    // This class represents a single day's story item in the adventure in the story generation process.
    public class StoryDayItem
    {
        public int DayNumber { get; set; }
        public string AdventureTaskId { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Story { get; set; } = null!;
        public string? VoiceUrl { get; set; }
    }
    // This class represents the introduction or conclusion story of the adventure in the story generation process.
    public class StoryIntroOutro
    {
        public string Title { get; set; } = null!;
        public string Story { get; set; } = null!;
        public string? VoiceUrl { get; set; }
    }
}
