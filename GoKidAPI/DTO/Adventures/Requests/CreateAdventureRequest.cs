using System.ComponentModel.DataAnnotations;

using GoKidAPI.Migrations;

namespace GoKidAPI.DTO.Adventures.Requests
{
    public class CreateAdventureRequest
    {
        [Required]
        public string Title { get; set; } = null!;

        [Required]
        public string TitleEn { get; set; } = null!;

        [Required]
        public string TitleAr { get; set; } = null!;

        [Required]
        public string DescriptionEn { get; set; } = null!;

        [Required]
        public string DescriptionAr { get; set; } = null!;

        [Required]
        public string GoalEn { get; set; } = null!;

        [Required]
        public string GoalAr { get; set; } = null!;

        public IFormFile? BannerImage { get; set; }

        public int WeekDuration { get; set; } = 7;
        public int BonusPoints { get; set; } = 50;

        // لو عايز يرفع voice جاهز بدل الـ Text-to-Voice
        public IFormFile? DescriptionVoiceFile { get; set; }

        public List<CreateAdventureTaskRequest> Tasks { get; set; } = new List<CreateAdventureTaskRequest>();
    }
}
