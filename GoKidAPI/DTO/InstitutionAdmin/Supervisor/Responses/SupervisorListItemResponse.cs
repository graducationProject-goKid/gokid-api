namespace GoKidAPI.DTO.InstitutionAdmin.Supervisor.Responses
{
    public class SupervisorListItemResponse
    {
        public string Id { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string AvatarUrl { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public int SupervisedClassesCount { get; set; }
        public string InstitutionName { get; set; } = null!;
        public bool IsAssignedToClass { get; set; } // NEW

    }
}
