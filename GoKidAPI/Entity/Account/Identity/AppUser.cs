using GoKidAPI.Enums;

using Microsoft.AspNetCore.Identity;

namespace GoKidAPI.Entity.Account.Identity
{
    public class AppUser : IdentityUser
    {
        public string? DisplayName { get; set; }
        public string? AvatarUrl { get; set; }
        public UserType UserType { get; set; }
        public string? FcmToken { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
