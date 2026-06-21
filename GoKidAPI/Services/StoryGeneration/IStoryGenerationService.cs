// Services/StoryGeneration/IStoryGenerationService.cs
using GoKidAPI.DTO.AdventureStory;

namespace GoKidAPI.Services.StoryGeneration
{
    public interface IStoryGenerationService
    {
        Task<LlmStoryResponse> GenerateAsync(string theme, string goal, List<string> tasks);
    }
}