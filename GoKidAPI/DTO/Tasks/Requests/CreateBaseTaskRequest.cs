using GoKidAPI.Entity.Base;
using GoKidAPI.Enums.Tasks;

namespace GoKidAPI.DTO.Tasks.Requests
{
    public class CreateBaseTaskRequest
    {
        public string TitleAr { get; set; } = null!;
        public string TitleEn { get; set; } = null!;
        public string DescriptionAr { get; set; } = null!;
        public string DescriptionEn { get; set; } = null!;
        public IFormFile? TaskImageFile { get; set; }
        public IFormFile? IconFile { get; set; }
        public string SubCategoryId { get; set; } = null!;
        public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Easy;
        public int BasePoints { get; set; } = 10;
        public int RecommendedAgeFrom { get; set; }
        public int RecommendedAgeTo { get; set; }
    }
}
