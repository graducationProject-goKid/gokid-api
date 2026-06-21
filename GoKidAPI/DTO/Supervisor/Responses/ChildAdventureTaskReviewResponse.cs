using GoKidAPI.Enums.Adventures;

namespace GoKidAPI.DTO.Supervisor.Responses
{
    public class ChildAdventureTaskReviewResponse
    {
        public string ChildAdventureTaskId { get; set; } = null!;
        public string ChildId { get; set; } = null!;
        public string ChildName { get; set; } = null!;
        public string? ChildAvatarUrl { get; set; }
        public int DayNumber { get; set; }
        public string TaskTitleEn { get; set; } = null!;
        public string TaskTitleAr { get; set; } = null!;
        public string? EvidenceUrl { get; set; }
        public AdventureChildTaskStatus Status { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public bool? IsApproved { get; set; }
        public string? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }
}
