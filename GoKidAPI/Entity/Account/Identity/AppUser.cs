using GoKidAPI.Entity.Account.Users;
using GoKidAPI.Entity.Institiution;
using GoKidAPI.Enums;

using Microsoft.AspNetCore.Identity;

using NPOI.POIFS.Properties;

namespace GoKidAPI.Entity.Account.Identity
{
    public class AppUser : IdentityUser
    {
        public string? DisplayName { get; set; }
        public string? AvatarUrl { get; set; }
        public UserType UserType { get; set; }
        //public string? FcmToken { get; set; } // For push notifications
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
