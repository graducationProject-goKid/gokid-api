namespace GoKidAPI.DTO.Account.Auth.Responses
{
    public class AuthResponse
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string UserType { get; set; }

        // For child
        public string? ChildId { get; set; }
        public string? ParentId { get; set; }
    }
}
