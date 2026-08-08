namespace GoKidAPI.DTO.Institution.Responses
{
    public class InstitutionListItemResponse
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? LogoUrl { get; set; }
        public string AdminName { get; set; } = null!;
        public string AdminEmail { get; set; } = null!;
        public string AdminPhoneNumber { get; set; } = null!;
        public string? Website { get; set; }
        public int ClassCount { get; set; }
        public int StudentCount { get; set; }
        public int SupervisorCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
