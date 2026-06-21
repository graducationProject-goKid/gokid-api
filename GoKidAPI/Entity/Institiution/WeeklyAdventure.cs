using System.ComponentModel.DataAnnotations.Schema;

using GoKidAPI.Entity.Base;
using GoKidAPI.Entity.Classes;
using GoKidAPI.Enums.Adventures;

namespace GoKidAPI.Entity.Institiution
{
    public class WeeklyAdventure : AuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string ClassId { get; set; } = null!;

        [ForeignKey(nameof(ClassId))]
        public Class Class { get; set; } = null!;

        public string AdventureId { get; set; } = null!;

        [ForeignKey(nameof(AdventureId))]
        public Adventure Adventure { get; set; } = null!;

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public WeeklyAdventureStatus Status { get; set; } = WeeklyAdventureStatus.Active;
        public ICollection<ChildAdventureTask> ChildTasks { get; set; } = new List<ChildAdventureTask>();

        public ICollection<ChildAdventureProgress> Progresses { get; set; } = new List<ChildAdventureProgress>();
    }
}
