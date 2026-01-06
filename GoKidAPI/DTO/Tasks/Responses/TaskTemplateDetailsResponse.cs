using GoKidAPI.Enums.Tasks;

namespace GoKidAPI.DTO.Tasks.Responses
{
    public class TaskTemplateDetailsResponse
    {
        public string Id { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string TitleAr { get; set; } = null!;
        public string TitleEn { get; set; } = null!;
        public string DescriptionAr { get; set; } = null!;
        public string DescriptionEn { get; set; } = null!;
        public string? IconUrl { get; set; }
        public string SubCategoryId { get; set; } = null!;
        public string SubCategoryNameEn { get; set; } = null!;
        public DifficultyLevel Difficulty { get; set; }
        public int BasePoints { get; set; }
        public TaskTemplateType TemplateType { get; set; }
        public DateTime CreatedAt { get; set; }

        // Common for Question types
        public string? QuestionText { get; set; }
        public string? TaskImageUrl { get; set; }
        public string? ExpectedCorrectAnswer { get; set; }
        public bool? CaseSensitive { get; set; }

        // Voice only
        public string? VoicePrompt { get; set; }
        public int? MaxVoiceAttempts { get; set; }
        public int? MaxVoiceDurationSeconds { get; set; }

        // Evidence only
        public string? InstructionsText { get; set; }
        public EvidenceType? EvidenceType { get; set; }
        public ReviewAuthority? ReviewBy { get; set; }
    }
}
