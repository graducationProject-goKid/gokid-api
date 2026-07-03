using GoKidAPI.DTO.Levels.Requests;
using GoKidAPI.DTO.Levels.Responses;
using GoKidAPI.Shared;

namespace GoKidAPI.Services.Levels
{
    public interface ILevelService
    {
        Task<Response<List<LevelResponse>>> GetAllAsync();
        Task<Response<LevelResponse>> GetByIdAsync(string levelId);
        Task<Response<LevelResponse>> CreateAsync(CreateLevelRequest request, string adminId);
        Task<Response<LevelResponse>> UpdateAsync(string levelId, UpdateLevelRequest request, string adminId);
        Task<Response<object>> DeleteAsync(string levelId, string adminId);
    }
}
