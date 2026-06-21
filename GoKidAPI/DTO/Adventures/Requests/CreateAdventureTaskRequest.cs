using System.ComponentModel.DataAnnotations;

namespace GoKidAPI.DTO.Adventures.Requests
{
    public class CreateAdventureTaskRequest
    {
        [Required]
        public string TaskTemplateId { get; set; } = null!;

        [Required]
        public int DayNumber { get; set; }   // من 1 إلى 7

        public string? StoryText { get; set; }

        // Voice للـ Story الخاصة بالـ Task (اختياري)
        public IFormFile? StoryVoiceFile { get; set; }
    }
}
