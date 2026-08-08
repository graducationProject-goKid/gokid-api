using Azure;
using GoKidAPI.Data;
using GoKidAPI.DTO.InstitutionAdmin.Supervisor.Requests;
using GoKidAPI.DTO.InstitutionAdmin.Supervisor.Responses;
using GoKidAPI.DTO.Supervisor.Requests;
using GoKidAPI.DTO.Supervisor.Responses;
using GoKidAPI.Entity.Account.Identity;
using GoKidAPI.Entity.Account.Users;
using GoKidAPI.Enums;
using GoKidAPI.Services.Email;
using GoKidAPI.Services.Institution.Interface;
using GoKidAPI.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GoKidAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InstitutionSupervisorController : ControllerBase
    {
        private readonly IInstitutionSupervisorService _supervisorService;
        private readonly ResponseHandler _response;

        public InstitutionSupervisorController(
            IInstitutionSupervisorService supervisorService,
            ResponseHandler response)
        {
            _supervisorService = supervisorService;
            _response = response;
        }

        /// <summary>
        /// Retrieves a paginated list of supervisors.
        /// </summary>
        /// <remarks>
        /// - **PlatformAdmin**: Can view **all** supervisors in the system.
        /// - **InstitutionAdmin**: Can only view supervisors belonging to their own institution.
        /// 
        /// Supports:
        /// - Pagination (PageNumber, PageSize)
        /// - Sorting (by FullName, Email, CreatedAt, PhoneNumber)
        /// - Optional search by name/email/username (via SearchTerm)
        /// 
        /// Returns paginated result with metadata (total count, pages, hasPrevious/hasNext).
        /// </remarks>
        /// <param name="filters">Pagination, sorting, and optional search filters</param>
        /// <response code="200">Supervisors retrieved successfully (paginated list)</response>
        /// <response code="401">Unauthorized – user not authenticated</response>
        /// <response code="403">Forbidden – user is not PlatformAdmin or InstitutionAdmin</response>
        /// <response code="404">Not found – no supervisors or no institution linked (for InstitutionAdmin)</response>
        /// <response code="500">Internal server error</response>
        //[Authorize(Roles = "InstitutionAdmin,PlatformAdmin")]
        [HttpGet]
        [ProducesResponseType(typeof(Shared.Response<SupervisorListItemResponse>), StatusCodes.Status200OK)]
        [Route("supervisors")]
        public async Task<IActionResult> GetSupervisors([FromQuery] GetSupervisorsFilters filters)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _supervisorService.GetAllSupervisorsAsync(currentUserId, filters);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Create a new Supervisor account for the institution of the logged-in InstitutionAdmin.
        /// The InstitutionAdmin must be authenticated to perform this action. 
        /// On success, a new AppUser of type Supervisor is created, assigned the Supervisor role, 
        /// linked to the institution, and login credentials are sent via email.
        /// </summary>
        /// <remarks>
        /// Sample login payload for the InstitutionAdmin:
        /// {
        ///   "identifier": "institutionadmin1@gokid.com",
        ///   "password": "P@ss123Pass",
        ///   "loginAs": "InstitutionAdmin"
        /// }
        /// </remarks>
        /// <param name="request">Supervisor creation request containing FullName, Email, Password, PhoneNumber, and optional AvatarFile</param>
        /// <response code="201">Supervisor account created successfully. Login credentials sent via email.</response>
        /// <response code="400">Request failed (email already exists, invalid data, or failed role assignment).</response>
        /// <response code="404">InstitutionAdmin or linked institution not found.</response>
        [HttpPost]
        [ProducesResponseType(typeof(Shared.Response<SupervisorCreatedResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateSupervisor([FromForm] CreateSupervisorRequest request)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _supervisorService.CreateSupervisorAsync(currentUserId, request);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Updates an existing Supervisor's info (name, phone, avatar).
        /// Only the InstitutionAdmin of the same institution can update.
        /// </summary>
        [HttpPut("{supervisorId}")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(Shared.Response<SupervisorUpdatedResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateSupervisor(
            string supervisorId,
            [FromForm] UpdateSupervisorRequest request)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _supervisorService.UpdateSupervisorAsync(currentUserId, supervisorId, request);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Soft deletes a Supervisor and removes them from all assigned classes.
        /// Locks the account to prevent future logins.
        /// </summary>
        [HttpDelete("{supervisorId}")]
        [ProducesResponseType(typeof(Shared.Response<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteSupervisor(string supervisorId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _supervisorService.DeleteSupervisorAsync(currentUserId, supervisorId);
            return StatusCode((int)result.StatusCode, result);
        }

    }
}
