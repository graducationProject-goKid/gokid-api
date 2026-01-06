using GoKidAPI.Entity.Account.Users;
using GoKidAPI.Entity.Base;
using GoKidAPI.Entity.Institiution;

namespace GoKidAPI.Entity.Classes
{
    public class Class : AuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = null!;
        public string InstitutionId { get; set; } = null!;
        public Institution Institution { get; set; } = null!;

        public ICollection<Child> Children { get; set; }
        public ICollection<ClassSupervisor> Supervisors { get; set; }
        public ICollection<WeeklyAdventure> WeeklyAdventures { get; set; }
    }
}
