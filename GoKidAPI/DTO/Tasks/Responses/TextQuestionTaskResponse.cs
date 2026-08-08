using GoKidAPI.Enums.Tasks;

namespace GoKidAPI.DTO.Tasks.Responses
{
    public class TextQuestionTaskResponse
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
        public TaskTemplateType TemplateType => TaskTemplateType.TextQuestion;
        public DateTime CreatedAt { get; set; }

        public string QuestionText { get; set; } = null!;
        public string? TaskImageUrl { get; set; }
        public string ExpectedCorrectAnswer { get; set; } = null!;
        public bool ?CaseSensitive { get; set; }
    }
}
