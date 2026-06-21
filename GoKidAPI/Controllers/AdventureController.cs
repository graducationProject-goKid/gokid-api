using Azure;
using GoKidAPI.DTO.Adventures.Requests;
using GoKidAPI.DTO.Adventures.Responses;
using GoKidAPI.DTO.AdventureStory;
using GoKidAPI.Enums.Adventures;
using GoKidAPI.Services.Adventure;
using GoKidAPI.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GoKidAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdventureController : ControllerBase
    {
        private readonly IAdventureService _adventureService;
        private readonly ResponseHandler _response;

        public AdventureController(
            IAdventureService adventureService,
            ResponseHandler response)
        {
            _adventureService = adventureService;
            _response = response;
        }

        /// <summary>
        /// Creates a new Adventure with its tasks (Drag & Drop order supported)
        /// </summary>
        /// <remarks>
        /// InstitutionAdmin can:
        /// - Create adventure with title and description
        /// - Upload description voice file (optional) or let system generate TTS
        /// - Add multiple tasks with DayNumber (Drag & Drop order)
        /// - Each task can have StoryText + StoryVoiceFile (optional)
        /// 
        /// The system will automatically generate voice using Text-to-Speech if no voice file is provided.
        /// </remarks>
        /// <param name="request">Adventure data + list of tasks</param>
        /// <response code="201">Adventure created successfully</response>
        /// <response code="400">Invalid request data or task templates not found</response>
        /// <response code="404">Institution not found for this admin</response>
        [HttpPost]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(Shared.Response<CreateAdventureResponse>), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateAdventure([FromForm] CreateAdventureRequest request)
        {
            var institutionAdminId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(institutionAdminId))
                return Unauthorized(_response.Unauthorized<object>("Unauthorized"));

            var result = await _adventureService.CreateAdventureAsync(institutionAdminId, request);

            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Gets paginated list of adventures for the current InstitutionAdmin
        /// </summary>
        /// <remarks>
        /// Supports filtering by status and searching by title.
        /// </remarks>
        [HttpGet]
        [ProducesResponseType(typeof(Shared.Response<PaginatedList<AdventureListItemResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllAdventures([FromQuery] GetAdventuresFilters filters)
        {
            var institutionAdminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _adventureService.GetAllAdventuresAsync(institutionAdminId, filters);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Gets detailed view of an Adventure
        /// </summary>
        /// <remarks>
        /// - InstitutionAdmin: Can view any adventure in their institution with full details.
        /// - Child: Can view only Active adventures assigned to their class. 
        ///   If inactive or not assigned → returns IsAccessible = false with message.
        /// </remarks>
        [HttpGet("{adventureId}")]
        public async Task<IActionResult> GetAdventureDetails(string adventureId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _adventureService.GetAdventureDetailsAsync(currentUserId, adventureId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Assigns an existing Adventure to a specific Class (creates WeeklyAdventure)
        /// </summary>
        /// <remarks>
        /// This activates the adventure for the selected class starting from the given date.
        /// </remarks>
        [HttpPost("classes/{classId}/assign-adventure")]
        [ProducesResponseType(typeof(Shared.Response<AssignAdventureToClassResponse>), StatusCodes.Status201Created)]
        public async Task<IActionResult> AssignAdventureToClass(
            string classId,
            [FromBody] AssignAdventureToClassRequest request)
        {
            var institutionAdminId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(institutionAdminId))
                return Unauthorized(_response.Unauthorized<object>("Unauthorized"));

            // نستخدم classId من الـ Route لو عايزين، أو من الـ Body
            request.ClassId = classId;   // لو عايز نأخذه من الـ Route

            var result = await _adventureService.AssignAdventureToClassAsync(institutionAdminId, request);

            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Updates an existing Adventure (title, description, voice, etc.)
        /// </summary>
        /// <remarks>
        /// Can update basic info and re-generate or upload new description voice.
        /// </remarks>
        [HttpPut("{adventureId}")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(Shared.Response<AdventureDetailsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Shared.Response<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateAdventure(string adventureId, [FromForm] UpdateAdventureRequest request)
        {
            var institutionAdminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _adventureService.UpdateAdventureAsync(institutionAdminId, adventureId, request);
            return StatusCode((int)result.StatusCode, result);
        }

        // =============================================
        // DELETE Adventure (Soft Delete)
        // =============================================
        /// <summary>
        /// Soft deletes an Adventure
        /// </summary>
        [HttpDelete("{adventureId}")]
        [ProducesResponseType(typeof(Shared.Response<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Shared.Response<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAdventure(string adventureId)
        {
            var institutionAdminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _adventureService.DeleteAdventureAsync(institutionAdminId, adventureId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Changes the status of an Adventure (Active / Inactive)
        /// </summary>
        [HttpPatch("{adventureId}/status")]
        [ProducesResponseType(typeof(Shared.Response<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Shared.Response<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangeStatus(string adventureId, [FromBody] ChangeAdventureStatusRequest request)
        {
            var institutionAdminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _adventureService.ChangeAdventureStatusAsync(institutionAdminId, adventureId, request.Status);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Gets the class assigned to a specific weekly adventure.
        /// Accessible by InstitutionAdmin and Supervisor.
        /// </summary>
        [HttpGet("{weeklyAdventureId}/classes")]
        [Authorize(Roles = "InstitutionAdmin,Supervisor")]
        public async Task<IActionResult> GetClassesByWeeklyAdventure(string weeklyAdventureId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _adventureService.GetClassesByWeeklyAdventureAsync(userId, weeklyAdventureId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Generates a story for the adventure using AI.
        /// Story voice is processed in the background.
        /// Admin receives a SignalR notification when voice is ready.
        /// </summary>
        [HttpPost("{adventureId}/generate-story")]
        public async Task<IActionResult> GenerateStory(string adventureId)
        {
            var institutionAdminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _adventureService.GenerateStoryAsync(institutionAdminId, adventureId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Gets the full story details of an Adventure (Intro + All Days + Outro)
        /// </summary>
        /// <remarks>
        /// Used for storytelling experience. Returns text and voice URLs for each part.
        /// InstitutionAdmin can view any adventure.
        /// Child can view only if the adventure is assigned and active.
        /// </remarks>
        [HttpGet("{adventureId}/story")]
        [ProducesResponseType(typeof(Shared.Response<AdventureStoryDetailsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAdventureStory(string adventureId)
        {
            var result = await _adventureService.GetAdventureStoryDetailsAsync(adventureId);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}
