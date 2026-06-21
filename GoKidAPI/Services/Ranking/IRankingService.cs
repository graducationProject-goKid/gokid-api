using GoKidAPI.DTO.Ranking.Responses;

namespace GoKidAPI.Services.Ranking
{
    public interface IRankingService
    {
        Task<Shared.Response<RankingResponse>> GetGlobalRankingAsync(string userId, string userRole, int topCount = 20);
        Task<Shared.Response<RankingResponse>> GetInstitutionRankingAsync(string userId, string userRole, int topCount = 3);
    }
}
