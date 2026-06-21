// Controllers/SupervisorController.cs
using GoKidAPI.DTO.Supervisor.Requests;
using GoKidAPI.Enums.Adventures;
using GoKidAPI.Services.Supervisor;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.Security.Claims;

namespace GoKidAPI.Controllers
{
    [ApiController]
    [Route("api/supervisor")]
    [Authorize(Roles = "Supervisor")]
    public class SupervisorController : ControllerBase
    {
        private readonly ISupervisorService _supervisorService;

        public SupervisorController(ISupervisorService supervisorService)
        {
            _supervisorService = supervisorService;
        }

        private string CurrentUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        /// <summary>
        /// Retrieves all active weekly adventures assigned to classes supervised by the current supervisor.
        /// Returns adventure details including class info, total children count,
        /// and number of submitted tasks pending review.
        /// </summary>
        [HttpGet("adventures")]
        public async Task<IActionResult> GetMyAdventures()
        {
            var result = await _supervisorService.GetMyAdventuresAsync(CurrentUserId);
            return StatusCode((int)result.StatusCode, result);

        }

        /// <summary>
        /// Retrieves child task submissions for a specific weekly adventure.
        /// Supports optional filtering by task status (Pending, Completed, Missed, etc.)
        /// and pagination for large result sets.
        /// Used by supervisors to review submitted child tasks.
        /// </summary>
        [HttpGet("adventures/{weeklyAdventureId}/tasks")]
        public async Task<IActionResult> GetAdventureChildTasks(
            string weeklyAdventureId,
            [FromQuery] AdventureChildTaskStatus? status = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _supervisorService.GetAdventureChildTasksAsync(
                CurrentUserId, weeklyAdventureId, status, pageNumber, pageSize);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Reviews a submitted child adventure task.
        /// Supervisor can approve or reject the task submission.
        /// 
        /// If approved:
        /// - Task status becomes Completed
        /// - Child earns stars and points
        /// - Adventure progress is updated
        /// - Completion bonus may be awarded automatically
        /// 
        /// If rejected:
        /// - Task status returns to Pending
        /// - Submitted evidence is removed
        /// - Child can resubmit the task
        /// </summary>
        [HttpPut("tasks/{childAdventureTaskId}/review")]
        public async Task<IActionResult> ReviewChildTask(
            string childAdventureTaskId,
            [FromBody] ReviewAdventureTaskRequest request)
        {
            var result = await _supervisorService.ReviewChildTaskAsync(
                CurrentUserId, childAdventureTaskId, request);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Retrieves all children in a specific class with their progress summary
        /// for a given weekly adventure.
        /// 
        /// Includes:
        /// - Total tasks
        /// - Submitted tasks count
        /// - Completed tasks count
        /// - Earned stars
        /// - Earned points
        /// - Adventure completion status
        /// </summary>
        [HttpGet("adventures/{weeklyAdventureId}/classes/{classId}/children")]
        public async Task<IActionResult> GetClassChildrenProgress(
            string weeklyAdventureId,
            string classId)
        {
            var result = await _supervisorService.GetClassChildrenProgressAsync(
                CurrentUserId, weeklyAdventureId, classId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Retrieves the full adventure history for a specific child
        /// within a weekly adventure.
        /// 
        /// Includes:
        /// - Progress summary
        /// - Completed, pending, and missed tasks
        /// - Earned points and stars
        /// - Detailed task history with submission evidence and review status
        /// </summary>
        [HttpGet("adventures/{weeklyAdventureId}/children/{childId}/history")]
        public async Task<IActionResult> GetChildAdventureHistory(
            string weeklyAdventureId,
            string childId)
        {
            var result = await _supervisorService.GetChildAdventureHistoryAsync(
                CurrentUserId, weeklyAdventureId, childId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Retrieves all classes assigned to the current supervisor
        /// with children count and active adventures count.
        /// </summary>
        [HttpGet("classes")]
        public async Task<IActionResult> GetMyClasses()
        {
            var result = await _supervisorService.GetMyClassesAsync(CurrentUserId);
            return StatusCode((int)result.StatusCode, result);
        }





    }
}