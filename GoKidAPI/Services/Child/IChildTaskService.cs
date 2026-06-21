using GoKidAPI.DTO.Adventures.Requests;
using GoKidAPI.DTO.Adventures.Responses;
using GoKidAPI.DTO.ChidAdventure.Responses;
using GoKidAPI.DTO.Childs.Requests;
using GoKidAPI.DTO.Childs.Responses;
using GoKidAPI.DTO.Tasks.Responses;
using GoKidAPI.Shared;

namespace GoKidAPI.Services.Child
{
    public interface IChildTaskService
    {
        Task<Response<List<TaskTemplateListItemResponse>>> GetDailyGeneralTasksAsync(string childId, DateTime date);
        Task<Response<SubmitTaskResponse>> SubmitTaskAsync(string childId, SubmitTaskRequest request);
       
        
        #region Child Adventure Flow
        Task<Response<List<ChildAdventureListItemResponse>>> GetMyAdventuresAsync(string childId);
        Task<Response<ChildAdventureDetailsResponse>> GetAdventureDetailsAsync(
    string childId,
    string weeklyAdventureId);

        Task<Response<SubmitTaskResponse>> SubmitAdventureTaskAsync(
    string childId,
    SubmitAdventureTaskRequest request);

        Task<Response<ChildWeeklyAdventureResponse>> GetWeeklyAdventureTasksAsync(
           string childId,
           string weeklyAdventureId);

        #endregion


    }
}
