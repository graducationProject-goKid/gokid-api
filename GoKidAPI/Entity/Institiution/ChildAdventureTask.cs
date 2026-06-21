using System.ComponentModel.DataAnnotations.Schema;

using GoKidAPI.Entity.Account.Users;
using GoKidAPI.Entity.Base;
using GoKidAPI.Enums.Adventures;

namespace GoKidAPI.Entity.Institiution
{
    public class ChildAdventureTask : AuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string ChildId { get; set; } = null!;

        [ForeignKey(nameof(ChildId))]
        public Child Child { get; set; } = null!;

        public string AdventureTaskId { get; set; } = null!;
        [ForeignKey(nameof(AdventureTaskId))]
        public AdventureTask AdventureTask { get; set; } = null!;

        public string WeeklyAdventureId { get; set; } = null!;
        [ForeignKey(nameof(WeeklyAdventureId))]
        public WeeklyAdventure WeeklyAdventure { get; set; } = null!;

        public string? ChildAdventureProgressId { get; set; }
        [ForeignKey(nameof(ChildAdventureProgressId))]
        public ChildAdventureProgress? Progress { get; set; }

        public AdventureChildTaskStatus Status { get; set; } = AdventureChildTaskStatus.Pending;

        public int EarnedStars { get; set; }

        public string? EvidenceUrl { get; set; }

        // Supervisour review 
        public bool? IsApproved { get; set; }
        public string? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }

        // timestamps
        public DateTime? SubmittedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

    }
}
