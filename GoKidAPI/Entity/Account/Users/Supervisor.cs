using GoKidAPI.Entity.Account.Identity;
using GoKidAPI.Entity.Base;
using GoKidAPI.Entity.Classes;

namespace GoKidAPI.Entity.Account.Users
{
    public class Supervisor : AuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string AppUserId { get; set; } = null!;
        public AppUser AppUser { get; set; } = null!;
        public ICollection<ClassSupervisor>? SupervisedClasses { get; set; }
    }
}
