using System.Security.Claims;

using GoKidAPI.DTO.Institution.Requests;
using GoKidAPI.DTO.Institution.Responses;
using GoKidAPI.Services.Institution.Interface;
using GoKidAPI.Shared;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GoKidAPI.Controllers
{
    [ApiController]
    [Route("api/institutions")]
    [Authorize(Roles = "PlatformAdmin")]
    public class InstitutionsController : ControllerBase
    {
        private readonly IInstitutionService _institutionService;
        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        public InstitutionsController(IInstitutionService institutionService)
        {
            _institutionService = institutionService;
        }

        /// <summary>
        /// Creates a new institution and generates an InstitutionAdmin account.
        /// Login credentials are sent to the admin email automatically.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(Response<InstitutionDetailsResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(Response<InstitutionDetailsResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Create([FromForm] CreateInstitutionRequest request)
        {
            var result = await _institutionService.CreateInstitutionAsync(request, CurrentUserId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Updates institution details. Admin account email cannot be changed here.
        /// </summary>
        [HttpPut("{institutionId}")]
        [ProducesResponseType(typeof(Response<InstitutionDetailsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response<InstitutionDetailsResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Update(string institutionId, [FromForm] UpdateInstitutionRequest request)
        {
            var result = await _institutionService.UpdateInstitutionAsync(institutionId, request, CurrentUserId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Soft deletes an institution and locks the associated admin account.
        /// </summary>
        [HttpDelete("{institutionId}")]
        [ProducesResponseType(typeof(Response<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Delete(string institutionId)
        {
            var result = await _institutionService.DeleteInstitutionAsync(institutionId, CurrentUserId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Returns a paginated list of all institutions with summary counts.
        /// Supports search by name, code, city, or country.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(Response<PaginatedList<InstitutionListItemResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            var result = await _institutionService.GetAllInstitutionsAsync(pageNumber, pageSize, search);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Returns full details of a specific institution including supervisor list and classes summary.
        /// </summary>
        [HttpGet("{institutionId}")]
        [ProducesResponseType(typeof(Response<InstitutionDetailsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response<InstitutionDetailsResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetDetails(string institutionId)
        {
            var result = await _institutionService.GetInstitutionDetailsAsync(institutionId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Returns the profile of a specific supervisor within an institution,
        /// including their assigned classes and children counts.
        /// </summary>
        [HttpGet("{institutionId}/supervisors/{supervisorId}")]
        [ProducesResponseType(typeof(Response<SupervisorProfileResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response<SupervisorProfileResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetSupervisorProfile(string institutionId, string supervisorId)
        {
            var result = await _institutionService.GetSupervisorProfileAsync(institutionId, supervisorId);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}
