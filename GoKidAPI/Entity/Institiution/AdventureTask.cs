using System.ComponentModel.DataAnnotations.Schema;

using GoKidAPI.Entity.Base;
using GoKidAPI.Entity.Tasks;

using NPOI.Util;

namespace GoKidAPI.Entity.Institiution
{
    public class AdventureTask : AuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string AdventureId { get; set; } = null!;

        [ForeignKey(nameof(AdventureId))]
        public Adventure Adventure { get; set; } = null!;

        public string TaskTemplateId { get; set; } = null!;

        [ForeignKey(nameof(TaskTemplateId))]
        public TaskTemplateBase TaskTemplate { get; set; } = null!;

        public int DayNumber { get; set; }

        public int Stars { get; set; } = 3;

        public string? StoryTitle { get; set; }
        public string? StoryText { get; set; }

        public string? StoryVoiceUrl { get; set; }

        public string? StoryVoicePublicId { get; set; }

        public ICollection<ChildAdventureTask> ChildTasks { get; set; } = new List<ChildAdventureTask>();
    }
}
