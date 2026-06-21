using System.ComponentModel.DataAnnotations.Schema;

using GoKidAPI.Entity.Account.Identity;
using GoKidAPI.Entity.Base;

namespace GoKidAPI.Entity.Institiution
{
    public class InstitutionAdmin : AuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string AppUserId { get; set; } = null!;
        public AppUser AppUser { get; set; } = null!;

        public string? InstitutionId { get; set; }
        [ForeignKey(nameof(InstitutionId))]
        public Institution? Institution { get; set; }
    }
}
