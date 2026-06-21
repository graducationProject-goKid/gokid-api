using System.Security.Claims;

using GoKidAPI.DTO.Tasks.Requests;
using GoKidAPI.DTO.Tasks.Responses;
using GoKidAPI.Enums.Tasks;
using GoKidAPI.Services.ParentTasks;
using GoKidAPI.Shared;

using Microsoft.AspNetCore.Mvc;

namespace GoKidAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ParentTaskController : ControllerBase
    {
        private readonly IParentTaskService _service;
        private readonly ResponseHandler _response;

        public ParentTaskController(IParentTaskService service, ResponseHandler response)
        {
            _service = service;
            _response = response;
        }

        /// <summary>
        /// Assign a task template to your child
        /// </summary>
        /// <remarks>
        /// Parent selects a task from the platform and assigns it to their child.
        /// Optional DueDate (if not provided, no deadline).
        /// </remarks>
        /// <response code="201">Task assigned successfully</response>
        /// <response code="400">Invalid request or task already assigned</response>
        /// <response code="404">Task template not found</response>
        [HttpPost("assign")]
        [ProducesResponseType(typeof(Response<AssignTaskResponse>), StatusCodes.Status201Created)]
        public async Task<IActionResult> AssignTask([FromBody] AssignTaskRequest request)
        {
            var parentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(parentId))
                return Unauthorized(_response.Unauthorized<object>("Unauthorized"));

            var result = await _service.AssignTaskToChildAsync(parentId, request);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Gets all tasks assigned by the parent to their child(ren), with optional filters
        /// </summary>
        /// <remarks>
        /// - Filter by status (Pending, InProgress(Child click on start), ReviewRequested, Completed, Rejected)
        /// - Search by task title
        /// - Pagination and sorting supported
        /// - If no childId provided, returns tasks for all linked children
        /// </remarks>
        [HttpGet]
        public async Task<IActionResult> GetChildTasks([FromQuery] GetChildTasksFilters filters)
        {
            var parentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _service.GetChildTasksAsync(parentId, filters);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Gets detailed view of a specific child task assigned by the parent
        /// </summary>
        /// <remarks>
        /// Returns all necessary information for the parent to review and decide (accept/reject):
        /// - Task details (title, description, points, type)
        /// - Status and dates
        /// - Child's note (ChildNote)
        /// - Uploaded evidence (AnswerMediaUrl)
        /// - Previous acceptance/rejection info (if found)
        /// 
        /// PlatformAdmin can use a separate endpoint if needed (full internal details).
        /// </remarks>
        /// <param name="childTaskId">The ID of the ChildTask</param>
        /// <response code="200">Task details for parent review</response>
        /// <response code="403">Forbidden – not your child's task</response>
        /// <response code="404">Task not found</response>
        [HttpGet("{childTaskId}/details")]
        public async Task<IActionResult> GetChildTaskDetails(string childTaskId)
        {
            var parentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _service.GetParentChildTaskDetailsAsync(parentId, childTaskId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Parent reviews a child task (approve or reject)
        /// </summary>
        /// <remarks>
        /// - Approve → task becomes Completed and child gets points
        /// - Reject → task becomes Rejected and parent must provide reason
        /// </remarks>
        /// <response code="200">Review completed successfully</response>
        /// <response code="400">Invalid request</response>
        /// <response code="404">Task not found or not eligible for review</response>
        [HttpPost("review")]
        [ProducesResponseType(typeof(Response<ReviewDecisionResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ReviewTask([FromBody] ReviewTaskDecisionRequest request)
        {
            var parentId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(parentId))
                return Unauthorized(_response.Unauthorized<object>("Unauthorized"));

            var result = await _service.ReviewChildTaskAsync(parentId, request);

            return StatusCode((int)result.StatusCode, result);
        }
    }
}
