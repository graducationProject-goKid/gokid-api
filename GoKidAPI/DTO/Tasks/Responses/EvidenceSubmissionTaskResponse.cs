using GoKidAPI.Enums.Tasks;

namespace GoKidAPI.DTO.Tasks.Responses
{
    public class EvidenceSubmissionTaskResponse
    {
        public string Id { get; set; } = null!;
        public string TitleAr { get; set; } = null!;
        public string TitleEn { get; set; } = null!;
        public string DescriptionAr { get; set; } = null!;
        public string DescriptionEn { get; set; } = null!;
        public string? IconUrl { get; set; }
        public string SubCategoryId { get; set; } = null!;
        public string SubCategoryNameEn { get; set; } = null!;
        public DifficultyLevel Difficulty { get; set; }
        public int BasePoints { get; set; }
        public int? RecommendedAgeFrom { get; set; }
        public int? RecommendedAgeTo { get; set; }
        public TaskTemplateType TemplateType => TaskTemplateType.EvidenceSubmission;
        public DateTime CreatedAt { get; set; }

        public string InstructionsText { get; set; } = null!;
        public string? TaskImageUrl { get; set; }
        public EvidenceType? EvidenceType { get; set; }
        public ReviewAuthority? ReviewBy { get; set; }
    }
}
