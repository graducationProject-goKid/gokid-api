using GoKidAPI.Enums.Tasks;

namespace GoKidAPI.DTO.Tasks.Requests
{
    public class CreateVoiceQuestionRequest : CreateBaseTaskRequest
    {
        public string QuestionText { get; set; } = null!;
        public string ExpectedCorrectAnswer { get; set; } = null!;
        public string? VoicePrompt { get; set; }
        public int MaxVoiceAttempts { get; set; } = 3;
        public int MaxVoiceDurationSeconds { get; set; } = 10;
    }
}
