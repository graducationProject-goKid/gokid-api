namespace GoKidAPI.DTO.Classes.Responses
{
    // Response for both Assign & Remove
    public class SupervisorAssignmentResponse
    {
        public string ClassId { get; set; } = null!;
        public string ClassName { get; set; } = null!;
        public string SupervisorId { get; set; } = null!;
        public string SupervisorName { get; set; } = null!;
        public string Message { get; set; } = null!;
    }
}
