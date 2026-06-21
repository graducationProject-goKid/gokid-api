// Services/TTSService/StoryTtsService.cs
namespace GoKidAPI.Services.TTSService
{
    public class StoryTtsService : IStoryTtsService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<StoryTtsService> _logger;

        public StoryTtsService(
            IHttpClientFactory httpClientFactory,
            ILogger<StoryTtsService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<byte[]> ConvertStoryToSpeechAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Story text cannot be empty");

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(5);

            var formData = new Dictionary<string, string>
            {
                { "text", text }
            };

            var response = await client.PostAsync(
                "https://waad-moaness-kokoro-tts-api.hf.space/generate-audio",
                new FormUrlEncodedContent(formData));

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "Story TTS FAILED\nStatusCode: {StatusCode}\nReason: {Reason}\nBody: {Body}",
                    response.StatusCode,
                    response.ReasonPhrase,
                    errorBody);

                throw new Exception($"Story TTS failed: {response.StatusCode} - {errorBody}");
            }

            return await response.Content.ReadAsByteArrayAsync();
        }
    }
}