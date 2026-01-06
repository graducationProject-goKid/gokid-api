using System.ComponentModel.DataAnnotations.Schema;

using GoKidAPI.Entity.Base;
using GoKidAPI.Entity.Tasks;

using NPOI.Util;

namespace GoKidAPI.Entity.Institiution
{
    public class AdventureTask : AuditableEntity
    {
        public string AdventureId { get; set; } = null!;
        [ForeignKey(nameof(AdventureId))]
        public Adventure Adventure { get; set; } = null!;

        public string TaskTemplateId { get; set; } = null!;
        [ForeignKey(nameof(TaskTemplateId))]
        public TaskTemplateBase Task { get; set; } = null!;

        public int DayNumber { get; set; } // 1-7

    }
}
