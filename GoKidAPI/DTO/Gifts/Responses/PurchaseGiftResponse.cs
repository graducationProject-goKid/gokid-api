namespace GoKidAPI.DTO.Gifts.Responses
{
    public class PurchaseGiftResponse
    {
        public string ChildGiftId { get; set; } = null!;
        public string GiftName { get; set; } = null!;
        public string? GiftImageUrl { get; set; }
        public int PointsSpent { get; set; }
        public int RemainingPoints { get; set; }    // النقاط المتبقية بعد الشراء
        public int HighestPoints { get; set; }      // أعلى نقاط للـ ranking
        public DateTime PurchasedAt { get; set; }
    }
}
