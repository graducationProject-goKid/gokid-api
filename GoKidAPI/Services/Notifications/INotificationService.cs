using GoKidAPI.DTO.Notifications;
using GoKidAPI.Enums;
using GoKidAPI.Shared;

namespace GoKidAPI.Services.Notifications
{
    public interface INotificationService
    {
        /// <summary>
        /// Saves a notification to the database and pushes it via SignalR to the target user.
        /// </summary>
        Task SendAsync(string userId, NotificationType type, string title, string body, string? relatedEntityId = null);

        /// <summary>
        /// Returns all notifications for the authenticated user, newest first.
        /// </summary>
        Task<Response<IEnumerable<NotificationResponse>>> GetNotificationsAsync(string userId);

        /// <summary>
        /// Marks a single notification as read. Returns NotFound if it doesn't belong to the user.
        /// </summary>
        Task<Response<bool>> MarkAsReadAsync(string notificationId, string userId);

        /// <summary>
        /// Marks all unread notifications for the user as read.
        /// </summary>
        Task<Response<bool>> MarkAllAsReadAsync(string userId);

        /// <summary>
        /// Returns the count of unread notifications for the user.
        /// </summary>
        Task<Response<int>> GetUnreadCountAsync(string userId);
    }
}
