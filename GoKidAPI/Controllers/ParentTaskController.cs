using System.Security.Claims;

using GoKidAPI.DTO.Tasks.Requests;
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
    }
}
