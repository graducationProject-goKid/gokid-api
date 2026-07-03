using GoKidAPI.Entity.Base;

namespace GoKidAPI.Entity.Levels
{
    public class Level : AuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = null!;
        public int Order { get; set; }
        public int MinPoints { get; set; }
        public string? BadgeUrl { get; set; }
        public string? BadgePublicId { get; set; }
    }
}
