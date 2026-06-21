using DocumentFormat.OpenXml.Spreadsheet;

using GoKidAPI.DTO.Tasks.Responses;
using GoKidAPI.Enums.Tasks;
using GoKidAPI.Shared;

namespace GoKidAPI.Services.TaskTemplate.Interfaces
{
    public interface ITaskTemplateQueryService
    {
        Task<Response<PaginatedList<TaskTemplateListItemResponse>>> GetAllAsync(TaskRequestFilters filters);
        Task<Response<object>> GetByIdAsync(string id, TaskTemplateType type);
        Task<Response<PaginatedList<TaskTemplateListItemResponse>>> GetBySubCategoryAsync(
            string subCategoryId, DifficultyLevel? difficulty, RequestFilters<TaskSortingColumn> filters);
        Task<Response<PaginatedList<TaskTemplateListItemResponse>>> GetByCategoryAsync(
            string categoryId, RequestFilters<TaskSortingColumn> filters);
    }
}
