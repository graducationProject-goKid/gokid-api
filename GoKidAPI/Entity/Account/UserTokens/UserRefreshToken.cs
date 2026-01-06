using System.ComponentModel.DataAnnotations;

using GoKidAPI.Entity.Account.Identity;

namespace GoKidAPI.Entity.Account.UserTokens
{
    public class UserRefreshToken
    {
        [Key]
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public string? Token { get; set; }
        public DateTime UsedAt { get; set; }
        public bool IsUsed { get; set; }
        public DateTime ExpiryDateUtc { get; set; }
        public virtual AppUser? User { get; set; }
    }
}
