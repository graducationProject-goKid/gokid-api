// Services/StoryGeneration/StoryGenerationService.cs
using GoKidAPI.DTO.AdventureStory;
using System.Text.Json;

namespace GoKidAPI.Services.StoryGeneration
{
    public class StoryGenerationService : IStoryGenerationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<StoryGenerationService> _logger;

        private const string ApiUrl = "https://waad-moaness-ai-story-generator.hf.space/generate_story";

        public StoryGenerationService(
            IHttpClientFactory httpClientFactory,
            ILogger<StoryGenerationService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<LlmStoryResponse> GenerateAsync(
            string theme,
            string goal,
            List<string> tasks)
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(5);

            var requestBody = new { theme, goal, tasks };

            var response = await client.PostAsJsonAsync(ApiUrl, requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "Story Generation FAILED\nStatusCode: {StatusCode}\nBody: {Body}",
                    response.StatusCode,
                    error);

                throw new Exception($"Story generation failed: {response.StatusCode} - {error}");
            }

            var json = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<LlmStoryResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result == null)
                throw new Exception("Story generation returned empty response");

            return result;
        }
    }
}