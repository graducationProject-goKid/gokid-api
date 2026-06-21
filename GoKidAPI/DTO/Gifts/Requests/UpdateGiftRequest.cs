using GoKidAPI.Enums.Gifts;

namespace GoKidAPI.DTO.Gifts.Requests
{
    public class UpdateGiftRequest
    {
        public string? NameEn { get; set; }
        public string? NameAr { get; set; }
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }
        public IFormFile? Image { get; set; }
        public int? PointsCost { get; set; }
        public GiftType? Type { get; set; }
    }
}
