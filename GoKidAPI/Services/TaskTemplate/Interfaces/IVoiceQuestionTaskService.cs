using GoKidAPI.DTO.Tasks.Requests;
using GoKidAPI.DTO.Tasks.Responses;
using GoKidAPI.Shared;

namespace GoKidAPI.Services.TaskTemplate.Interfaces
{
    public interface IVoiceQuestionTaskService
    {
        Task<Response<VoiceQuestionTaskResponse>> CreateAsync(CreateVoiceQuestionRequest request);
    }
}
