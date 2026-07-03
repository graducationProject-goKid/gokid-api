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

        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? LogoUrl { get; set; }
        public string? LogoPublicId { get; set; }
        public string? Website { get; set; }
        public string? Description { get; set; }

        public string InstitutionAdminId { get; set; } = null!;
        [ForeignKey(nameof(InstitutionAdminId))]
        public InstitutionAdmin Admin { get; set; } = null!;

        public ICollection<Supervisor> Supervisors { get; set; } = new List<Supervisor>();
        public ICollection<Class> Classes { get; set; } = new List<Class>();
        public ICollection<Child> EnrolledChildren { get; set; } = new List<Child>();
    }
}
