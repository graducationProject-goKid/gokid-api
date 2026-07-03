using System.Security.Claims;

using GoKidAPI.DTO.Levels.Requests;
using GoKidAPI.DTO.Levels.Responses;
using GoKidAPI.Services.Levels;
using GoKidAPI.Shared;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GoKidAPI.Controllers
{
    [ApiController]
    [Route("api/levels")]
    [Authorize(Roles = "PlatformAdmin")]
    public class LevelsController : ControllerBase
    {
        private readonly ILevelService _levelService;
        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        public LevelsController(ILevelService levelService)
        {
            _levelService = levelService;
        }

        /// <summary>
        /// Returns all levels ordered by their progression order.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(Response<List<LevelResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _levelService.GetAllAsync();
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Returns a single level by ID.
        /// </summary>
        [HttpGet("{levelId}")]
        [ProducesResponseType(typeof(Response<LevelResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response<LevelResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetById(string levelId)
        {
            var result = await _levelService.GetByIdAsync(levelId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Creates a new progression level. Order must be unique.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(Response<LevelResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(Response<LevelResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Create([FromForm] CreateLevelRequest request)
        {
            var result = await _levelService.CreateAsync(request, CurrentUserId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Updates an existing level. All fields are optional.
        /// </summary>
        [HttpPut("{levelId}")]
        [ProducesResponseType(typeof(Response<LevelResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response<LevelResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Response<LevelResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Update(string levelId, [FromForm] UpdateLevelRequest request)
        {
            var result = await _levelService.UpdateAsync(levelId, request, CurrentUserId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Soft deletes a level. Fails if any children are currently assigned to it.
        /// </summary>
        [HttpDelete("{levelId}")]
        [ProducesResponseType(typeof(Response<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Response<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Delete(string levelId)
        {
            var result = await _levelService.DeleteAsync(levelId, CurrentUserId);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}
