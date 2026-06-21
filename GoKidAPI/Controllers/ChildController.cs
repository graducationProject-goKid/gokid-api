using GoKidAPI.DTO.Childs.Requests;
using GoKidAPI.Enums.Tasks;
using GoKidAPI.Services.Child;
using GoKidAPI.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GoKidAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChildController : ControllerBase
    {
        private readonly IChildTaskService _taskService;
        private readonly IChildService _childService;
        private readonly ResponseHandler _response;

        public ChildController(IChildTaskService taskService, ResponseHandler response, IChildService childService)
        {
            _taskService = taskService;
            _response = response;
            _childService = childService;
        }

        private string CurrentChildId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        /// <summary>
        /// Gets tasks for the logged-in child based on the selected tab/source
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTasks([FromQuery] GetChildTasksTypesFilters query)
        {
            var date = query.Date?.Date ?? DateTime.UtcNow.Date;
            object resultData;
            string successMessage;

            switch (query.SourceFilter)
            {
                case TaskSourceTab.General:
                    var generalResult = await _taskService.GetDailyGeneralTasksAsync(CurrentChildId, date);
                    if (!generalResult.Succeeded)
                        return StatusCode((int)generalResult.StatusCode, generalResult);
                    resultData = generalResult.Data;
                    successMessage = "General tasks retrieved successfully";
                    break;

                case TaskSourceTab.Parent:
                    var parentResult = await _childService.GetParentAssignedTasksAsync(CurrentChildId);
                    if (!parentResult.Succeeded)
                        return StatusCode((int)parentResult.StatusCode, parentResult);
                    resultData = parentResult.Data;
                    successMessage = "Parent assigned tasks retrieved successfully";
                    break;

                default:
                    return StatusCode((int)StatusCodes.Status400BadRequest);
            }

            return Ok(_response.Success(resultData, successMessage));
        }

        /// <summary>
        /// Gets the child's current points balance
        /// </summary>
        [HttpGet("points")]
        public async Task<IActionResult> GetPoints()
        {
            var result = await _childService.GetPointsAsync(CurrentChildId);
            return StatusCode((int)result.StatusCode, result);
        }


        /// <summary>
        /// Submit a task for the logged-in child
        /// </summary>
        [HttpPost("submit/{taskId}")]
        public async Task<IActionResult> SubmitTask(
            [FromRoute] string taskId,
            [FromForm] SubmitTaskRequest request)
        {
            if (string.IsNullOrEmpty(taskId))
                return BadRequest("TaskId is required");

            request.TaskId = taskId;

            var result = await _taskService.SubmitTaskAsync(CurrentChildId, request);

            if (!result.Succeeded)
                return StatusCode((int)result.StatusCode, result);

            return Ok(result);
        }

    }
}
