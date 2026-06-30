using GoKidAPI.Enums.Gifts;

namespace GoKidAPI.DTO.Gifts.Responses
{
    public class ChildGiftResponse
    {
        public string ChildGiftId { get; set; } = null!;
        public string GiftId { get; set; } = null!;
        public string NameEn { get; set; } = null!;
        public string NameAr { get; set; } = null!;
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }
        public string? ImageUrl { get; set; }
        public int PointsCost { get; set; }
        public GiftType Type { get; set; }
        public int PointsSpent { get; set; }
        public DateTime PurchasedAt { get; set; }
    }
}
