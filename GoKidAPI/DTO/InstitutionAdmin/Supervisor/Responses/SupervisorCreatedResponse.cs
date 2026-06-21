namespace GoKidAPI.DTO.InstitutionAdmin.Supervisor.Responses
{
    public class SupervisorCreatedResponse
    {
        public string SupervisorId { get; set; } = null!;
        public string AppUserId { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string InstitutionName { get; set; } = null!;
        public string AvatarUrl { get; set; } = null!;
    }
}
