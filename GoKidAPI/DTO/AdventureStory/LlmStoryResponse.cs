namespace GoKidAPI.DTO.AdventureStory
{
    public class LlmStoryResponse
    {
        public LlmStorySection Intro { get; set; } = null!;
        public List<LlmStoryDay> Days { get; set; } = new();
        public LlmStorySection Outro { get; set; } = null!;
    }
}
