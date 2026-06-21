using System.ComponentModel.DataAnnotations.Schema;

using GoKidAPI.Entity.Account.Users;
using GoKidAPI.Entity.Base;
using GoKidAPI.Entity.Classes;

namespace GoKidAPI.Entity.Institiution
{
    public class Institution : AuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!; // SCH-12345

        public string InstitutionAdminId { get; set; } = null!;
        [ForeignKey(nameof(InstitutionAdminId))]
        public InstitutionAdmin Admin { get; set; } = null!;

        public ICollection<Supervisor> Supervisors { get; set; } = new List<Supervisor>();
        public ICollection<Class> Classes { get; set; }
        public ICollection<Child> EnrolledChildren { get; set; } = new List<Child>();
    }
}
