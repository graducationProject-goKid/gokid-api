using GoKidAPI.DTO.Tasks.Requests;
using GoKidAPI.DTO.Tasks.Responses;
using GoKidAPI.Enums.Tasks;
using GoKidAPI.Shared;

namespace GoKidAPI.Services.ParentTasks
{
    public interface IParentTaskService
    {
        Task<Response<AssignTaskResponse>> AssignTaskToChildAsync(string parentId, AssignTaskRequest request);
        
        /// <summary>
        /// Parent approves or rejects a submitted task
        /// </summary>
        Task<Response<ReviewDecisionResponse>> ReviewChildTaskAsync(string parentId, ReviewTaskDecisionRequest request);

        Task<Response<PaginatedList<ChildTaskListItemResponse>>> GetChildTasksAsync(
            string parentId,
            GetChildTasksFilters filters);
        // For Admin view task
        Task<Response<ChildTaskDetailsResponse>> GetChildTaskDetailsAsync(string parentId, string childTaskId);
        // For Parent view
        Task<Response<ParentChildTaskDetailsResponse>> GetParentChildTaskDetailsAsync(string parentId, string childTaskId);
    }
}
