using GoKidAPI.DTO.Adventures.Requests;
using GoKidAPI.DTO.Adventures.Responses;
using GoKidAPI.DTO.ChidAdventure.Responses;
using GoKidAPI.Services.Child;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

// عاوزين نرجع ف الريسبونس وانا بعرض الادفنشرز كلها ف حاله ان التاسك كانت سابميتيد والطفل خد ليها نجوم هرجع انه
// خد منها نجوم ك فلاج
// وارجع عدد النجوم

namespace GoKidAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChildAdventureController : ControllerBase
    {
        private readonly IChildTaskService _taskService;

        public ChildAdventureController(IChildTaskService taskService)
        {
            _taskService = taskService;
        }
        private string CurrentChildId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        /// <summary>
        /// Retrieves all weekly adventures assigned to the logged-in child's class,
        /// including progress summary, completion status, and day-by-day tracking.
        /// </summary>
        /// <remarks>
        /// This endpoint returns a list of weekly adventures available to the child,
        /// along with aggregated progress data such as earned points, stars,
        /// completed tasks, and the status of each adventure day (locked, unlocked, completed, or missed).
        /// </remarks>
        [HttpGet]
        [ProducesResponseType(typeof(Shared.Response<List<ChildAdventureListItemResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyAdventures()
        {
            var result = await _taskService.GetMyAdventuresAsync(CurrentChildId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Retrieves detailed information for a specific weekly adventure,
        /// including all tasks with their lock/unlock status and submission details.
        /// </summary>
        /// <remarks>
        /// This endpoint returns the full structure of a weekly adventure,
        /// including:
        /// - Adventure metadata (title, description, goal, media)
        /// - Tasks ordered by day
        /// - Access control status per task (locked/unlocked/completed)
        /// - Submission status (not submitted, pending, approved, missed)
        /// - Child progress (points, stars, completion state)
        /// </remarks>
        [HttpGet("{weeklyAdventureId}")]
        [ProducesResponseType(typeof(Shared.Response<ChildAdventureDetailsResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAdventureDetails(string weeklyAdventureId)
        {
            var result = await _taskService.GetAdventureDetailsAsync(
                CurrentChildId, weeklyAdventureId);
            return StatusCode((int)result.StatusCode, result);
        }


        /// <summary>
        /// Gets all tasks for a weekly adventure with lock/unlock status per day
        /// </summary>
        [HttpGet("weekly-adventures/{weeklyAdventureId}/tasks")]
        public async Task<IActionResult> GetWeeklyAdventureTasks(string weeklyAdventureId)
        {
            var result = await _taskService.GetWeeklyAdventureTasksAsync(
                CurrentChildId, weeklyAdventureId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Submit an adventure task
        /// - InstantReward → points immediately
        /// - VoiceQuestion → AI evaluates
        /// - EvidenceSubmission → Supervisor reviews
        /// </summary>
        [HttpPost("adventure-tasks/submit")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SubmitAdventureTask(
            [FromForm] SubmitAdventureTaskRequest request)
        {
            var result = await _taskService.SubmitAdventureTaskAsync(CurrentChildId, request);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}
