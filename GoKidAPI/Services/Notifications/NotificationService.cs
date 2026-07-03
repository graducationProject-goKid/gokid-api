using GoKidAPI.Data;
using GoKidAPI.DTO.Notifications;
using GoKidAPI.Entity;
using GoKidAPI.Enums;
using GoKidAPI.Hubs;
using GoKidAPI.Services.Firebase;
using GoKidAPI.Shared;

using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Services.Notifications
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hub;
        private readonly ResponseHandler _response;
        private readonly IFirebaseNotificationService _firebase;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            AppDbContext context,
            IHubContext<NotificationHub> hub,
            ResponseHandler response,
            IFirebaseNotificationService firebase,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _hub = hub;
            _response = response;
            _firebase = firebase;
            _logger = logger;
        }

        public async Task SendAsync(
            string userId,
            NotificationType type,
            string title,
            string body,
            string? relatedEntityId = null)
        {
            // 1. Persist — always, for every role
            var notification = new Notification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Body = body,
                RelatedEntityId = relatedEntityId,
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // 2. Route delivery based on the recipient's UserType:
            //    Mobile users (Parent / Child)      → Firebase push only
            //    Web users   (Admin / Supervisor)   → SignalR only
            var userType = await _context.AppUsers
                .Where(u => u.Id == userId)
                .Select(u => u.UserType)
                .FirstOrDefaultAsync();

            if (userType == UserType.Parent || userType == UserType.Child)
            {
                // Mobile — Firebase push only
                try
                {
                    var data = new Dictionary<string, string>
                    {
                        ["type"]           = type.ToString(),
                        ["notificationId"] = notification.Id,
                    };
                    await _firebase.SendToUserAsync(userId, title, body, data);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Firebase push failed silently for user {UserId}", userId);
                }
            }
            else
            {
                // Web Dashboard — SignalR only
                var payload = new NotificationResponse
                {
                    Id = notification.Id,
                    Type = notification.Type,
                    Title = notification.Title,
                    Body = notification.Body,
                    RelatedEntityId = notification.RelatedEntityId,
                    IsRead = notification.IsRead,
                    CreatedAt = notification.CreatedAt,
                };

                await _hub.Clients.Group(userId).SendAsync("ReceiveNotification", payload);
            }
        }

        public async Task<Response<NotificationListResponse>> GetNotificationsAsync(
    string userId,
    int page,
    int pageSize)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt);

            var totalCount = await query.CountAsync();

            var notifications = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationResponse
                {
                    Id = n.Id,
                    Type = n.Type,
                    Title = n.Title,
                    Body = n.Body,
                    RelatedEntityId = n.RelatedEntityId,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                })
                .ToListAsync();

            var response = new NotificationListResponse
            {
                Notifications = notifications,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                HasNextPage = page * pageSize < totalCount
            };

            return _response.Success(response, "Notifications retrieved successfully.");
        }

        public async Task<Response<bool>> MarkAsReadAsync(string notificationId, string userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification is null)
                return _response.NotFound<bool>("Notification not found.");

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return _response.Success(true, "Notification marked as read.");
        }

        public async Task<Response<bool>> MarkAllAsReadAsync(string userId)
        {
            await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

            return _response.Success(true, "All notifications marked as read.");
        }

        public async Task<Response<int>> GetUnreadCountAsync(string userId)
        {
            var count = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            return _response.Success(count, "Unread count retrieved successfully.");
        }
    }
}
