namespace GoKidAPI.Services.TTSService
{
    public interface ITextToSpeechService
    {
        Task<byte[]> ConvertTextToSpeechAsync(string text);
    }
}
