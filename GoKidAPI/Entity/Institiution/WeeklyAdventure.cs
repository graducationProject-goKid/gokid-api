using GoKidAPI.Entity.Base;
using GoKidAPI.Entity.Classes;

namespace GoKidAPI.Entity.Institiution
{
    public class WeeklyAdventure : AuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ClassId { get; set; } = null!;
        public string AdventureId { get; set; } = null!;
        public DateTime StartDate { get; set; }

        public Class Class { get; set; } = null!;
        public Adventure Adventure { get; set; } = null!;

        public ICollection<ChildAdventureProgress> Progresses { get; set; }
    }
}
