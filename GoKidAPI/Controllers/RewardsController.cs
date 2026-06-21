using GoKidAPI.DTO.Gifts.Requests;
using GoKidAPI.Services.Rewards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace GoKidAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RewardsController : ControllerBase
    {
        private readonly IRewardService _rewardService;
        public RewardsController(IRewardService rewardService) => _rewardService = rewardService;
        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        /// <summary>
        /// Creates a new reward for the parent's child.
        /// Reward includes target points, description, image,
        /// and stays inactive until manually assigned to the child.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateRewardRequest request)
        {
            var result = await _rewardService.CreateRewardAsync(CurrentUserId, request);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Deletes an existing reward created by the parent.
        /// Only rewards owned by the current parent can be removed.
        /// </summary>
        [HttpDelete("{rewardId}")]
        public async Task<IActionResult> Delete(string rewardId)
        {
            var result = await _rewardService.DeleteRewardAsync(CurrentUserId, rewardId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Retrieves all rewards created by the current parent.
        /// Includes assigned and unassigned rewards.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMyRewards()
        {
            var result = await _rewardService.GetMyRewardsAsync(CurrentUserId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Assigns a reward to the child.
        /// Triggered when the parent decides to give the reward
        /// after the child reaches the target milestone.
        /// </summary>
        [HttpPut("{rewardId}/give")]
        public async Task<IActionResult> GiveToChild(string rewardId)
        {
            var result = await _rewardService.GiveRewardToChildAsync(CurrentUserId, rewardId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Retrieves all rewards given to the current child by their parent.
        /// Only shows rewards with status = Given.
        /// </summary>
        [HttpGet("my-rewards")]
        //[Authorize(Roles = "Child")]
        public async Task<IActionResult> GetChildRewards()
        {
            var result = await _rewardService.GetChildRewardsAsync(CurrentUserId);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}
