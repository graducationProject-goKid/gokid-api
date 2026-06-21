using GoKidAPI.Entity.Account.Users;
using GoKidAPI.Entity.Base;

namespace GoKidAPI.Entity.Gifts
{
    public class ChildGift : AuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ChildId { get; set; } = null!;
        public Child Child { get; set; } = null!;
        public string GiftId { get; set; } = null!;
        public Gift Gift { get; set; } = null!;
        public int PointsSpent { get; set; }
        public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;
    }
}
