using GoKidAPI.Entity.Account.Identity;
using GoKidAPI.Entity.Base;

using NPOI.POIFS.Properties;

namespace GoKidAPI.Entity.Account.Users
{
    public class Parent : AuditableEntity
    {
        public string Id { get; set; } = null!;       // shared PK == AppUser.Id
        public AppUser AppUser { get; set; } = null!;

        //public ICollection<Child> Children { get; set; } = new List<Child>();
        public string? ActiveChildId { get; set; } // MVP: واحد بس نشط
        public Child? ActiveChild { get; set; }
    }
}
