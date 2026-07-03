using System.Security.Claims;

using GoKidAPI.Services.Notifications;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoKidAPI.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        public NotificationsController(INotificationService notificationService)
            => _notificationService = notificationService;

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        /// <summary>
        /// Returns all notifications for the authenticated user, newest first.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20)
        {
            var result = await _notificationService.GetNotificationsAsync(
                CurrentUserId,
                page,
                pageSize);

            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Returns the count of unread notifications for the authenticated user.
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var result = await _notificationService.GetUnreadCountAsync(CurrentUserId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Marks a single notification as read.
        /// </summary>
        [HttpPatch("{notificationId}/read")]
        public async Task<IActionResult> MarkAsRead(string notificationId)
        {
            var result = await _notificationService.MarkAsReadAsync(notificationId, CurrentUserId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Marks all notifications for the authenticated user as read.
        /// </summary>
        [HttpPatch("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var result = await _notificationService.MarkAllAsReadAsync(CurrentUserId);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}
