// Services/Supervisor/SupervisorService.cs
using GoKidAPI.Data;
using GoKidAPI.DTO.Childs.Responses;
using GoKidAPI.DTO.Levels.Responses;
using GoKidAPI.DTO.Supervisor.Requests;
using GoKidAPI.DTO.Supervisor.Responses;
using GoKidAPI.Entity;
using GoKidAPI.Entity.Institiution;
using GoKidAPI.Enums;
using GoKidAPI.Enums.Adventures;
using GoKidAPI.Services.LevelProgression;
using GoKidAPI.Services.Notifications;
using GoKidAPI.Shared;

using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Services.Supervisor
{
    public class SupervisorService : ISupervisorService
    {
        private readonly AppDbContext _context;
        private readonly ResponseHandler _response;
        private readonly ILogger<SupervisorService> _logger;
        private readonly INotificationService _notificationService;
        private readonly ILevelProgressionService _levelProgression;

        public SupervisorService(
            AppDbContext context,
            ResponseHandler response,
            ILogger<SupervisorService> logger,
            INotificationService notificationService,
            ILevelProgressionService levelProgression)
        {
            _context = context;
            _response = response;
            _logger = logger;
            _notificationService = notificationService;
            _levelProgression = levelProgression;
        }

        public async Task<Response<List<SupervisorAdventureListResponse>>> GetMyAdventuresAsync(string supervisorUserId)
        {
            // جيب الـ Supervisor entity من الـ Id (shared PK with AppUser)
            var supervisor = await _context.Supervisors
                .FirstOrDefaultAsync(s => s.Id == supervisorUserId && !s.IsDeleted);

            if (supervisor == null)
                return _response.NotFound<List<SupervisorAdventureListResponse>>("Supervisor not found");

            // جيب الكلاسات اللي الـ Supervisor مسؤول عنها
            var myClassIds = await _context.ClassSupervisors
                .Where(cs => cs.SupervisorId == supervisor.Id && !cs.IsDeleted)
                .Select(cs => cs.ClassId)
                .ToListAsync();

            if (!myClassIds.Any())
                return _response.Success(new List<SupervisorAdventureListResponse>(), "No classes assigned yet");

            // جيب الـ WeeklyAdventures المفعلة على الكلاسات دي
            var adventures = await _context.WeeklyAdventures
                .Include(wa => wa.Adventure)
                .Include(wa => wa.Class)
                    .ThenInclude(c => c.Children)
                .Where(wa => myClassIds.Contains(wa.ClassId)
                          && wa.Status == WeeklyAdventureStatus.Active
                          && !wa.IsDeleted)
                .ToListAsync();

            var result = adventures.Select(wa => new SupervisorAdventureListResponse
            {
                WeeklyAdventureId = wa.Id,
                AdventureId = wa.AdventureId,
                TitleEn = wa.Adventure.TitleEn,
                TitleAr = wa.Adventure.TitleAr,
                ClassName = wa.Class.Name,
                ClassId = wa.ClassId,
                StartDate = wa.StartDate,
                EndDate = wa.EndDate,
                TotalChildren = wa.Class.Children.Count(c => !c.IsDeleted),
                // عدد التاسكات اللي في حالة Pending review (submitted لكن ملقتش review لسه)
                PendingReviewsCount = _context.ChildAdventureTasks
                    .Count(cat => cat.WeeklyAdventureId == wa.Id
                               && cat.Status == AdventureChildTaskStatus.Pending
                               && cat.IsApproved == null
                               && cat.EvidenceUrl != null)
            }).ToList();

            return _response.Success(result, "Adventures retrieved successfully");
        }

        public async Task<Response<PaginatedList<ChildAdventureTaskReviewResponse>>> GetAdventureChildTasksAsync(
            string supervisorUserId,
            string weeklyAdventureId,
            AdventureChildTaskStatus? statusFilter,
            int pageNumber,
            int pageSize)
        {
            // تحقق إن الـ Supervisor عنده صلاحية على الـ WeeklyAdventure دي
            var hasAccess = await SupervisorHasAccessToWeeklyAdventureAsync(supervisorUserId, weeklyAdventureId);
            if (!hasAccess)
                return _response.Forbidden<PaginatedList<ChildAdventureTaskReviewResponse>>(
                    "You do not have access to this adventure");

            var query = _context.ChildAdventureTasks
                .Include(cat => cat.Child)
                    .ThenInclude(c => c.Level)
                .Include(cat => cat.AdventureTask)
                    .ThenInclude(at => at.TaskTemplate)
                .Where(cat => cat.WeeklyAdventureId == weeklyAdventureId && !cat.IsDeleted);

            // فلتر بالـ status لو موجود
            if (statusFilter.HasValue)
                query = query.Where(cat => cat.Status == statusFilter.Value);

            var totalCount = await query.CountAsync();

            var rawTasks = await query
                .OrderByDescending(cat => cat.SubmittedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var tasks = rawTasks.Select(cat => new ChildAdventureTaskReviewResponse
            {
                ChildAdventureTaskId = cat.Id,
                ChildId = cat.ChildId,
                ChildName = cat.Child.Name,
                ChildAvatarUrl = cat.Child.AvatarUrl,
                Level = cat.Child.Level != null ? new LevelInfo
                {
                    Id = cat.Child.Level.Id,
                    Name = cat.Child.Level.Name,
                    Order = cat.Child.Level.Order,
                    BadgeUrl = cat.Child.Level.BadgeUrl
                } : null,
                DayNumber = cat.AdventureTask.DayNumber,
                TaskTitleEn = cat.AdventureTask.TaskTemplate.TitleEn,
                TaskTitleAr = cat.AdventureTask.TaskTemplate.TitleAr,
                EvidenceUrl = cat.EvidenceUrl,
                Status = cat.Status,
                SubmittedAt = cat.SubmittedAt,
                IsApproved = cat.IsApproved,
                ReviewedBy = cat.ReviewedBy,
                ReviewedAt = cat.ReviewedAt
            }).ToList();

            var paginated = new PaginatedList<ChildAdventureTaskReviewResponse>(
                tasks, pageNumber, pageSize, totalCount);

            return _response.Success(paginated, "Tasks retrieved successfully");
        }

        public async Task<Response<ChildAdventureTaskReviewResponse>> ReviewChildTaskAsync(
            string supervisorUserId,
            string childAdventureTaskId,
            ReviewAdventureTaskRequest request)
        {
            var childTask = await _context.ChildAdventureTasks
                .Include(cat => cat.Child)
                    .ThenInclude(c => c.Level)
                .Include(cat => cat.AdventureTask)
                    .ThenInclude(at => at.TaskTemplate)
                .Include(cat => cat.Progress)
                .Include(cat => cat.WeeklyAdventure)
                    .ThenInclude(wa => wa.Adventure)
                .FirstOrDefaultAsync(cat => cat.Id == childAdventureTaskId && !cat.IsDeleted);

            if (childTask == null)
                return _response.NotFound<ChildAdventureTaskReviewResponse>("Task not found");

            // تحقق إن الـ Supervisor عنده صلاحية على الـ WeeklyAdventure دي
            var hasAccess = await SupervisorHasAccessToWeeklyAdventureAsync(
                supervisorUserId, childTask.WeeklyAdventureId);

            if (!hasAccess)
                return _response.Forbidden<ChildAdventureTaskReviewResponse>(
                    "You do not have access to review this task");

            // تحقق إن التاسك في حالة ممكن تتعمل review عليها
            //if (childTask.IsApproved != null)
            //    return _response.BadRequest<ChildAdventureTaskReviewResponse>("Task already reviewed");

            if (childTask.EvidenceUrl == null)
                return _response.BadRequest<ChildAdventureTaskReviewResponse>(
                    "Task has no evidence to review");

            // عمل الـ Review
            childTask.IsApproved = request.IsApproved;
            childTask.ReviewedBy = supervisorUserId;
            childTask.ReviewedAt = DateTime.UtcNow;
            childTask.UpdatedAt = DateTime.UtcNow;
            childTask.UpdatedBy = supervisorUserId;

            if (request.IsApproved)
            {
                childTask.Status = AdventureChildTaskStatus.Completed;
                childTask.CompletedAt = DateTime.UtcNow;
                childTask.EarnedStars = childTask.AdventureTask.Stars;

                // ادي الطفل نقاط
                var basePoints = childTask.AdventureTask.TaskTemplate.BasePoints;
                childTask.Child.TotalPoints += basePoints;

                _context.PointsTransactions.Add(new PointsTransaction
                {
                    ChildId = childTask.ChildId,
                    Points = basePoints,
                    Reason = $"Completed Adventure Task: {childTask.AdventureTask.TaskTemplate.TitleEn}",
                    SourceType = PointsSourceType.ChildTask,
                    SourceEntityId = childTask.Id
                });

                // حدّث الـ Progress
                if (childTask.Progress != null)
                {
                    childTask.Progress.EarnedStars += childTask.EarnedStars;
                    childTask.Progress.EarnedPoints += basePoints;
                    childTask.Progress.CompletedDaysCount++;

                    // لو اتكملت كل تاسكات الأدفنتشر → award bonus
                    await CheckAndAwardAdventureBonusAsync(childTask);
                }
            }
            else
            {
                // Rejected → يرجع Pending عشان الطفل يقدر يعيد
                childTask.Status = AdventureChildTaskStatus.Pending;
                childTask.EvidenceUrl = null; // امسح الـ evidence القديمة
            }

            await _context.SaveChangesAsync();

            if (request.IsApproved)
                await _levelProgression.CheckAndUpdateLevelAsync(childTask.ChildId, supervisorUserId);

            _logger.LogInformation(
                "Supervisor {SupervisorId} {Action} task {TaskId} for child {ChildId}",
                supervisorUserId,
                request.IsApproved ? "approved" : "rejected",
                childAdventureTaskId,
                childTask.ChildId);

            if (request.IsApproved)
            {
                await _notificationService.SendAsync(
                    childTask.ChildId,
                    NotificationType.TaskApproved,
                    "Adventure Task Approved!",
                    $"Day {childTask.AdventureTask.DayNumber} of \"{childTask.WeeklyAdventure.Adventure.TitleEn}\" approved. " +
                    $"You earned {childTask.AdventureTask.TaskTemplate.BasePoints} points!",
                    childTask.WeeklyAdventureId);
            }
            else
            {
                await _notificationService.SendAsync(
                    childTask.ChildId,
                    NotificationType.TaskRejected,
                    "Try Again!",
                    $"Your supervisor sent back day {childTask.AdventureTask.DayNumber} of " +
                    $"\"{childTask.WeeklyAdventure.Adventure.TitleEn}\". Re-Submit your Task.",
                    childTask.WeeklyAdventureId);
            }

            var result = new ChildAdventureTaskReviewResponse
            {
                ChildAdventureTaskId = childTask.Id,
                ChildId = childTask.ChildId,
                ChildName = childTask.Child.Name,
                ChildAvatarUrl = childTask.Child.AvatarUrl,
                Level = childTask.Child.Level != null ? new LevelInfo
                {
                    Id = childTask.Child.Level.Id,
                    Name = childTask.Child.Level.Name,
                    Order = childTask.Child.Level.Order,
                    BadgeUrl = childTask.Child.Level.BadgeUrl
                } : null,
                DayNumber = childTask.AdventureTask.DayNumber,
                TaskTitleEn = childTask.AdventureTask.TaskTemplate.TitleEn,
                TaskTitleAr = childTask.AdventureTask.TaskTemplate.TitleAr,
                EvidenceUrl = childTask.EvidenceUrl,
                Status = childTask.Status,
                SubmittedAt = childTask.SubmittedAt,
                IsApproved = childTask.IsApproved,
                ReviewedBy = childTask.ReviewedBy,
                ReviewedAt = childTask.ReviewedAt
            };

            return _response.Success(result,
                request.IsApproved ? "Task approved successfully" : "Task rejected");
        }

        public async Task<Response<List<ClassChildrenListResponse>>> GetClassChildrenProgressAsync(
            string supervisorUserId,
            string weeklyAdventureId,
            string classId)
        {
            // تحقق إن الـ Supervisor عنده access على الـ WeeklyAdventure دي
            var hasAccess = await SupervisorHasAccessToWeeklyAdventureAsync(supervisorUserId, weeklyAdventureId);
            if (!hasAccess)
                return _response.Forbidden<List<ClassChildrenListResponse>>(
                    "You do not have access to this adventure");

            // تحقق إن الـ class ده فعلاً تابع للـ WeeklyAdventure دي
            var weeklyAdventure = await _context.WeeklyAdventures
                .FirstOrDefaultAsync(wa => wa.Id == weeklyAdventureId && wa.ClassId == classId && !wa.IsDeleted);

            if (weeklyAdventure == null)
                return _response.NotFound<List<ClassChildrenListResponse>>(
                    "This class is not associated with the selected adventure");

            // جيب كل الأطفال في الكلاس ده
            var children = await _context.Childrens
                .Include(c => c.Level)
                .Where(c => c.ClassId == classId && !c.IsDeleted)
                .ToListAsync();

            if (!children.Any())
                return _response.Success(new List<ClassChildrenListResponse>(), "No children in this class");

            var childIds = children.Select(c => c.Id).ToList();

            // جيب عدد التاسكات الكلية في الـ Adventure دي
            var totalTasksCount = await _context.AdventureTasks
                .CountAsync(at => at.AdventureId == weeklyAdventure.AdventureId && !at.IsDeleted);

            // جيب كل الـ ChildAdventureTasks للأطفال دول في الـ WeeklyAdventure دي
            var allChildTasks = await _context.ChildAdventureTasks
                .Where(cat => childIds.Contains(cat.ChildId)
                           && cat.WeeklyAdventureId == weeklyAdventureId
                           && !cat.IsDeleted)
                .ToListAsync();

            // جيب الـ Progress لكل طفل
            var allProgresses = await _context.ChildAdventureProgresses
                .Where(p => childIds.Contains(p.ChildId)
                         && p.WeeklyAdventureId == weeklyAdventureId)
                .ToDictionaryAsync(p => p.ChildId);

            var result = children.Select(child =>
            {
                var childTasks = allChildTasks.Where(t => t.ChildId == child.Id).ToList();
                allProgresses.TryGetValue(child.Id, out var progress);

                return new ClassChildrenListResponse
                {
                    ChildId = child.Id,
                    ChildName = child.Name,
                    ChildAvatarUrl = child.AvatarUrl,
                    Age = child.Age,
                    TotalTasksCount = totalTasksCount,
                    SubmittedTasksCount = childTasks.Count(t => t.SubmittedAt != null),
                    CompletedTasksCount = childTasks.Count(t => t.Status == AdventureChildTaskStatus.Completed),
                    EarnedStars = progress?.EarnedStars ?? 0,
                    EarnedPoints = progress?.EarnedPoints ?? 0,
                    IsAdventureCompleted = progress?.IsCompleted ?? false,
                    Level = child.Level != null ? new LevelInfo
                    {
                        Id = child.Level.Id,
                        Name = child.Level.Name,
                        Order = child.Level.Order,
                        BadgeUrl = child.Level.BadgeUrl
                    } : null
                };
            })
            .OrderByDescending(c => c.CompletedTasksCount)  // الأكثر إنجازاً أولاً
            .ToList();

            return _response.Success(result, "Class children progress retrieved successfully");
        }

        public async Task<Response<ChildAdventureHistoryResponse>> GetChildAdventureHistoryAsync(
            string supervisorUserId,
            string weeklyAdventureId,
            string childId)
        {
            // تحقق إن الـ Supervisor عنده access
            var hasAccess = await SupervisorHasAccessToWeeklyAdventureAsync(supervisorUserId, weeklyAdventureId);
            if (!hasAccess)
                return _response.Forbidden<ChildAdventureHistoryResponse>(
                    "You do not have access to this adventure");

            // جيب الطفل
            var child = await _context.Childrens
                .Include(c => c.Level)
                .FirstOrDefaultAsync(c => c.Id == childId && !c.IsDeleted);

            if (child == null)
                return _response.NotFound<ChildAdventureHistoryResponse>("Child not found");

            // جيب الـ Progress
            var progress = await _context.ChildAdventureProgresses
                .FirstOrDefaultAsync(p => p.ChildId == childId
                                       && p.WeeklyAdventureId == weeklyAdventureId);

            // جيب كل تاسكات الطفل في الـ adventure دي مع التفاصيل
            var childTasks = await _context.ChildAdventureTasks
                .Include(cat => cat.AdventureTask)
                    .ThenInclude(at => at.TaskTemplate)
                .Where(cat => cat.ChildId == childId
                           && cat.WeeklyAdventureId == weeklyAdventureId
                           && !cat.IsDeleted)
                .OrderBy(cat => cat.AdventureTask.DayNumber)
                .ToListAsync();

            // جيب عدد التاسكات الكلية في الـ Adventure
            var weeklyAdventure = await _context.WeeklyAdventures
                .FirstOrDefaultAsync(wa => wa.Id == weeklyAdventureId);

            var totalTasks = await _context.AdventureTasks
                .CountAsync(at => at.AdventureId == weeklyAdventure!.AdventureId && !at.IsDeleted);

            var result = new ChildAdventureHistoryResponse
            {
                ChildId = child.Id,
                ChildName = child.Name,
                ChildAvatarUrl = child.AvatarUrl,
                Level = child.Level != null ? new LevelInfo
                {
                    Id = child.Level.Id,
                    Name = child.Level.Name,
                    Order = child.Level.Order,
                    BadgeUrl = child.Level.BadgeUrl
                } : null,

                // Summary من الـ Progress
                TotalTasks = totalTasks,
                CompletedTasks = progress?.CompletedDaysCount ?? 0,
                PendingTasks = childTasks.Count(t => t.Status == AdventureChildTaskStatus.Pending),
                MissedTasks = childTasks.Count(t => t.Status == AdventureChildTaskStatus.Missed),
                EarnedStars = progress?.EarnedStars ?? 0,
                EarnedPoints = progress?.EarnedPoints ?? 0,
                IsAdventureCompleted = progress?.IsCompleted ?? false,

                // التاسكات بالتفاصيل
                Tasks = childTasks.Select(cat => new ChildAdventureTaskDetailResponse
                {
                    ChildAdventureTaskId = cat.Id,
                    DayNumber = cat.AdventureTask.DayNumber,
                    TaskTitleEn = cat.AdventureTask.TaskTemplate.TitleEn,
                    TaskTitleAr = cat.AdventureTask.TaskTemplate.TitleAr,
                    TaskImageUrl = cat.AdventureTask.TaskTemplate.TaskImageUrl,
                    StoryText = cat.AdventureTask.StoryText,
                    StoryVoiceUrl = cat.AdventureTask.StoryVoiceUrl,
                    Status = cat.Status,
                    EvidenceUrl = cat.EvidenceUrl,
                    EarnedStars = cat.EarnedStars,
                    IsApproved = cat.IsApproved,
                    ReviewedBy = cat.ReviewedBy,
                    SubmittedAt = cat.SubmittedAt,
                    CompletedAt = cat.CompletedAt
                }).ToList()
            };

            return _response.Success(result, "Child adventure history retrieved successfully");
        }

        public async Task<Response<List<SupervisorClassResponse>>> GetMyClassesAsync(string supervisorUserId)
        {
            var supervisor = await _context.Supervisors
                .FirstOrDefaultAsync(s => s.Id == supervisorUserId && !s.IsDeleted);

            if (supervisor == null)
                return _response.NotFound<List<SupervisorClassResponse>>("Supervisor not found");

            var classes = await _context.ClassSupervisors
                .Include(cs => cs.Class)
                    .ThenInclude(c => c.Institution)
                .Include(cs => cs.Class)
                    .ThenInclude(c => c.Children)
                .Include(cs => cs.Class)
                    .ThenInclude(c => c.WeeklyAdventures)
                .Where(cs => cs.SupervisorId == supervisor.Id && !cs.IsDeleted)
                .Select(cs => new SupervisorClassResponse
                {
                    ClassId = cs.Class.Id,
                    Name = cs.Class.Name,
                    InstitutionName = cs.Class.Institution.Name,
                    ChildrenCount = cs.Class.Children.Count(c => !c.IsDeleted),
                    ActiveAdventuresCount = cs.Class.WeeklyAdventures
                        .Count(wa => wa.Status == WeeklyAdventureStatus.Active && !wa.IsDeleted)
                })
                .ToListAsync();

            return _response.Success(classes, "Classes retrieved successfully");
        }


        // ========== Private Helpers ==========

        private async Task<bool> SupervisorHasAccessToWeeklyAdventureAsync(
            string supervisorUserId,
            string weeklyAdventureId)
        {
            var supervisor = await _context.Supervisors
                .FirstOrDefaultAsync(s => s.Id == supervisorUserId && !s.IsDeleted);

            if (supervisor == null) return false;

            // جيب الـ WeeklyAdventure وشوف لو الـ class بتاعها تحت إشراف الـ Supervisor ده
            var weeklyAdventure = await _context.WeeklyAdventures
                .FirstOrDefaultAsync(wa => wa.Id == weeklyAdventureId && !wa.IsDeleted);

            if (weeklyAdventure == null) return false;

            return await _context.ClassSupervisors
                .AnyAsync(cs => cs.SupervisorId == supervisor.Id
                             && cs.ClassId == weeklyAdventure.ClassId
                             && !cs.IsDeleted);
        }

        private async Task CheckAndAwardAdventureBonusAsync(ChildAdventureTask childTask)
        {
            if (childTask.Progress == null) return;

            // جيب عدد تاسكات الأدفنتشر الكلية
            var totalTasks = await _context.AdventureTasks
                .CountAsync(at => at.AdventureId == childTask.AdventureTask.AdventureId && !at.IsDeleted);

            // لو الطفل خلّص كلهم وال bonus ملمّش يتادّى
            if (childTask.Progress.CompletedDaysCount >= totalTasks
                && !childTask.Progress.WeekBonusAwarded)
            {
                var weeklyAdventure = await _context.WeeklyAdventures
                    .Include(wa => wa.Adventure)
                    .FirstOrDefaultAsync(wa => wa.Id == childTask.WeeklyAdventureId);

                if (weeklyAdventure == null) return;

                var bonusPoints = weeklyAdventure.Adventure.BonusPoints;

                childTask.Child.TotalPoints += bonusPoints;
                childTask.Progress.WeekBonusAwarded = true;
                childTask.Progress.IsCompleted = true;
                childTask.Progress.CompletedAt = DateTime.UtcNow;

                _context.PointsTransactions.Add(new PointsTransaction
                {
                    ChildId = childTask.ChildId,
                    Points = bonusPoints,
                    Reason = $"Adventure Completion Bonus: {weeklyAdventure.Adventure.TitleEn}",
                    SourceType = PointsSourceType.ChildTask,
                    SourceEntityId = childTask.WeeklyAdventureId
                });

                await _notificationService.SendAsync(
                    childTask.ChildId,
                    NotificationType.WeekBonus,
                    "Adventure Complete!",
                    $"You finished \"{weeklyAdventure.Adventure.TitleEn}\"! Bonus: +{bonusPoints} points. Amazing!",
                    childTask.WeeklyAdventureId);
            }
        }
    }
}