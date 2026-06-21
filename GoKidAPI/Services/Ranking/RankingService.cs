using GoKidAPI.Data;
using GoKidAPI.DTO.Ranking.Responses;
using GoKidAPI.Shared;

using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Services.Ranking
{
    public class RankingService : IRankingService
    {
        private readonly AppDbContext _context;
        private readonly ResponseHandler _response;

        public RankingService(AppDbContext context, ResponseHandler response)
        {
            _context = context;
            _response = response;
        }

        public async Task<Response<RankingResponse>> GetGlobalRankingAsync(
    string userId, string userRole, int topCount = 20)
        {
            var childId = await ResolveChildIdAsync(userId, userRole);
            if (childId == null)
                return _response.NotFound<RankingResponse>(
                    userRole == "Parent" ? "No child linked to this parent" : "Child not found");

            var allChildren = await _context.Childrens
                .Where(c => !c.IsDeleted)
                .OrderByDescending(c => c.HighestPoints)
                .Select(c => new { c.Id, c.Name, c.AvatarUrl, c.HighestPoints })
                .ToListAsync();

            var ranked = allChildren
                .Select((c, index) => new RankingItemResponse
                {
                    Rank = index + 1,
                    ChildId = c.Id,
                    ChildName = c.Name,
                    AvatarUrl = c.AvatarUrl,
                    HighestPoints = c.HighestPoints,
                    IsCurrentChild = c.Id == childId
                })
                .ToList();

            var topRanking = ranked.Take(topCount).ToList();
            var myRank = ranked.FirstOrDefault(r => r.ChildId == childId);
            if (myRank != null && myRank.Rank <= topCount)
                myRank = null;

            return _response.Success(new RankingResponse
            {
                TopRanking = topRanking,
                MyRank = myRank
            }, "Global ranking retrieved successfully");
        }

        public async Task<Response<RankingResponse>> GetInstitutionRankingAsync(
     string userId, string userRole, int topCount = 3)
        {
            var childId = await ResolveChildIdAsync(userId, userRole);
            if (childId == null)
                return _response.NotFound<RankingResponse>(
                    userRole == "Parent" ? "No child linked to this parent" : "Child not found");

            var child = await _context.Childrens
                .FirstOrDefaultAsync(c => c.Id == childId && !c.IsDeleted);

            if (child?.InstitutionId == null)
                return _response.BadRequest<RankingResponse>("Child is not enrolled in any institution");

            var institutionChildren = await _context.Childrens
                .Where(c => !c.IsDeleted && c.InstitutionId == child.InstitutionId)
                .OrderByDescending(c => c.HighestPoints)
                .Select(c => new { c.Id, c.Name, c.AvatarUrl, c.HighestPoints })
                .ToListAsync();

            var ranked = institutionChildren
                .Select((c, index) => new RankingItemResponse
                {
                    Rank = index + 1,
                    ChildId = c.Id,
                    ChildName = c.Name,
                    AvatarUrl = c.AvatarUrl,
                    HighestPoints = c.HighestPoints,
                    IsCurrentChild = c.Id == childId
                })
                .ToList();

            var topRanking = ranked.Take(topCount).ToList();
            var myRank = ranked.FirstOrDefault(r => r.ChildId == childId);
            if (myRank != null && myRank.Rank <= topCount)
                myRank = null;

            return _response.Success(new RankingResponse
            {
                TopRanking = topRanking,
                MyRank = myRank
            }, "Institution ranking retrieved successfully");
        }

        private async Task<string?> ResolveChildIdAsync(string userId, string userRole)
        {
            if (userRole == "Parent")
            {
                // جيب أول child مرتبط بالـ Parent
                var child = await _context.Childrens
                    .FirstOrDefaultAsync(c => c.ParentId == userId && !c.IsDeleted);
                return child?.Id;
            }

            // Child بيدخل بـ ID بتاعه مباشرة
            var exists = await _context.Childrens
                .AnyAsync(c => c.Id == userId && !c.IsDeleted);
            return exists ? userId : null;
        }
    }
}
