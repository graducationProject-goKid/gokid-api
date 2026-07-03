namespace GoKidAPI.DTO.Institution.Responses
{
    public class SupervisorProfileResponse
    {
        public string Id { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public string InstitutionId { get; set; } = null!;
        public string InstitutionName { get; set; } = null!;
        public List<AssignedClassInfo> AssignedClasses { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public class AssignedClassInfo
    {
        public string ClassId { get; set; } = null!;
        public string ClassName { get; set; } = null!;
        public int ChildrenCount { get; set; }
    }
}
