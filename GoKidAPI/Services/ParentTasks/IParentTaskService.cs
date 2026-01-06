using GoKidAPI.DTO.Tasks.Requests;
using GoKidAPI.Shared;

namespace GoKidAPI.Services.ParentTasks
{
    public interface IParentTaskService
    {
        Task<Response<AssignTaskResponse>> AssignTaskToChildAsync(string parentId, AssignTaskRequest request);
    }
}
