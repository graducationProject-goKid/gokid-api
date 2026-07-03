using GoKidAPI.Data;
using GoKidAPI.DTO.Tasks.Requests;
using GoKidAPI.DTO.Tasks.Responses;
using GoKidAPI.Entity;
using GoKidAPI.Entity.Account.Users;
using GoKidAPI.Entity.Tasks;
using GoKidAPI.Enums;
using GoKidAPI.Enums.Shared;
using GoKidAPI.Enums.Tasks;
using GoKidAPI.Services.Notifications;
using GoKidAPI.Shared;

using Microsoft.EntityFrameworkCore;

using NPOI.POIFS.Properties;

using TaskStatus = GoKidAPI.Enums.Tasks.TaskStatus;

namespace GoKidAPI.Services.ParentTasks
{
    public class ParentTaskService : IParentTaskService
    {
        private readonly AppDbContext _context;
        private readonly ResponseHandler _response;
        private readonly ILogger<ParentTaskService> _logger;
        private readonly INotificationService _notificationService;

        public ParentTaskService(
            AppDbContext context,
            ResponseHandler response,
            ILogger<ParentTaskService> logger,
            INotificationService notificationService)
        {
            _context = context;
            _response = response;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<Response<AssignTaskResponse>> AssignTaskToChildAsync(string parentId, AssignTaskRequest request)
        {
            try
            {
                // جيب الأب وتأكد إن عنده طفل نشط (MVP: واحد بس)
                var parent = await _context.Parents
                    .Include(p => p.ActiveChild)
                    .FirstOrDefaultAsync(p => p.Id == parentId);

                if (parent == null || parent.ActiveChild ==null )
                    return _response.BadRequest<AssignTaskResponse>("No child linked to your account");

                //var child = parent.Children.First(); // MVP: Only one child per parent 

                // Check if task template exists
                var template = await _context.TaskTemplates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == request.TaskTemplateId);

                if (template == null)
                    return _response.NotFound<AssignTaskResponse>("Task template not found");

                // Check if the task is already assigned to the child by this parent, if yes, prevent duplicate assignment
                // if the existing task is not yet completed and due date is either null or in the future
                var alreadyAssigned = await _context.ChildTasks
                    .AnyAsync(ct => ct.ChildId == parent.ActiveChildId
                                 && ct.TaskTemplateId == request.TaskTemplateId
                                 && ct.Source == TaskSource.Parent && (ct.DueDate == null || ct.DueDate >= DateTime.UtcNow));

                if (alreadyAssigned)
                    return _response.BadRequest<AssignTaskResponse>("This task is already assigned to your child");

                var childTask = new ChildTask
                {
                    ChildId = parent.ActiveChildId,
                    TaskTemplateId = request.TaskTemplateId,
                    Source = TaskSource.Parent,
                    AssignedByParentId = parentId,
                    DueDate = request.DueDate?.Date.AddDays(1).AddSeconds(-1), // End of the day
                    Status = TaskStatus.Pending,
                    AssignedAt = DateTime.UtcNow,
                    CreatedBy = parentId,
                };

                _context.ChildTasks.Add(childTask);
                await _context.SaveChangesAsync();

                var resp = new AssignTaskResponse
                {
                    ChildTaskId = childTask.Id,
                    TaskTemplateId = template.Id,
                    TitleAr = template.TitleAr,
                    TitleEn = template.TitleEn,
                    DueDate = childTask.DueDate,
                    AssignedAt = childTask.AssignedAt
                };

                _logger.LogInformation("Parent {ParentId} assigned task {TaskId} to child {ChildId}", parentId, template.Id, parent.ActiveChildId);

                await _notificationService.SendAsync(
                    parent.ActiveChildId!,
                    NotificationType.TaskAssigned,
                    "New Task!",
                    $"Your parent assigned you \"{template.TitleEn}\".",
                    childTask.Id);

                return _response.Created(resp, "Task assigned successfully to your child");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning task for parent {ParentId}", parentId);
                return _response.ServerError<AssignTaskResponse>("An error occurred while assigning the task");
            }
        }
        public async Task<Response<ReviewDecisionResponse>> ReviewChildTaskAsync(string parentId, ReviewTaskDecisionRequest request)
        {
            var task = await _context.ChildTasks
                .Include(t => t.Child)
                .Include(t => t.Template)
                .FirstOrDefaultAsync(t => t.Id == request.ChildTaskId
                                       && t.Child.ParentId == parentId
                                       && t.Status == TaskStatus.ReviewRequested
                                       && !t.IsDeleted);

            if (task == null)
                return _response.NotFound<ReviewDecisionResponse>("Task not found, already reviewed, or not assigned by you");

            if (request.IsApproved)
            {
                // Approve → Complete + نقاط
                task.Status = TaskStatus.Completed;
                task.CompletedAt = DateTime.UtcNow;
                task.ApprovedAt = DateTime.UtcNow;
                task.ParentAcceptanceMessage = request.AcceptanceMessage;


                // أضف النقاط للطفل
                task.Child.TotalPoints += task.Template.BasePoints;

                // سجل في PointsTransaction
                _context.PointsTransactions.Add(new PointsTransaction
                {
                    ChildId = task.ChildId,
                    Points = task.Template.BasePoints,
                    Reason = $"Parent approved: {task.Template.TitleEn} for child {task.ChildId} with points {task.Template.BasePoints}",
                    SourceType = PointsSourceType.ChildTask,
                    SourceEntityId = task.Id,
                    CreatedBy = parentId
                });

                var resp = new ReviewDecisionResponse
                {
                    ChildTaskId = task.Id,
                    Status = "Completed",
                    Message = "Task approved and points awarded",
                    AwardedPoints = task.Template.BasePoints
                };

                await _context.SaveChangesAsync();

                _ = _notificationService.SendAsync(
                    task.ChildId,
                    NotificationType.TaskApproved,
                    "Task Approved!",
                    $"Your parent approved \"{task.Template.TitleEn}\". You earned {task.Template.BasePoints} points!",
                    task.Id);

                return _response.Success(resp, "Task approved successfully");
            }
            else
            {
                // Reject
                if (string.IsNullOrWhiteSpace(request.RejectionReason))
                    return _response.BadRequest<ReviewDecisionResponse>("Rejection reason is required");

                task.Status = TaskStatus.Rejected;
                task.RejectionReason = request.RejectionReason;
                task.UpdatedAt = DateTime.UtcNow;
                task.UpdatedBy = parentId;

                await _context.SaveChangesAsync();

                var reason = request.RejectionReason.Length > 80
                    ? request.RejectionReason[..80] + "…"
                    : request.RejectionReason;

                _ = _notificationService.SendAsync(
                    task.ChildId,
                    NotificationType.TaskRejected,
                    "Task Needs Changes",
                    $"Your parent reviewed \"{task.Template.TitleEn}\": {reason}",
                    task.Id);

                var resp = new ReviewDecisionResponse
                {
                    ChildTaskId = task.Id,
                    Status = "Rejected",
                    Message = $"Task rejected: {request.RejectionReason}"
                };

                return _response.Success(resp, "Task rejected successfully");
            }
        }

