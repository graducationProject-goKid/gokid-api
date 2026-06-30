namespace GoKidAPI.DTO.Account.Profile
{
    public class ParentProfileResponse
    {
        public string Id { get; set; } = null!;
        public string? DisplayName { get; set; }
        public string? Email { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public ChildSummary? ActiveChild { get; set; }
    }
}
