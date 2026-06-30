using GoKidAPI.Enums;

namespace GoKidAPI.DTO.Notifications
{
    public class NotificationResponse
    {
        public string Id { get; set; } = null!;
        public NotificationType Type { get; set; }
        public string Title { get; set; } = null!;
        public string Body { get; set; } = null!;
        public string? RelatedEntityId { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
