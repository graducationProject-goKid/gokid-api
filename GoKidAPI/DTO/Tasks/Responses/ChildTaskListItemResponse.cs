namespace GoKidAPI.DTO.Tasks.Responses
{
    public class ChildTaskListItemResponse
    {
        public string ChildTaskId { get; set; } = null!;
        public string ChildId { get; set; } = null!;
        public string ChildName { get; set; } = null!;
        public string TaskTemplateId { get; set; } = null!;
        public string TitleAr { get; set; } = null!;
        public string TitleEn { get; set; } = null!;
        public string DescriptionEn { get; set; } = null!;
        public string CategoryNameAr { get; set; } = null!;
        public string CategoryNameEn { get; set; } = null!;
        public string SubCategoryNameAr { get; set; } = null!;
        public string SubCategoryNameEn { get; set; } = null!;
        public string Difficulty { get; set; } = null!;
        public string? IconUrl { get; set; }
        public int Points { get; set; }
        public Enums.Tasks.TaskStatus Status { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? ReviewRequestedAt { get; set; }
        public string? RejectionReason { get; set; }
        public string? EvidenceUrl { get; set; }
    }
}
