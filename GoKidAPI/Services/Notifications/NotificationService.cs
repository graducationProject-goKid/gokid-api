using GoKidAPI.Data;
using GoKidAPI.DTO.Notifications;
using GoKidAPI.Entity;
using GoKidAPI.Enums;
using GoKidAPI.Hubs;
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

        public NotificationService(
            AppDbContext context,
            IHubContext<NotificationHub> hub,
            ResponseHandler response)
        {
            _context = context;
            _hub = hub;
            _response = response;
        }

        public async Task SendAsync(
            string userId,
            NotificationType type,
            string title,
            string body,
            string? relatedEntityId = null)
        {
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

        public async Task<Response<IEnumerable<NotificationResponse>>> GetNotificationsAsync(string userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
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

            return _response.Success<IEnumerable<NotificationResponse>>(notifications, "Notifications retrieved successfully.");
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
