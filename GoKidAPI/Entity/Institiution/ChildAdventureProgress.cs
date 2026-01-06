using System.ComponentModel.DataAnnotations.Schema;

using GoKidAPI.Entity.Account.Users;
using GoKidAPI.Entity.Base;

using NPOI.Util;

namespace GoKidAPI.Entity.Institiution
{
    public class ChildAdventureProgress : AuditableEntity
    {
        public string ChildId { get; set; } = null!;
        [ForeignKey(nameof(ChildId))]
        public Child Child { get; set; } = null!;

        public string WeeklyAdventureId { get; set; } = null!;
        [ForeignKey(nameof(WeeklyAdventureId))]
        public WeeklyAdventure WeeklyAdventure { get; set; } = null!;


        public string DayCompletionJson { get; set; } = "{}"; // { "1": true, "2": false ... }

        public bool WeekBonusAwarded { get; set; } = false;

    }
}
