using GoKidAPI.Data;
using GoKidAPI.DTO.Childs.Responses;
using GoKidAPI.DTO.Tasks.Responses;
using GoKidAPI.Enums.Tasks;
using GoKidAPI.Shared;

using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Services.Child
{
    public class ChildService : IChildService
    {
        private readonly AppDbContext _context;
        private readonly ResponseHandler _responseHandler;

        public ChildService(AppDbContext context, ResponseHandler responseHandler)
        {
            _context = context;
            _responseHandler = responseHandler;
        }

        public async Task<Response<ChildPointsResponse>> GetPointsAsync(string childId)
        {
            var child = await _context.Childrens.FirstOrDefaultAsync(c => c.Id == childId && !c.IsDeleted);
            if (child == null)
                return _responseHandler.NotFound<ChildPointsResponse>("Child not found");

            return _responseHandler.Success(new ChildPointsResponse { TotalPoints = child.TotalPoints }, "Points retrieved successfully");
        }

        public async Task<Response<List<ParentAssignedTaskResponse>>> GetParentAssignedTasksAsync(string childId)
        {
            var child = await _context.Childrens
                .FirstOrDefaultAsync(c => c.Id == childId && !c.IsDeleted);

            if (child == null)
                return _responseHandler.NotFound<List<ParentAssignedTaskResponse>>("Child not found");

            // جيب التاسكات اللي مصدرها Parent ومش deleted
            var tasks = await _context.ChildTasks
                .Include(ct => ct.Template).ThenInclude(ct=>ct.SubCategory)  // Include SubCategory to get its name
                .Where(ct => ct.ChildId == childId
                          && ct.Source == TaskSource.Parent && ct.Status != Enums.Tasks.TaskStatus.Completed
                          && !ct.IsDeleted
                          && (ct.DueDate == null || ct.DueDate >= DateTime.UtcNow))  // مش expired
                .OrderByDescending(ct => ct.AssignedAt)  // الأحدث أولاً
                .Select(ct => new ParentAssignedTaskResponse
                {
                    ChildTaskId = ct.Id,
                    TaskTemplateId = ct.TaskTemplateId,
                    TitleAr = ct.Template.TitleAr,
                    TitleEn = ct.Template.TitleEn,
                    DescriptionAr = ct.Template.DescriptionAr,
                    DescriptionEn = ct.Template.DescriptionEn,
                    IconUrl = ct.Template.TaskImageUrl,
                    Points = ct.Template.BasePoints,
                    //VerificationType = ct.Template.TemplateType switch
                    //{
                    //    TaskTemplateType.InstantReward => TaskVerificationType.SystemAuto,
                    //    TaskTemplateType.VoiceQuestion => TaskVerificationType.AIVoice,
                    //    TaskTemplateType.EvidenceSubmission => TaskVerificationType.ParentReview,
                    //    _ => TaskVerificationType.None
                    //},
                    AssignedAt = ct.AssignedAt,
                    DueDate = ct.DueDate,
                    Status = ct.Status,
                    CompletedAt = ct.CompletedAt,
                    RejectionReason = ct.RejectionReason,
                    subCategoryNameEn = ct.Template.SubCategory != null ? ct.Template.SubCategory.NameEn : null,
                    TemplateType = ct.Template.TemplateType.ToString()
                })
                .ToListAsync();

            if (!tasks.Any())
                return _responseHandler.Success<List<ParentAssignedTaskResponse>>(new List<ParentAssignedTaskResponse>(), "No tasks assigned by parent yet");

            return _responseHandler.Success(tasks, "Parent assigned tasks retrieved successfully");
        }

    }
}
