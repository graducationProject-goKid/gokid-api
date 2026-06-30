using FirebaseAdmin.Messaging;

using GoKidAPI.Data;

using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Services.Firebase
{
    public class FirebaseNotificationService : IFirebaseNotificationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<FirebaseNotificationService> _logger;

        public FirebaseNotificationService(AppDbContext context, ILogger<FirebaseNotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SendAsync(string fcmToken, string title, string body, Dictionary<string, string>? data = null)
        {
            var message = new Message
            {
                Token = fcmToken,
                Notification = new Notification
                {
                    Title = title,
                    Body = body,
                },
                Data = data ?? new Dictionary<string, string>(),
            };

            try
            {
                var result = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                _logger.LogInformation("FCM message sent. MessageId: {MessageId}", result);
            }
            catch (FirebaseMessagingException ex)
            {
                _logger.LogWarning("FCM send failed. Token: {Token}, Error: {Error}", fcmToken, ex.Message);
            }
        }

        public async Task SendToUserAsync(string userId, string title, string body, Dictionary<string, string>? data = null)
        {
            var fcmToken = await _context.AppUsers
                .Where(u => u.Id == userId)
                .Select(u => u.FcmToken)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(fcmToken))
                return;

            await SendAsync(fcmToken, title, body, data);
        }
    }
}
