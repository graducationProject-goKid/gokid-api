using GoKidAPI.DTO.Dashboard;
using GoKidAPI.Services.Dashboard;
using GoKidAPI.Shared;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoKidAPI.Controllers
{
    /// <summary>
    /// Platform-wide statistics dashboard. Accessible by PlatformAdmin only.
    /// Responses are cached for 5 minutes; use DELETE /cache to force a refresh.
    /// </summary>
    [ApiController]
    [Route("api/platform/dashboard")]
    [Authorize(Roles = "PlatformAdmin")]
    public class PlatformDashboardController : ControllerBase
    {
        private readonly IPlatformDashboardService _dashboard;
        private readonly ResponseHandler _response;
        private readonly ILogger<PlatformDashboardController> _logger;

        public PlatformDashboardController(
            IPlatformDashboardService dashboard,
            ResponseHandler response,
            ILogger<PlatformDashboardController> logger)
        {
            _dashboard = dashboard;
            _response = response;
            _logger = logger;
        }

        /// <summary>
        /// Returns the full platform statistics dashboard with 8 sections:
        /// Overview, Users, Institutions, Tasks, Adventures, Points &amp; Levels, Gifts, Recent Activity.
        /// Result is cached for 5 minutes.
        /// </summary>
        /// <response code="200">Dashboard data returned successfully.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">PlatformAdmin role required.</response>
        /// <response code="500">An error occurred while building the dashboard.</response>
        [HttpGet]
        [ProducesResponseType(typeof(Response<PlatformDashboardResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var data = await _dashboard.GetDashboardAsync();
                var result = _response.Success(data, "Dashboard retrieved successfully.");
                return StatusCode((int)result.StatusCode, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building platform dashboard");
                var result = _response.InternalServerError<PlatformDashboardResponse>(
                    "An error occurred while building the dashboard.");
                return StatusCode((int)result.StatusCode, result);
            }
        }

        /// <summary>
        /// Invalidates the dashboard cache so the next GET recomputes all statistics.
        /// </summary>
        /// <response code="200">Cache invalidated successfully.</response>
        [HttpDelete("cache")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult InvalidateCache()
        {
            _dashboard.InvalidateCache();
            _logger.LogInformation("Platform dashboard cache invalidated by {UserId}",
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            var result = _response.Deleted<object>("Dashboard cache cleared. Next request will recompute statistics.");
            return StatusCode((int)result.StatusCode, result);
        }
    }
}
