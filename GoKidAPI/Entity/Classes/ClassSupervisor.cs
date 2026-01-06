using System.ComponentModel.DataAnnotations.Schema;

using GoKidAPI.Entity.Account.Users;
using GoKidAPI.Entity.Base;

namespace GoKidAPI.Entity.Classes
{
    public class ClassSupervisor : AuditableEntity
    {
        public string ClassId { get; set; } = null!;
        [ForeignKey(nameof(ClassId))]
        public Class Class { get; set; } = null!;

        public string SupervisorId { get; set; } = null!;
        [ForeignKey(nameof(SupervisorId))]
        public Supervisor Supervisor { get; set; } = null!;

    }
}
