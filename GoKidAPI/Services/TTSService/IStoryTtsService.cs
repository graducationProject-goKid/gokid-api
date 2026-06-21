namespace GoKidAPI.Services.TTSService
{
    public interface IStoryTtsService
    {
        Task<byte[]> ConvertStoryToSpeechAsync(string text);

    }
}
