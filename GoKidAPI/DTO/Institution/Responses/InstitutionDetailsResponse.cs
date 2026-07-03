namespace GoKidAPI.DTO.Institution.Responses
{
    public class InstitutionDetailsResponse
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? LogoUrl { get; set; }
        public string? Website { get; set; }
        public string? Description { get; set; }
        public string AdminId { get; set; } = null!;
        public string AdminName { get; set; } = null!;
        public string AdminEmail { get; set; } = null!;
        public int ClassCount { get; set; }
        public int StudentCount { get; set; }
        public int SupervisorCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<SupervisorSummary> Supervisors { get; set; } = new();
        public List<ClassSummary> Classes { get; set; } = new();
    }

    public class ClassSummary
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int ChildrenCount { get; set; }
        public int SupervisorsCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SupervisorSummary
    {
        public string Id { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public int AssignedClassesCount { get; set; }
    }
}
