using GoKidAPI.Entity.Account.Identity;
using GoKidAPI.Entity.Account.Users;
using GoKidAPI.Entity.Base;
using GoKidAPI.Enums.Gifts;
using System.ComponentModel.DataAnnotations;

namespace GoKidAPI.Entity.Gifts
{
    public class Reward : AuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string NameEn { get; set; } = null!;
        public string NameAr { get; set; } = null!;
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImagePublicId { get; set; }
        public int TargetPoints { get; set; }
        public string? MessageToChild { get; set; }

        // Relation between child and parent
        public string ParentId { get; set; } = null!;
        public AppUser Parent { get; set; } = null!;
        public string ChildId { get; set; } = null!;
        public Child Child { get; set; } = null!;

        public RewardStatus Status { get; set; } = RewardStatus.Pending;
        public DateTime? GivenAt { get; set; }
    }
}
