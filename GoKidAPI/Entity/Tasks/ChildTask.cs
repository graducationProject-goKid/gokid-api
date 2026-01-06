using GoKidAPI.Entity.Account.Users;
using GoKidAPI.Entity.Base;
using GoKidAPI.Enums.Tasks;

namespace GoKidAPI.Entity.Tasks
{
    public class ChildTask :AuditableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string ChildId { get; set; } = null!;
        public Child Child { get; set; } = null!;

        public string TaskTemplateId { get; set; } = null!;
        public TaskTemplateBase Template { get; set; } = null!;

        public TaskSource Source { get; set; } = TaskSource.SystemGeneral;

        public string? AssignedByParentId { get; set; }
        //public string? AssignedByWeeklyAdventureId { get; set; } // Commented, will be uncommented if needed in future

        public Enums.Tasks.TaskStatus Status { get; set; } = Enums.Tasks.TaskStatus.Pending;

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DueDate { get; set; }

        
        public DateTime? StartedAt { get; set; }
        public DateTime? ReviewRequestedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? RejectionReason { get; set; }

        // Media & AI
        // How many times the child attempted to complete the task,
        // if the task has Maximum Attempts, this will be used to limit the attempts (Voice)
        public int AttemptCount { get; set; } = 0; 
        public string? AnswerText { get; set; }
        public string? AnswerMediaUrl { get; set; }

        //public ICollection<Notification> Notifications { get; set; }
        public ICollection<PointsTransaction> PointsTransactions { get; set; }
    }
}
