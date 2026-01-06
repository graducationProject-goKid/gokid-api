namespace GoKidAPI.DTO.Tasks.Requests
{
    public class AssignTaskRequest
    {
        public string TaskTemplateId { get; set; } = null!;
        public DateTime? DueDate { get; set; }
    }
}
