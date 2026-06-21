using System.ComponentModel.DataAnnotations.Schema;

using GoKidAPI.Entity.Account.Users;
using GoKidAPI.Entity.Base;

using NPOI.Util;

namespace GoKidAPI.Entity.Institiution
{
    public class ChildAdventureProgress : AuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string ChildId { get; set; } = null!;
        public Child Child { get; set; } = null!;

        public string WeeklyAdventureId { get; set; } = null!;
        public WeeklyAdventure WeeklyAdventure { get; set; } = null!;

        // Summary for overall child weekly adventure
        public int EarnedStars { get; set; }
        public int EarnedPoints { get; set; }

        public int CompletedDaysCount { get; set; }

        public bool WeekBonusAwarded { get; set; } = false;

        public bool IsCompleted { get; set; } = false;

        public DateTime? CompletedAt { get; set; }

        public ICollection<ChildAdventureTask> Tasks { get; set; } = new List<ChildAdventureTask>();
    }
}
