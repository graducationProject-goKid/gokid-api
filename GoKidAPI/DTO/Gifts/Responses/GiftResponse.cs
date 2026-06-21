using GoKidAPI.Enums.Gifts;

namespace GoKidAPI.DTO.Gifts.Responses
{
    public class GiftResponse
    {
        public string Id { get; set; } = null!;
        public string NameEn { get; set; } = null!;
        public string NameAr { get; set; } = null!;
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }
        public string? ImageUrl { get; set; }
        public int PointsCost { get; set; }
        public GiftType Type { get; set; }
        public GiftStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