        public async Task<Response<PaginatedList<ChildTaskListItemResponse>>> GetChildTasksAsync(
            string parentId,
            GetChildTasksFilters filters)
        {
            // 1. جيب الأطفال التابعين للـ Parent
            var childQuery = _context.Childrens
                .Where(c => c.ParentId == parentId && !c.IsDeleted);

            var childIds = await childQuery.Select(c => c.Id).ToListAsync();

            if (!childIds.Any())
                return _response.Success<PaginatedList<ChildTaskListItemResponse>>(
                    new PaginatedList<ChildTaskListItemResponse>(new(), filters.PageNumber, filters.PageSize, 0),
                    "No children linked to this parent");

            // 2. بناء الـ Query
            var query = _context.ChildTasks
                .Include(ct => ct.Template)
                .ThenInclude(t => t.SubCategory) // Include SubCategory for category names
                .ThenInclude(c=>c.Category)
                .Include(ct => ct.Child)
                .Where(ct => childIds.Contains(ct.ChildId)
                          //&& ct.Source == TaskSource.Parent  // بس اللي الـ Parent أداها
                          && !ct.IsDeleted);

            // فلترة بالـ Status لو موجود
            if (filters.Status.HasValue)
                query = query.Where(ct => ct.Status == filters.Status.Value);

            // Pagination
            var totalCount = await query.CountAsync();

            var tasks = await query
                .Skip((filters.PageNumber - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .Select(ct => new ChildTaskListItemResponse
                {
                    ChildTaskId = ct.Id,
                    ChildId = ct.ChildId,
                    ChildName = ct.Child.Name,
                    TaskTemplateId = ct.TaskTemplateId,
                    TitleAr = ct.Template.TitleAr,
                    TitleEn = ct.Template.TitleEn,
                    IconUrl = ct.Template.IconUrl,
                    Points = ct.Template.BasePoints,
                    Status = ct.Status,
                    AssignedAt = ct.AssignedAt,
                    DueDate = ct.DueDate,
                    CompletedAt = ct.CompletedAt,
                    ReviewRequestedAt = ct.ReviewRequestedAt,
                    RejectionReason = ct.RejectionReason,
                    EvidenceUrl = ct.AnswerMediaUrl, // افترض إنك أضفت الحقل ده
                    CategoryNameAr = ct.Template.SubCategory != null ? ct.Template.SubCategory.Category.NameAr : null,
                    CategoryNameEn = ct.Template.SubCategory != null ? ct.Template.SubCategory.Category.NameEn : null,
                    SubCategoryNameAr = ct.Template.SubCategory != null ? ct.Template.SubCategory.NameAr : null,
                    SubCategoryNameEn = ct.Template.SubCategory != null ? ct.Template.SubCategory.NameEn : null,
                        Difficulty = ct.Template.Difficulty.ToString(),

                })
                .ToListAsync();

            var paginated = new PaginatedList<ChildTaskListItemResponse>(
                tasks,
                filters.PageNumber,
                filters.PageSize,
                totalCount);

            return _response.Success(paginated, tasks.Any() ? "Child tasks retrieved successfully" : "No tasks found");
        }

        // This is for admin 
        public async Task<Response<ChildTaskDetailsResponse>> GetChildTaskDetailsAsync(string parentId, string childTaskId)
        {
            var task = await _context.ChildTasks
                .Include(ct => ct.Template)
                .Include(ct => ct.Child)
                .FirstOrDefaultAsync(ct => ct.Id == childTaskId
                                        && ct.Child.ParentId == parentId
                                        && !ct.IsDeleted);

            if (task == null)
                return _response.NotFound<ChildTaskDetailsResponse>("Task not found or does not belong to your child");

            var response = new ChildTaskDetailsResponse
            {
                Id = task.Id,
                ChildId = task.ChildId,
                ChildName = task.Child.Name,
                ChildNickName = task.Child.NickName,
                TaskTemplateId = task.TaskTemplateId,
                TitleAr = task.Template.TitleAr,
                TitleEn = task.Template.TitleEn,
                DescriptionAr = task.Template.DescriptionAr,
                DescriptionEn = task.Template.DescriptionEn,
                IconUrl = task.Template.IconUrl,
                Points = task.Template.BasePoints,
                TemplateType = task.Template.TemplateType,
                Source = task.Source,
                AssignedByParentId = task.AssignedByParentId,
                Status = task.Status,
                AssignedAt = task.AssignedAt,
                DueDate = task.DueDate,
                StartedAt = task.StartedAt,
                ReviewRequestedAt = task.ReviewRequestedAt,
                CompletedAt = task.CompletedAt,
                ApprovedAt = task.ApprovedAt,
                RejectionReason = task.RejectionReason,
                ParentAcceptanceMessage = task.ParentAcceptanceMessage,
                ChildNote = task.ChildNote,
                AttemptCount = task.AttemptCount,
                AnswerText = task.AnswerText,
                AnswerMediaUrl = task.AnswerMediaUrl
            };

            return _response.Success(response, "Child task details retrieved successfully");
        }

        // For Parent Portal
        public async Task<Response<ParentChildTaskDetailsResponse>> GetParentChildTaskDetailsAsync(string parentId, string childTaskId)
        {
            var task = await _context.ChildTasks
                .Include(ct => ct.Template)
                .Include(ct => ct.Child)
                .FirstOrDefaultAsync(ct => ct.Id == childTaskId
                                        && ct.Child.ParentId == parentId
                                        && !ct.IsDeleted);

            if (task == null)
                return _response.NotFound<ParentChildTaskDetailsResponse>("Task not found or does not belong to your child");

            var response = new ParentChildTaskDetailsResponse
            {
                ChildTaskId = task.Id,
                ChildId = task.ChildId,
                ChildName = task.Child.Name,
                ChildNickName = task.Child.NickName,
                TaskTemplateId = task.TaskTemplateId,
                TitleAr = task.Template.TitleAr,
                TitleEn = task.Template.TitleEn,
                DescriptionAr = task.Template.DescriptionAr,
                DescriptionEn = task.Template.DescriptionEn,
                IconUrl = task.Template.IconUrl,
                Points = task.Template.BasePoints,
                TemplateType = task.Template.TemplateType,
                Status = task.Status,
                AssignedAt = task.AssignedAt,
                DueDate = task.DueDate,
                ReviewRequestedAt = task.ReviewRequestedAt,
                CompletedAt = task.CompletedAt,
                ChildNote = task.ChildNote,
                AnswerMediaUrl = task.AnswerMediaUrl,          // الصورة أو الدليل
                RejectionReason = task.RejectionReason,
                ParentAcceptanceMessage = task.ParentAcceptanceMessage
            };

            return _response.Success(response, "Child task details retrieved successfully");
        }
    }
}
