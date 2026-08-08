using GoKidAPI.Enums.Gifts;
using System.ComponentModel.DataAnnotations;

namespace GoKidAPI.DTO.Gifts.Responses
{
    public class RewardResponse
    {
        public string Id { get; set; } = null!;
        public string NameEn { get; set; } = null!;
        public string NameAr { get; set; } = null!;
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }
        public string? ImageUrl { get; set; }
        public int TargetPoints { get; set; }
        public string ChildId { get; set; } = null!;
        public string ChildName { get; set; } = null!;
        public int ChildCurrentPoints { get; set; }
        public bool TargetReached { get; set; }     // عشان الـ Frontend يعرف يظهر زرار Give
        public string? MessageToChild { get; set; }
        public RewardStatus Status { get; set; }
        public DateTime? GivenAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
