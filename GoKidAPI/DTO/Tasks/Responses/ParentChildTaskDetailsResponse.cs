using GoKidAPI.Enums.Tasks;

namespace GoKidAPI.DTO.Tasks.Responses
{
    // This for parent portal to review the task
    public class ParentChildTaskDetailsResponse
    {
        public string ChildTaskId { get; set; } = null!;
        public string ChildId { get; set; } = null!;
        public string ChildName { get; set; } = null!;
        public string? ChildNickName { get; set; }

        public string TaskTemplateId { get; set; } = null!;
        public string TitleAr { get; set; } = null!;
        public string TitleEn { get; set; } = null!;
        public string? DescriptionAr { get; set; }
        public string? DescriptionEn { get; set; }
        public string? IconUrl { get; set; }
        public int Points { get; set; }
        public TaskTemplateType TemplateType { get; set; }

        public Enums.Tasks.TaskStatus Status { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? ReviewRequestedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public string? ChildNote { get; set; }                  // ملاحظة الطفل
        public string? AnswerMediaUrl { get; set; }            // رابط الصورة أو الدليل
        public string? RejectionReason { get; set; }           // سبب الرفض لو مرفوض
        public string? ParentAcceptanceMessage { get; set; }   // رسالة القبول لو وافق سابقًا
    }
}
