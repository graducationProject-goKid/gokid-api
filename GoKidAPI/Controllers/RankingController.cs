using System.Security.Claims;

using GoKidAPI.Services.Ranking;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GoKidAPI.Controllers
{
    [ApiController]
    [Route("api/ranking")]
    [Authorize(Roles = "Child,Parent")]
    public class RankingController : ControllerBase
    {
        private readonly IRankingService _rankingService;
        public RankingController(IRankingService rankingService) => _rankingService = rankingService;

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        private string CurrentUserRole => User.FindFirstValue(ClaimTypes.Role)!;

        /// <summary>
        /// Global leaderboard. Accessible by Child and Parent.
        /// Parent sees their child's rank automatically.
        /// </summary>
        [HttpGet("global")]
        public async Task<IActionResult> GetGlobalRanking([FromQuery] int topCount = 20)
        {
            var result = await _rankingService.GetGlobalRankingAsync(
                CurrentUserId, CurrentUserRole, topCount);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Institution leaderboard. Accessible by Child and Parent.
        /// Parent sees their child's rank automatically.
        /// </summary>
        [HttpGet("institution")]
        public async Task<IActionResult> GetInstitutionRanking([FromQuery] int topCount = 3)
        {
            var result = await _rankingService.GetInstitutionRankingAsync(
                CurrentUserId, CurrentUserRole, topCount);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}

