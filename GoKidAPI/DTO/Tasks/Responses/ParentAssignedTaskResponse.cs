namespace GoKidAPI.DTO.Tasks.Responses
{
    public class ParentAssignedTaskResponse
    {
        public string ChildTaskId { get; set; } = null!;
        public string TaskTemplateId { get; set; } = null!;
        public string TitleAr { get; set; } = null!;
        public string TitleEn { get; set; } = null!;
        public string? DescriptionAr { get; set; }
        public string? DescriptionEn { get; set; }
        public string? subCategoryNameEn { get; set; }
        public string? IconUrl { get; set; }
        public int Points { get; set; }
        //public TaskVerificationType VerificationType { get; set; }
        public DateTime AssignedAt { get; set; }
        public string? TemplateType { get; set; }
        public DateTime? DueDate { get; set; }
        public Enums.Tasks.TaskStatus Status { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? RejectionReason { get; set; }  // لو تم رفضها من الـ Parent
    }
}
