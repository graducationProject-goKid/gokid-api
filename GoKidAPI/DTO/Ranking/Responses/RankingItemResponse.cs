namespace GoKidAPI.DTO.Ranking.Responses
{
    public class RankingItemResponse
    {
        public int Rank { get; set; }
        public string ChildId { get; set; } = null!;
        public string ChildName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public int HighestPoints { get; set; }
        public bool IsCurrentChild { get; set; }  // عشان الـ Frontend يعرف يـ highlight
    }
}
