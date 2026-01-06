namespace GoKidAPI.DTO.Tasks.Requests
{
    public class AssignTaskResponse
    {
        public string ChildTaskId { get; set; } = null!;
        public string TaskTemplateId { get; set; } = null!;
        public string TitleAr { get; set; } = null!;
        public string TitleEn { get; set; } = null!;
        public DateTime? DueDate { get; set; }
        public DateTime AssignedAt { get; set; }
    }
}
