using GoKidAPI.Entity.Base;

namespace GoKidAPI.Entity.Institiution
{
    public class Adventure : AuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public int WeekDuration { get; set; } = 7;
        public int BonusPoints { get; set; } = 50;

        public ICollection<AdventureTask> Tasks { get; set; } 
        public ICollection<WeeklyAdventure> WeeklyAssignments { get; set; }
    }
}
