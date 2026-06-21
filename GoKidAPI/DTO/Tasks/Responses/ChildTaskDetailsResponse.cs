using GoKidAPI.Enums.Tasks;

namespace GoKidAPI.DTO.Tasks.Responses
{
    public class ChildTaskDetailsResponse
    {
        public string Id { get; set; } = null!;
        public string ChildId { get; set; } = null!;
        public string ChildName { get; set; } = null!;
        public string ChildNickName { get; set; } = null!;

        public string TaskTemplateId { get; set; } = null!;
        public string TitleAr { get; set; } = null!;
        public string TitleEn { get; set; } = null!;
        public string? DescriptionAr { get; set; }
        public string? DescriptionEn { get; set; }
        public string? IconUrl { get; set; }
        public int Points { get; set; }
        public TaskTemplateType TemplateType { get; set; }

        public TaskSource Source { get; set; }
        public string? AssignedByParentId { get; set; }

        public Enums.Tasks.TaskStatus Status { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? ReviewRequestedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? RejectionReason { get; set; }
        public string? ParentAcceptanceMessage { get; set; }  // ← الجديد
        public string? ChildNote { get; set; }               // ← الجديد

        public int AttemptCount { get; set; }
        public string? AnswerText { get; set; }
        public string? AnswerMediaUrl { get; set; }  // صورة أو صوت الإجابة
    }
}
