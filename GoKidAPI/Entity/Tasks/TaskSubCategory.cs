    using GoKidAPI.Entity.Base;

    namespace GoKidAPI.Entity.Tasks
    {
        public class TaskSubCategory :AuditableEntity
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();

            public string NameAr { get; set; } = null!;
            public string NameEn { get; set; } = null!;
            public string? IconUrl { get; set; }
            public string? IconPublicId { get; set; }

            // Foreign Key
            public string CategoryId { get; set; } = null!;
            public TaskCategory Category { get; set; } = null!;

            // Navigation
            public ICollection<TaskTemplateBase>? Templates { get; set; }
    }
}
