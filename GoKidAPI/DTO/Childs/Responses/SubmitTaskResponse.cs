namespace GoKidAPI.DTO.Childs.Responses
{
    public class SubmitTaskResponse
    {
        public string TaskId { get; set; } = null!;
        public Enums.Tasks.TaskStatus Status { get; set; }
        public int? AwardedPoints { get; set; }  // null لو مش InstantReward أو Voice ناجح
        public string Message { get; set; } = null!;

        // خاص بـ VoiceQuestion فقط
        public VoiceShadowingResult? ShadowingResult { get; set; }

        // خاص بـ EvidenceSubmission
        public bool? IsReviewRequested { get; set; }
    }
    public class VoiceShadowingResult
    {
        public List<WordShadowing> Words { get; set; } = new();
        public string ScoreStatus { get; set; } = "Poor"; // "Excellent", "Good", "Poor"
    }

    public class WordShadowing
    {
        public string Word { get; set; } = null!;
        public string Color { get; set; } = "Gray"; // Green / Yellow / Red
    }
    // Used for ai model response
    public class AiShadowingResponse
    {
        public string score_status { get; set; } = null!;
        public List<AiWordStatus> word_status { get; set; } = new();
    }

    public class AiWordStatus
    {
        public string word { get; set; } = null!;
        public string status { get; set; } = null!;
    }
}
