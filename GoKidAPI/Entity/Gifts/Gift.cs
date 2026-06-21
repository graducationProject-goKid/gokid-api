using GoKidAPI.Entity.Base;
using GoKidAPI.Enums.Gifts;

namespace GoKidAPI.Entity.Gifts
{
    public class Gift : AuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string NameEn { get; set; } = null!;
        public string NameAr { get; set; } = null!;
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImagePublicId { get; set; }
        public int PointsCost { get; set; }
        public GiftType Type { get; set; }
        public GiftStatus Status { get; set; } = GiftStatus.Active;

        public ICollection<ChildGift> ChildGifts { get; set; } = new List<ChildGift>();
    }
}
