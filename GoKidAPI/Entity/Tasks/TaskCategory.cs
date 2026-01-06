using GoKidAPI.Entity.Base;

namespace GoKidAPI.Entity.Tasks
{
    // [Migration format] : [Action - type(col,table) - name]
    public class TaskCategory : AuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string NameAr { get; set; } = null!;
        public string NameEn { get; set; } = null!;     
        public string? ColorHex { get; set; } = "#3498db";
        public string IconUrl { get; set; } = null!;
        public string IconPublicId { get; set; } = null!;

        // Navigation
        public ICollection<TaskSubCategory>? SubCategories { get; set; }
    }
}
