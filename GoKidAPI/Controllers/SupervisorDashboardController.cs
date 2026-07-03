using System.Security.Claims;

using GoKidAPI.DTO.Dashboard;
using GoKidAPI.Services.Dashboard;
using GoKidAPI.Shared;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoKidAPI.Controllers
{
    /// <summary>
    /// Supervisor-scoped statistics dashboard. Accessible by Supervisor role only.
    /// Returns metrics for all classes the supervisor is assigned to. Cached per supervisor for 3 minutes.
    /// </summary>
    [ApiController]
    [Route("api/supervisor/dashboard")]
    [Authorize(Roles = "Supervisor")]
    public class SupervisorDashboardController : ControllerBase
    {
        private readonly ISupervisorDashboardService _dashboard;
        private readonly ResponseHandler _response;
        private readonly ILogger<SupervisorDashboardController> _logger;

        public SupervisorDashboardController(
            ISupervisorDashboardService dashboard,
            ResponseHandler response,
            ILogger<SupervisorDashboardController> logger)
        {
            _dashboard = dashboard;
            _response = response;
            _logger = logger;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        /// <summary>
        /// Returns the full supervisor dashboard for the authenticated supervisor.
        /// Includes: Overview, Classes, Pending Reviews, Children Progress, Adventures, Task Performance, Recent Activity.
        /// Result is cached per supervisor for 3 minutes.
        /// </summary>
        /// <response code="200">Dashboard data returned successfully.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Supervisor role required.</response>
        /// <response code="404">Supervisor profile not found.</response>
        /// <response code="500">An error occurred while building the dashboard.</response>
        [HttpGet]
        [ProducesResponseType(typeof(Response<SupervisorDashboardResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var data = await _dashboard.GetDashboardAsync(CurrentUserId);
                if (data == null)
                {
                    var notFound = _response.NotFound<SupervisorDashboardResponse>("Supervisor profile not found.");
                    return StatusCode((int)notFound.StatusCode, notFound);
                }

                var result = _response.Success(data, "Supervisor dashboard retrieved successfully.");
                return StatusCode((int)result.StatusCode, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building supervisor dashboard for user {UserId}", CurrentUserId);
                var result = _response.InternalServerError<SupervisorDashboardResponse>(
                    "An error occurred while building the dashboard.");
                return StatusCode((int)result.StatusCode, result);
            }
        }

        /// <summary>
        /// Invalidates the cached dashboard so the next GET recomputes all statistics.
        /// Useful after reviewing tasks or when real-time data is needed immediately.
        /// </summary>
        /// <response code="200">Cache invalidated successfully.</response>
        /// <response code="404">Supervisor profile not found.</response>
        [HttpDelete("cache")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> InvalidateCache()
        {
            var data = await _dashboard.GetDashboardAsync(CurrentUserId);
            if (data == null)
            {
                var notFound = _response.NotFound<object>("Supervisor profile not found.");
                return StatusCode((int)notFound.StatusCode, notFound);
            }

            _dashboard.InvalidateCache(data.SupervisorId);
            _logger.LogInformation("Supervisor dashboard cache invalidated by {UserId}", CurrentUserId);

            var result = _response.Deleted<object>("Dashboard cache cleared. Next request will recompute statistics.");
            return StatusCode((int)result.StatusCode, result);
        }
    }
}
