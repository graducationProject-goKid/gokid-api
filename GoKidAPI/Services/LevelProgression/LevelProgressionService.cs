using GoKidAPI.Data;
using GoKidAPI.Enums;
using GoKidAPI.Services.Notifications;

using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Services.LevelProgression
{
    public class LevelProgressionService : ILevelProgressionService
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<LevelProgressionService> _logger;

        public LevelProgressionService(
            AppDbContext context,
            INotificationService notificationService,
            ILogger<LevelProgressionService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task CheckAndUpdateLevelAsync(string childId, string updatedBy)
        {
            var child = await _context.Childrens
                .Include(c => c.Level)
                .FirstOrDefaultAsync(c => c.Id == childId && !c.IsDeleted);

            if (child == null) return;

            // All active levels, highest MinPoints first
            var levels = await _context.Levels
                .OrderByDescending(l => l.MinPoints)
                .ToListAsync();

            if (!levels.Any()) return;

            // Highest level the child qualifies for
            var newLevel = levels.FirstOrDefault(l => child.TotalPoints >= l.MinPoints);
            if (newLevel == null) return;

            // No change
            if (child.LevelId == newLevel.Id) return;

            // Never downgrade
            if (child.Level != null && newLevel.Order <= child.Level.Order) return;

            var oldLevelName = child.Level?.Name ?? "none";
            child.LevelId = newLevel.Id;
            child.UpdatedAt = DateTime.UtcNow;
            child.UpdatedBy = updatedBy;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Child {ChildId} leveled up from {OldLevel} to {NewLevel}",
                childId, oldLevelName, newLevel.Name);

            // Notify child
            await _notificationService.SendAsync(
                childId,
                NotificationType.LevelUp,
                "Level Up!",
                $"You've reached \"{newLevel.Name}\"! Keep it up!",
                newLevel.Id);

            // Notify parent
            if (child.ParentId != null)
            {
                await _notificationService.SendAsync(
                    child.ParentId,
                    NotificationType.LevelUp,
                    "Your child leveled up!",
                    $"{child.Name} has reached \"{newLevel.Name}\"! Great progress!",
                    newLevel.Id);
            }

            // Notify class supervisors if child is enrolled
            if (child.ClassId != null)
            {
                    await NotifyClassSupervisorsAsync(
                        child.ClassId,
                        NotificationType.LevelUp,
                        "Student Leveled Up!",
                        $"{child.Name} has reached \"{newLevel.Name}\"!",
                        newLevel.Id);
            }
        }

        private async Task NotifyClassSupervisorsAsync(
            string classId,
            NotificationType type,
            string title,
            string body,
            string? relatedEntityId = null)
        {
            var supervisorUserIds = await _context.ClassSupervisors
                .Where(cs => cs.ClassId == classId && !cs.IsDeleted)
                .Select(cs => cs.Supervisor.Id)
                .ToListAsync();

            foreach (var supervisorUserId in supervisorUserIds)
            {
                await _notificationService.SendAsync(
                    supervisorUserId,
                    type,
                    title,
                    body,
                    relatedEntityId);
            }
        }
    }
}
