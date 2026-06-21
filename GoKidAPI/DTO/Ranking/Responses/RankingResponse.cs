namespace GoKidAPI.DTO.Ranking.Responses
{
    public class RankingResponse
    {
        public List<RankingItemResponse> TopRanking { get; set; } = new();
        public RankingItemResponse? MyRank { get; set; }
    }
}
