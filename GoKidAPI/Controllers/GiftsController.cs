using System.Security.Claims;

using GoKidAPI.DTO.Gifts.Requests;
using GoKidAPI.Enums.Gifts;
using GoKidAPI.Services.Gifts;
using GoKidAPI.Services.Rewards;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GoKidAPI.Controllers
{
    // Controllers/GiftsController.cs
    [ApiController]
    [Route("api/gifts")]
    public class GiftsController : ControllerBase
    {
        private readonly IGiftService _giftService;
        public GiftsController(IGiftService giftService) => _giftService = giftService;
        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Platform Admin Endpoints
        /// <summary>
        /// Creates a new platform gift.
        /// Gifts can represent badges, characters, collectibles,
        /// or other purchasable virtual items available for children.
        /// </summary>
        [HttpPost, Authorize(Roles = "PlatformAdmin")]
        public async Task<IActionResult> Create([FromForm] CreateGiftRequest request)
        {
            var result = await _giftService.CreateGiftAsync(request, CurrentUserId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Updates an existing platform gift.
        /// Allows editing gift information such as name, image,
        /// description, points cost, and gift type.
        /// </summary>
        [HttpPut("{giftId}"), Authorize(Roles = "PlatformAdmin")]
        public async Task<IActionResult> Update(string giftId, [FromForm] UpdateGiftRequest request)
        {
            var result = await _giftService.UpdateGiftAsync(giftId, request, CurrentUserId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Soft deletes a platform gift.
        /// Deleted gifts will no longer appear in available listings.
        /// </summary>
        [HttpDelete("{giftId}"), Authorize(Roles = "PlatformAdmin")]
        public async Task<IActionResult> Delete(string giftId)
        {
            var result = await _giftService.DeleteGiftAsync(giftId, CurrentUserId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Changes the status of a gift.
        /// Used to activate or deactivate gifts displayed to children.
        /// </summary>
        [HttpPatch("{giftId}/status"), Authorize(Roles = "PlatformAdmin")]
        public async Task<IActionResult> ChangeStatus(string giftId, [FromQuery] GiftStatus status)
        {
            var result = await _giftService.ChangeGiftStatusAsync(giftId, status, CurrentUserId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Retrieves all platform gifts with pagination and filtering.
        /// Supports filtering by gift type and gift status.
        /// </summary>
        [HttpGet, Authorize(Roles = "PlatformAdmin")]
        public async Task<IActionResult> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] GiftType? type = null,
            [FromQuery] GiftStatus? status = null)
        {
            var result = await _giftService.GetAllGiftsAsync(pageNumber, pageSize, type, status);
            return StatusCode((int)result.StatusCode, result);
        }

        // Child Endpoints
        /// <summary>
        /// Retrieves all active gifts available for purchase by the child.
        /// Includes gifts the child can unlock using earned points.
        /// </summary>
        [HttpGet("available"), Authorize(Roles = "Child")]
        public async Task<IActionResult> GetAvailable(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _giftService.GetAvailableGiftsAsync(CurrentUserId, pageNumber, pageSize);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Purchases a gift using the child's available points.
        /// Deducts the required points while preserving highest score
        /// for leaderboard ranking calculations.
        /// </summary>
        [HttpPost("{giftId}/purchase"), Authorize(Roles = "Child")]
        public async Task<IActionResult> Purchase(string giftId)
        {
            var result = await _giftService.PurchaseGiftAsync(CurrentUserId, giftId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Retrieves all gifts purchased by the current child.
        /// Includes unlocked badges, characters, and collectibles.
        /// </summary>
        [HttpGet("my-gifts"), Authorize(Roles = "Child")]
        public async Task<IActionResult> GetMyGifts()
        {
            var result = await _giftService.GetMyGiftsAsync(CurrentUserId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Returns all gifts purchased by the parent's active child.
        /// </summary>
        [HttpGet("my-child-gifts"), Authorize(Roles = "Parent")]
        public async Task<IActionResult> GetChildGifts()
        {
            var result = await _giftService.GetChildGiftsForParentAsync(CurrentUserId);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}
