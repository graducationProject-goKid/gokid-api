using GoKidAPI.Enums.Adventures;

namespace GoKidAPI.DTO.Childs.Responses
{
    public class ChildAdventureTaskDetailResponse
    {
        public string ChildAdventureTaskId { get; set; } = null!;
        public int DayNumber { get; set; }
        public string TaskTitleEn { get; set; } = null!;
        public string TaskTitleAr { get; set; } = null!;
        public string? TaskImageUrl { get; set; }
        public string? StoryText { get; set; }
        public string? StoryVoiceUrl { get; set; }
        public AdventureChildTaskStatus Status { get; set; }
        public string? EvidenceUrl { get; set; }
        public int EarnedStars { get; set; }
        public bool? IsApproved { get; set; }
        public string? ReviewedBy { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
