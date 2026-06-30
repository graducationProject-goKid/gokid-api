using GoKidAPI.Entity.Account.Identity;
using GoKidAPI.Enums;

namespace GoKidAPI.Entity
{
    public class Notification
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = null!;
        public AppUser User { get; set; } = null!;

        public NotificationType Type { get; set; }
        public string Title { get; set; } = null!;
        public string Body { get; set; } = null!;
        public string? RelatedEntityId { get; set; } // ChildTaskId or WeeklyAdventureId
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
