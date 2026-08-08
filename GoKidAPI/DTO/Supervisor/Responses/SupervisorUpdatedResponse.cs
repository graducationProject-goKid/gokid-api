namespace GoKidAPI.DTO.Supervisor.Responses
{
    public class SupervisorUpdatedResponse
    {
        public string SupervisorId { get; set; } = null!;
        public string AppUserId { get; set; } = null!;
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public string InstitutionName { get; set; } = null!;
    }
}
