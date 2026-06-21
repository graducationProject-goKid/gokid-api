using GoKidAPI.Entity.Account.Users;
using GoKidAPI.Entity.Base;
using GoKidAPI.Enums;

namespace GoKidAPI.Entity
{
    public class PointsTransaction : AuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ChildId { get; set; } = null!;
        public Child Child { get; set; } = null!;

        public int Points { get; set; }
        public string Reason { get; set; } = null!;
        public PointsSourceType SourceType { get; set; }
        public string? SourceEntityId { get; set; } // ChildTaskId or WeeklyAdventureId (ChildAdventureTaskId)
    }
}
