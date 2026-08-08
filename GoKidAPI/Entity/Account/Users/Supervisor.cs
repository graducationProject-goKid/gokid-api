using System.ComponentModel.DataAnnotations.Schema;

using GoKidAPI.Entity.Account.Identity;
using GoKidAPI.Entity.Base;
using GoKidAPI.Entity.Classes;
using GoKidAPI.Entity.Institiution;

namespace GoKidAPI.Entity.Account.Users
{
    public class Supervisor : AuditableEntity
    {
        public string Id { get; set; } = null!;       // shared PK == AppUser.Id
        public AppUser AppUser { get; set; } = null!;

        public string InstitutionId { get; set; } = null!;
        [ForeignKey(nameof(InstitutionId))]
        public Institution Institution { get; set; } = null!;
        public ICollection<ClassSupervisor>? SupervisedClasses { get; set; }
    }
}
