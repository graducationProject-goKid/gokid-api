using GoKidAPI.DTO.Classes.Requests;
using GoKidAPI.DTO.Classes.Responses;
using GoKidAPI.Services.Classes;
using GoKidAPI.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GoKidAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClassController : ControllerBase
    {
        private readonly IClassService _classService;
        private readonly ResponseHandler _response;

        public ClassController(IClassService classService, ResponseHandler response)
        {
            _classService = classService;
            _response = response;
        }

        /// <summary>
        /// Create a new class
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(Response<ClassDetailsResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(Response<ClassDetailsResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Response<ClassDetailsResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateClass([FromBody] CreateClassRequest request)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _classService.CreateClassAsync(currentUserId, request);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Update class
        /// </summary>
        [HttpPut("{classId}")]
        [ProducesResponseType(typeof(Response<ClassDetailsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response<ClassDetailsResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateClass(string classId, [FromBody] UpdateClassRequest request)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _classService.UpdateClassAsync(currentUserId, classId, request);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Delete class
        /// </summary>
        [HttpDelete("{classId}")]
        [ProducesResponseType(typeof(Response<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteClass(string classId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _classService.DeleteClassAsync(currentUserId, classId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Get class by id
        /// </summary>
        [HttpGet("{classId}")]
        [ProducesResponseType(typeof(Response<ClassDetailsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response<ClassDetailsResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetClassById(string classId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _classService.GetClassByIdAsync(currentUserId, classId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Get classes list
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(Response<PaginatedList<ClassListItemResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response<PaginatedList<ClassListItemResponse>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAllClasses([FromQuery] GetClassesFilters filters)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _classService.GetAllClassesAsync(currentUserId, filters);
            return StatusCode((int)result.StatusCode, result);
        }
        /// <summary>
        /// Assigns a supervisor to a specific class within the current admin's institution.
        /// </summary>
        /// <remarks>
        /// Rules:
        /// - The supervisor must belong to the same institution as the admin.
        /// - The class must belong to the admin's institution.
        /// - Duplicate assignments are not allowed.
        /// </remarks>
        /// <param name="classId">Target class identifier</param>
        /// <param name="request">Supervisor assignment request</param>
        [HttpPost("assign-supervisor/{classId}")]
        [ProducesResponseType(typeof(Response<SupervisorAssignmentResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response<SupervisorAssignmentResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Response<SupervisorAssignmentResponse>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AssignSupervisor(string classId, [FromBody] AssignSupervisorToClassRequest request)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _classService.AssignSupervisorToClassAsync(currentUserId, classId, request);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Removes a supervisor from a specific class (UnAssign)
        /// </summary>
        /// <remarks>
        /// The operation performs a soft delete of the supervisor-class assignment.
        /// </remarks>
        /// <param name="classId">Target class identifier</param>
        /// <param name="supervisorId">Supervisor identifier</param>
        [HttpDelete("{classId}/supervisors/{supervisorId}")]
        [ProducesResponseType(typeof(Response<SupervisorAssignmentResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response<SupervisorAssignmentResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Response<SupervisorAssignmentResponse>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RemoveSupervisor(string classId, string supervisorId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _classService.RemoveSupervisorFromClassAsync(currentUserId, classId, supervisorId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Enrolls a child to the institution using their registration code.
        /// Child won't be assigned to any class yet.
        /// </summary>
        // I Use the Enroll Cause it has the same prop we need 
        [HttpPost("institution/enroll-child")]
        public async Task<IActionResult> EnrollChildToInstitution(
            [FromBody] EnrollChildToClassRequest erollToInstitutionRequest)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _classService.EnrollChildToInstitutionAsync(currentUserId, erollToInstitutionRequest.RegistrationCode);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Removes a child from the institution (and their class if assigned).
        /// </summary>
        [HttpDelete("institution/children/{childId}")]
        public async Task<IActionResult> RemoveChildFromInstitution(string childId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _classService.RemoveChildFromInstitutionAsync(currentUserId, childId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Assigns an institution-enrolled child to a specific class.
        /// Child must be enrolled in the institution first.
        /// </summary>
        [HttpPost("{classId}/enroll-child")]
        public async Task<IActionResult> EnrollChildToClass(
            string classId,
            [FromBody] EnrollChildToClassRequest request)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _classService.EnrollChildToClassAsync(currentUserId, classId, request);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Removes a child from a class only. Child remains in the institution.
        /// </summary>
        [HttpDelete("{classId}/children/{childId}")]
        public async Task<IActionResult> RemoveChildFromClass(string classId, string childId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _classService.RemoveChildFromClassAsync(currentUserId, classId, childId);
            return StatusCode((int)result.StatusCode, result);
        }
        /// <summary>
        /// Gets all children enrolled in the institution.
        /// Supports filtering by name and class.
        /// </summary>
        [HttpGet("institution/children")]
        //[Authorize(Roles = "InstitutionAdmin,Supervisor")]
        public async Task<IActionResult> GetInstitutionChildren(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string? search = null,
    [FromQuery] string? classId = null)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role)!;

            var result = await _classService.GetInstitutionChildrenAsync(
                currentUserId, currentUserRole, pageNumber, pageSize, search, classId);

            return StatusCode((int)result.StatusCode, result);
        }
    }
}
