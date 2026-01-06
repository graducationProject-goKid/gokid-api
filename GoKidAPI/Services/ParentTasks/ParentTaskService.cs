using GoKidAPI.Data;
using GoKidAPI.DTO.Tasks.Requests;
using GoKidAPI.Entity.Tasks;
using GoKidAPI.Enums.Tasks;
using GoKidAPI.Shared;

using Microsoft.EntityFrameworkCore;

using TaskStatus = GoKidAPI.Enums.Tasks.TaskStatus;

namespace GoKidAPI.Services.ParentTasks
{
    public class ParentTaskService
    {
        private readonly AppDbContext _context;
        private readonly ResponseHandler _response;
        private readonly ILogger<ParentTaskService> _logger;

        public ParentTaskService(AppDbContext context, ResponseHandler response, ILogger<ParentTaskService> logger)
        {
            _context = context;
            _response = response;
            _logger = logger;
        }

        public async Task<Response<AssignTaskResponse>> AssignTaskToChildAsync(string parentId, AssignTaskRequest request)
        {
            try
            {
                // جيب الأب وتأكد إن عنده طفل نشط (MVP: واحد بس)
                var parent = await _context.Parents
                    .Include(p => p.ActiveChild)
                    .FirstOrDefaultAsync(p => p.AppUserId == parentId);

                if (parent == null || parent.ActiveChild!=null)
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

                // هنا ممكن نبعت Notification للطفل (Push أو InApp)

                return _response.Created(resp, "Task assigned successfully to your child");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning task for parent {ParentId}", parentId);
                return _response.ServerError<AssignTaskResponse>("An error occurred while assigning the task");
            }
        }
    }
}
