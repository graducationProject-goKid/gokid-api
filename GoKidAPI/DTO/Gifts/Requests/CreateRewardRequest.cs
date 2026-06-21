namespace GoKidAPI.DTO.Gifts.Requests
{
    public class CreateRewardRequest
    {
        public string NameEn { get; set; } = null!;
        public string NameAr { get; set; } = null!;
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }
        public IFormFile? Image { get; set; }
        public int TargetPoints { get; set; }
        public string ChildId { get; set; } = null!;
    }
}
