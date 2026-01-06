using GoKidAPI.Data;
using GoKidAPI.DTO.Tasks.Responses;
using GoKidAPI.Entity.Tasks;
using GoKidAPI.Enums.Shared;
using GoKidAPI.Enums.Tasks;
using GoKidAPI.Services.TaskTemplate.Interfaces;
using GoKidAPI.Shared;

using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Services.TaskTemplate
{
    public class TaskTemplateQueryService : ITaskTemplateQueryService
    {
        private readonly AppDbContext _context;
        private readonly ResponseHandler _response;
        private readonly ILogger<TaskTemplateQueryService> _logger;

        public TaskTemplateQueryService(AppDbContext context, ResponseHandler response, ILogger<TaskTemplateQueryService> logger)
        {
            _context = context;
            _response = response;
            _logger = logger;
        }

        public async Task<Response<PaginatedList<TaskTemplateListItemResponse>>> GetAllAsync(RequestFilters<TaskSortingColumn> filters)
        {
            try
            {
                // Default sorting
                var sortColumn = filters.SortColumn ?? TaskSortingColumn.CreatedAt;
                var sortDirection = filters.SortDirection ?? SortDirection.DESC;

                _logger.LogInformation("Fetching task templates - Page: {PageNumber}, Size: {PageSize}, Sort: {SortColumn} {SortDirection}",
                    filters.PageNumber, filters.PageSize, sortColumn, sortDirection);

                // Build Query
                var query = _context.TaskTemplates
                    .Include(t => t.SubCategory)
                    .AsQueryable().AsNoTracking();

                //// 3. Apply Sorting
                //query = sortColumn switch
                //{
                //    TaskSortingColumn.Title => sortDirection == SortDirection.DESC
                //        ? query.OrderByDescending(t => t.TitleEn)
                //        : query.OrderBy(t => t.TitleEn),

                //    TaskSortingColumn.Difficulty => sortDirection == SortDirection.DESC
                //        ? query.OrderByDescending(t => t.Difficulty)
                //        : query.OrderBy(t => t.Difficulty),

                //    TaskSortingColumn.BasePoints => sortDirection == SortDirection.DESC
                //        ? query.OrderByDescending(t => t.BasePoints)
                //        : query.OrderBy(t => t.BasePoints),

                //    TaskSortingColumn.TemplateType => sortDirection == SortDirection.DESC
                //        ? query.OrderByDescending(t => t.TemplateType)
                //        : query.OrderBy(t => t.TemplateType),

                //    TaskSortingColumn.CreatedAt => sortDirection == SortDirection.DESC
                //        ? query.OrderByDescending(t => t.CreatedAt)
                //        : query.OrderBy(t => t.CreatedAt),

                //    TaskSortingColumn.SubCategory => sortDirection == SortDirection.DESC
                //        ? query.OrderByDescending(t => t.Category!.NameEn)
                //        : query.OrderBy(t => t.Category!.NameEn),

                //    _ => sortDirection == SortDirection.DESC
                //        ? query.OrderByDescending(t => t.CreatedAt)
                //        : query.OrderBy(t => t.CreatedAt)
                //};

                // 4. Get Total Count
                var total = await query.CountAsync();

                // 5. Pagination + Projection
                var items = await query
                    .Skip((filters.PageNumber - 1) * filters.PageSize)
                    .Take(filters.PageSize)
                    .Select(t => new TaskTemplateListItemResponse
                    {
                        Id = t.Id,
                        TitleAr = t.TitleAr,
                        TitleEn = t.TitleEn,
                        DescriptionAr = t.DescriptionAr,
                        DescriptionEn = t.DescriptionEn,
                        IconUrl = t.IconUrl,
                        SubCategoryId = t.SubCategoryId.ToString(),
                        SubCategoryNameEn = t.SubCategory!.NameEn,
                        Difficulty = t.Difficulty,
                        BasePoints = t.BasePoints,
                        TemplateType = t.TemplateType,
                        CreatedAt = t.CreatedAt
                    })
                    .ToListAsync();

                // 6. Build Paginated Result
                var paginated = new PaginatedList<TaskTemplateListItemResponse>(items, filters.PageNumber, filters.PageSize, total);

                _logger.LogInformation("Successfully retrieved {Count} task templates (Total: {Total})", items.Count, total);

                return _response.Success(paginated, "Task templates retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving task templates with filters: Page {Page}, Size {Size}", filters.PageNumber, filters.PageSize);
                return _response.ServerError<PaginatedList<TaskTemplateListItemResponse>>("An error occurred while retrieving task templates");
            }
        }

        public async Task<Response<TaskTemplateDetailsResponse>> GetByIdAsync(string id)
        {
            // Id is the tempateId
            var baseTemplate = await _context.TaskTemplates
                .AsNoTracking()
                .Include(t => t.SubCategory)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (baseTemplate == null)
                return _response.NotFound<TaskTemplateDetailsResponse>("Task template not found");

            TaskTemplateDetailsResponse response = new()
            {
                Id = baseTemplate.Id,
                TitleAr = baseTemplate.TitleAr,
                TitleEn = baseTemplate.TitleEn,
                DescriptionAr = baseTemplate.DescriptionAr,
                DescriptionEn = baseTemplate.DescriptionEn,
                IconUrl = baseTemplate.IconUrl,
                SubCategoryId = baseTemplate.SubCategoryId.ToString(),
                SubCategoryNameEn = baseTemplate.SubCategory.NameEn,
                Difficulty = baseTemplate.Difficulty,
                BasePoints = baseTemplate.BasePoints,
                TemplateType = baseTemplate.TemplateType,
                CreatedAt = baseTemplate.CreatedAt
            };

            switch (baseTemplate.TemplateType)
            {
                case TaskTemplateType.TextQuestion:
                        response.QuestionText = baseTemplate.QuestionText;
                        response.TaskImageUrl = baseTemplate.TaskImageUrl;
                        response.ExpectedCorrectAnswer = baseTemplate.ExpectedCorrectAnswer;
                        response.CaseSensitive = baseTemplate.CaseSensitive;
                    break;

                case TaskTemplateType.VoiceQuestion:
                        response.QuestionText = baseTemplate.QuestionText;
                        response.TaskImageUrl = baseTemplate.TaskImageUrl;
                        response.ExpectedCorrectAnswer = baseTemplate.ExpectedCorrectAnswer;
                        response.VoicePrompt = baseTemplate.VoicePrompt;
                        response.MaxVoiceAttempts = baseTemplate.MaxVoiceAttempts;
                        response.MaxVoiceDurationSeconds = baseTemplate.MaxVoiceDurationSeconds;
                    break;

                case TaskTemplateType.EvidenceSubmission:
                        response.InstructionsText = baseTemplate.InstructionsText;
                        response.TaskImageUrl = baseTemplate.TaskImageUrl;
                        response.EvidenceType = baseTemplate.EvidenceType;
                        response.ReviewBy = baseTemplate.ReviewBy;
                    break;

                    // InstantReward مفيش حقول إضافية
            }

            return _response.Success(response,"Task details retrived succsessfully");
        }
        public async Task<Response<PaginatedList<TaskTemplateListItemResponse>>> GetBySubCategoryAsync(
            string subCategoryId, DifficultyLevel? difficulty, RequestFilters<TaskSortingColumn> filters)
        {
            try
            {
                // Validation + Defaults
                if (filters.PageNumber < 1)
                {
                    _logger.LogWarning("Invalid PageNumber {PageNumber}, defaulting to 1", filters.PageNumber);
                    filters.PageNumber = 1;
                }

                if (filters.PageSize < 1 || filters.PageSize > 100)
                {
                    _logger.LogWarning("Invalid PageSize {PageSize}, defaulting to 20", filters.PageSize);
                    filters.PageSize = 20;
                }

                var sortColumn = filters.SortColumn ?? TaskSortingColumn.Title;
                var sortDirection = filters.SortDirection ?? SortDirection.ASC;

                _logger.LogInformation("Fetching tasks for SubCategory {SubCategoryId} - Difficulty: {Difficulty}, Page: {Page}, Size: {Size}",
                    subCategoryId, difficulty, filters.PageNumber, filters.PageSize);

                // Check if subcategory exists
                var subCategoryExists = await _context.SubCategories.AnyAsync(s => s.Id == subCategoryId);
                if (!subCategoryExists)
                    return _response.NotFound<PaginatedList<TaskTemplateListItemResponse>>("SubCategory not found");

                var query = _context.TaskTemplates
                    .AsNoTracking()
                    .Include(t => t.SubCategory)
                    .Where(t => t.SubCategoryId.ToString() == subCategoryId);

                if (difficulty.HasValue)
                    query = query.Where(t => t.Difficulty == difficulty.Value);

                // Apply Sorting
                query = sortColumn switch
                {
                    TaskSortingColumn.Title => sortDirection == SortDirection.DESC
                        ? query.OrderByDescending(t => t.TitleEn)
                        : query.OrderBy(t => t.TitleEn),

                    TaskSortingColumn.Difficulty => sortDirection == SortDirection.DESC
                        ? query.OrderByDescending(t => t.Difficulty)
                        : query.OrderBy(t => t.Difficulty),

                    TaskSortingColumn.BasePoints => sortDirection == SortDirection.DESC
                        ? query.OrderByDescending(t => t.BasePoints)
                        : query.OrderBy(t => t.BasePoints),

                    TaskSortingColumn.TemplateType => sortDirection == SortDirection.DESC
                        ? query.OrderByDescending(t => t.TemplateType)
                        : query.OrderBy(t => t.TemplateType),

                    TaskSortingColumn.CreatedAt => sortDirection == SortDirection.DESC
                        ? query.OrderByDescending(t => t.CreatedAt)
                        : query.OrderBy(t => t.CreatedAt),

                    _ => sortDirection == SortDirection.DESC
                        ? query.OrderByDescending(t => t.TitleEn)
                        : query.OrderBy(t => t.TitleEn)
                };

                var total = await query.CountAsync();

                var items = await query
                    .Skip((filters.PageNumber - 1) * filters.PageSize)
                    .Take(filters.PageSize)
                    .Select(t => new TaskTemplateListItemResponse
                    {
                        Id = t.Id,
                        TitleAr = t.TitleAr,
                        TitleEn = t.TitleEn,
                        DescriptionAr = t.DescriptionAr,
                        DescriptionEn = t.DescriptionEn,
                        IconUrl = t.IconUrl,
                        SubCategoryId = t.SubCategoryId.ToString(),
                        SubCategoryNameEn = t.SubCategory!.NameEn,
                        Difficulty = t.Difficulty,
                        BasePoints = t.BasePoints,
                        TemplateType = t.TemplateType,
                        CreatedAt = t.CreatedAt
                    })
                    .ToListAsync();

                var paginated = new PaginatedList<TaskTemplateListItemResponse>(items, filters.PageNumber, filters.PageSize, total);

                _logger.LogInformation("Retrieved {Count} tasks for SubCategory {SubCategoryId} (Total: {Total})", items.Count, subCategoryId, total);

                return _response.Success(paginated, "Task templates retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tasks for SubCategory {SubCategoryId}", subCategoryId);
                return _response.ServerError<PaginatedList<TaskTemplateListItemResponse>>("An error occurred");
            }
        }
        
        public async Task<Response<PaginatedList<TaskTemplateListItemResponse>>> GetByCategoryAsync(
            string categoryId, RequestFilters<TaskSortingColumn> filters)
        {
            try
            {
                // Validation + Defaults
                if (filters.PageNumber < 1)
                {
                    _logger.LogWarning("Invalid PageNumber {PageNumber}, defaulting to 1", filters.PageNumber);
                    filters.PageNumber = 1;
                }

                if (filters.PageSize < 1 || filters.PageSize > 100)
                {
                    _logger.LogWarning("Invalid PageSize {PageSize}, defaulting to 20", filters.PageSize);
                    filters.PageSize = 20;
                }

                var sortColumn = filters.SortColumn ?? TaskSortingColumn.Title;
                var sortDirection = filters.SortDirection ?? SortDirection.ASC;

                _logger.LogInformation("Fetching tasks for Category {CategoryId} - Page: {Page}, Size: {Size}, Sort: {SortColumn} {SortDirection}",
                    categoryId, filters.PageNumber, filters.PageSize, sortColumn, sortDirection);

                // Check if category exists
                var categoryExists = await _context.TaskCategories.AnyAsync(c => c.Id == categoryId);
                if (!categoryExists)
                    return _response.NotFound<PaginatedList<TaskTemplateListItemResponse>>("Category not found");

                // Get all subcategories under this category
                var subCategoryIds = await _context.SubCategories
                    .Where(s => s.CategoryId == categoryId)
                    .Select(s => s.Id)
                    .ToListAsync();

                if (!subCategoryIds.Any())
                {
                    var empty = new PaginatedList<TaskTemplateListItemResponse>(new List<TaskTemplateListItemResponse>(), 1, filters.PageSize, 0);
                    return _response.Success(empty, "No subcategories or tasks found in this category");
                }

                // Build query for tasks in these subcategories
                var query = _context.TaskTemplates
                    .AsNoTracking()
                    .Include(t => t.SubCategory)
                    .Where(t => subCategoryIds.Contains(t.SubCategoryId.ToString()));

                // Apply Sorting
                query = sortColumn switch
                {
                    TaskSortingColumn.Title => sortDirection == SortDirection.DESC
                        ? query.OrderByDescending(t => t.TitleEn)
                        : query.OrderBy(t => t.TitleEn),

                    TaskSortingColumn.Difficulty => sortDirection == SortDirection.DESC
                        ? query.OrderByDescending(t => t.Difficulty)
                        : query.OrderBy(t => t.Difficulty),

                    TaskSortingColumn.BasePoints => sortDirection == SortDirection.DESC
                        ? query.OrderByDescending(t => t.BasePoints)
                        : query.OrderBy(t => t.BasePoints),

                    TaskSortingColumn.TemplateType => sortDirection == SortDirection.DESC
                        ? query.OrderByDescending(t => t.TemplateType)
                        : query.OrderBy(t => t.TemplateType),

                    TaskSortingColumn.CreatedAt => sortDirection == SortDirection.DESC
                        ? query.OrderByDescending(t => t.CreatedAt)
                        : query.OrderBy(t => t.CreatedAt),

                    TaskSortingColumn.SubCategory => sortDirection == SortDirection.DESC
                        ? query.OrderByDescending(t => t.SubCategory!.NameEn)
                        : query.OrderBy(t => t.SubCategory!.NameEn),

                    _ => sortDirection == SortDirection.DESC
                        ? query.OrderByDescending(t => t.TitleEn)
                        : query.OrderBy(t => t.TitleEn)
                };

                var total = await query.CountAsync();

                var items = await query
                    .Skip((filters.PageNumber - 1) * filters.PageSize)
                    .Take(filters.PageSize)
                    .Select(t => new TaskTemplateListItemResponse
                    {
                        Id = t.Id,
                        TitleAr = t.TitleAr,
                        TitleEn = t.TitleEn,
                        DescriptionAr = t.DescriptionAr,
                        DescriptionEn = t.DescriptionEn,
                        IconUrl = t.IconUrl,
                        SubCategoryId = t.SubCategoryId.ToString(),
                        SubCategoryNameEn = t.SubCategory!.NameEn,
                        Difficulty = t.Difficulty,
                        BasePoints = t.BasePoints,
                        TemplateType = t.TemplateType,
                        CreatedAt = t.CreatedAt
                    })
                    .ToListAsync();

                var paginated = new PaginatedList<TaskTemplateListItemResponse>(items, filters.PageNumber, filters.PageSize, total);

                _logger.LogInformation("Retrieved {Count} tasks for Category {CategoryId} (Total: {Total})", items.Count, categoryId, total);

                return _response.Success(paginated, "Task templates retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tasks for Category {CategoryId}", categoryId);
                return _response.ServerError<PaginatedList<TaskTemplateListItemResponse>>("An error occurred while retrieving task templates");
            }
        }
    }
}

