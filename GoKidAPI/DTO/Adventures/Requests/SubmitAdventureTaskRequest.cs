using System.ComponentModel.DataAnnotations;

namespace GoKidAPI.DTO.Adventures.Requests
{
    public class SubmitAdventureTaskRequest
    {
        public string AdventureTaskId { get; set; } = null!;
        public string WeeklyAdventureId { get; set; } = null!;
        public IFormFile? EvidenceFile { get; set; }
        public IFormFile? VoiceFile { get; set; }
        public string? Comment { get; set; }
    }
}
