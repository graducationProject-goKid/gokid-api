using GoKidAPI.DTO.Category.Requests;
using GoKidAPI.DTO.Category.Responses;
using GoKidAPI.Shared;

namespace GoKidAPI.Services.SubCategory
{
    public interface ITaskSubCategoryService
    {
        Task<Response<List<SubCategoryResponse>>> GetAllAsync();
        Task<Response<List<SubCategoryResponse>>> GetByCategoryIdAsync(string categoryId);
        Task<Response<SubCategoryResponse>> GetByIdAsync(string id);
        Task<Response<SubCategoryResponse>> CreateAsync(CreateSubCategoryRequest request);
        Task<Response<SubCategoryResponse>> UpdateAsync(string id, UpdateSubCategoryRequest request);
        Task<Response<string>> DeleteAsync(string id);
    }
}
