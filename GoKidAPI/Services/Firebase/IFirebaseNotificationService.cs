namespace GoKidAPI.Services.Firebase
{
    public interface IFirebaseNotificationService
    {
        /// <summary>
        /// Sends a push notification directly to a device FCM token.
        /// </summary>
        Task SendAsync(string fcmToken, string title, string body, Dictionary<string, string>? data = null);

        /// <summary>
        /// Looks up the user's stored FCM token and sends a push notification.
        /// Silently skips if the user has no FCM token registered.
        /// </summary>
        Task SendToUserAsync(string userId, string title, string body, Dictionary<string, string>? data = null);
    }
}
