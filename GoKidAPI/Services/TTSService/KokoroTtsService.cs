namespace GoKidAPI.Services.TTSService
{
    public class KokoroTtsService : ITextToSpeechService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<KokoroTtsService> _logger;

        public KokoroTtsService(IHttpClientFactory httpClientFactory, ILogger<KokoroTtsService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<byte[]> ConvertTextToSpeechAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Text cannot be empty");

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
                var headers = response.Headers.ToString();

                _logger.LogError(
                    "TTS FAILED\nStatusCode: {StatusCode}\nReason: {Reason}\nHeaders: {Headers}\nBody: {Body}",
                    response.StatusCode,
                    response.ReasonPhrase,
                    headers,
                    errorBody
                );

                throw new Exception($"TTS failed: {response.StatusCode} - {errorBody}");
            }

            var bytes = await response.Content.ReadAsByteArrayAsync();
            return bytes;
        }
    }
}
