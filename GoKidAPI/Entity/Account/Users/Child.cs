using GoKidAPI.Entity.Account.Identity;
using GoKidAPI.Entity.Base;
using GoKidAPI.Entity.Classes;
using GoKidAPI.Entity.Institiution;
using GoKidAPI.Entity.Tasks;
using GoKidAPI.Enums;

namespace GoKidAPI.Entity.Account.Users
{
    public class Child : AuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = null!;
        public string? NickName { get; set; }
        public int Age { get; set; }
        public Gender Gender { get; set; }
        public Relationship RelationshipToParent { get; set; } // Who is the parent to the child (e.g., Father, Mother, etc.)
        public string? AvatarUrl { get; set; }

        public string? ParentId { get; set; }
        public AppUser? Parent { get; set; }

        // Code that parent create for his cihld to login 
        public string? RegistrationCode { get; set; } // unique 6 digits
        public DateTime? CodeGeneratedAt { get; set; }

        public int TotalPoints { get; set; } = 0;

        // Institution
        public string? ClassId { get; set; }
        public Class? Class { get; set; }

        public string? InstitutionId { get; set; }
        public Institution? Institution { get; set; }

        // Navigation
        public ICollection<ChildTask>? Tasks { get; set; } 
        public ICollection<ChildAdventureProgress>? AdventureProgresses { get; set; }
        public ICollection<PointsTransaction>? PointsTransactions { get; set; }
        //public ICollection<Notification> Notifications { get; set; }
    }
}
