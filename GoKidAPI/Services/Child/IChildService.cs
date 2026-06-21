using GoKidAPI.DTO.Childs.Responses;
using GoKidAPI.DTO.Tasks.Responses;
using GoKidAPI.Shared;

namespace GoKidAPI.Services.Child
{
    public interface IChildService
    {
        Task<Response<ChildPointsResponse>> GetPointsAsync(string childId);
        Task<Response<List<ParentAssignedTaskResponse>>> GetParentAssignedTasksAsync(string childId);

    }
}
