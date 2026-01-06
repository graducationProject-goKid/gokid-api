using GoKidAPI.DTO.Category.Requests;
using GoKidAPI.DTO.Category.Responses;
using GoKidAPI.Shared;

namespace GoKidAPI.Services.Category
{
    public interface ITaskCategoryService
    {
        Task<Response<List<CategoryResponse>>> GetAllAsync();
        Task<Response<CategoryResponse>> GetByIdAsync(string id);
        Task<Response<CategoryResponse>> CreateAsync(CreateCategoryRequest request);
        Task<Response<CategoryResponse>> UpdateAsync(string id, UpdateCategoryRequest request);
        Task<Response<string>> DeleteAsync(string id);
    }
}
