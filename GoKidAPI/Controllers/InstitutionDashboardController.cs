using System.Security.Claims;

using GoKidAPI.DTO.Dashboard;
using GoKidAPI.Services.Dashboard;
using GoKidAPI.Shared;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoKidAPI.Controllers
{
    /// <summary>
    /// Institution-scoped statistics dashboard. Accessible by InstitutionAdmin only.
    /// Returns metrics for the admin's own institution. Cached per institution for 5 minutes.
    /// </summary>
    [ApiController]
    [Route("api/institution/dashboard")]
    [Authorize(Roles = "InstitutionAdmin")]
    public class InstitutionDashboardController : ControllerBase
    {
        private readonly IInstitutionDashboardService _dashboard;
        private readonly ResponseHandler _response;
        private readonly ILogger<InstitutionDashboardController> _logger;

        public InstitutionDashboardController(
            IInstitutionDashboardService dashboard,
            ResponseHandler response,
            ILogger<InstitutionDashboardController> logger)
        {
            _dashboard = dashboard;
            _response = response;
            _logger = logger;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        /// <summary>
        /// Returns the full institution dashboard for the authenticated admin.
        /// Includes: Overview, Classes, Supervisors, Children, Adventures, Tasks, Points &amp; Levels, Recent Activity.
        /// Result is cached per institution for 5 minutes.
        /// </summary>
        /// <response code="200">Dashboard data returned successfully.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">InstitutionAdmin role required.</response>
        /// <response code="404">Admin has no institution assigned.</response>
        /// <response code="500">An error occurred while building the dashboard.</response>
        [HttpGet]
        [ProducesResponseType(typeof(Response<InstitutionDashboardResponse>), StatusCodes.Status200OK)]
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
                    var notFound = _response.NotFound<InstitutionDashboardResponse>("No institution found for this admin.");
                    return StatusCode((int)notFound.StatusCode, notFound);
                }

                var result = _response.Success(data, "Institution dashboard retrieved successfully.");
                return StatusCode((int)result.StatusCode, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building institution dashboard for admin {AdminId}", CurrentUserId);
                var result = _response.InternalServerError<InstitutionDashboardResponse>(
                    "An error occurred while building the dashboard.");
                return StatusCode((int)result.StatusCode, result);
            }
        }

        /// <summary>
        /// Invalidates the dashboard cache for this admin's institution so the next GET recomputes all statistics.
        /// </summary>
        /// <response code="200">Cache invalidated successfully.</response>
        /// <response code="404">Admin has no institution assigned.</response>
        [HttpDelete("cache")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> InvalidateCache()
        {
            var data = await _dashboard.GetDashboardAsync(CurrentUserId);
            if (data == null)
            {
                var notFound = _response.NotFound<object>("No institution found for this admin.");
                return StatusCode((int)notFound.StatusCode, notFound);
            }

            _dashboard.InvalidateCache(data.InstitutionId);
            _logger.LogInformation("Institution dashboard cache invalidated by admin {AdminId}", CurrentUserId);

            var result = _response.Deleted<object>("Dashboard cache cleared. Next request will recompute statistics.");
            return StatusCode((int)result.StatusCode, result);
        }
    }
}
