using System.Security.Claims;

using GoKidAPI.Enums;
using GoKidAPI.Services.Statistics;
using GoKidAPI.Shared;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GoKidAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Child,Parent")]
    public class StatisticsController : ControllerBase
    {
        private readonly IStatisticsService _statisticsService;
        private readonly ResponseHandler _response;

        public StatisticsController(
            IStatisticsService statisticsService,
            ResponseHandler response)
        {
            _statisticsService = statisticsService;
            _response = response;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        private string CurrentUserRole => User.FindFirstValue(ClaimTypes.Role)!;

        /// <summary>
        /// Gets comprehensive statistics.
        /// Accessible by Child and Parent — Parent sees their child's stats automatically.
        /// </summary>
        /// <param name="period">ThisWeek, ThisMonth, or AllTime (default = ThisWeek)</param>
        [HttpGet]
        public async Task<IActionResult> GetStatistics(
            [FromQuery] StatisticsPeriod period = StatisticsPeriod.ThisWeek)
        {
            var result = await _statisticsService.GetParentStatisticsAsync(
                CurrentUserId, CurrentUserRole, period);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}
