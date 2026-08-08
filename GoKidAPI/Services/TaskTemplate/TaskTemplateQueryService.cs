using GoKidAPI.Data;
using GoKidAPI.DTO.Tasks.Requests;
using GoKidAPI.DTO.Tasks.Responses;
using GoKidAPI.Entity.Tasks;
using GoKidAPI.Enums;
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

        public async Task<Response<PaginatedList<TaskTemplateListItemResponse>>> GetAllAsync(
            TaskRequestFilters filters,
            string? role,
            string? requesterId = null)
        {
            try
            {
                var sortColumn = filters.SortColumn ?? TaskSortingColumn.CreatedAt;
                var sortDirection = filters.SortDirection ?? SortDirection.DESC;

                var query = _context.TaskTemplates
                    .Include(t => t.SubCategory)
                    .ThenInclude(sc => sc.Category)
                    .AsNoTracking()
                    .AsQueryable();

                if (role == UserType.Parent.ToString())
                {
                    query = query.Where(t => t.TemplateType != TaskTemplateType.InstantReward);
                }

                // ✅ Task-specific filters
                if (filters.TemplateType.HasValue)
                {
                    query = query.Where(t => t.TemplateType == filters.TemplateType.Value);
                }

                // ✅ Difficulty filter
                if (filters.Difficulty.HasValue)
                {
                    query = query.Where(t => t.Difficulty == filters.Difficulty.Value);
                }

                // ✅ SubCategory filter
                if (!string.IsNullOrEmpty(filters.SubCategoryId.ToString()))
                {
                    query = query.Where(t => t.SubCategoryId == filters.SubCategoryId.ToString());
                }

                // ✅ Age-appropriate filtering: for a Parent, automatically restrict to tasks
                // suitable for their active child's age. Otherwise, the caller (e.g. an
                // Institution Admin building an Adventure) may manually pass RecommendedAge.
                int? age = null;
                if (role == UserType.Parent.ToString() && !string.IsNullOrEmpty(requesterId))
                {
                    age = await GetParentActiveChildAgeAsync(requesterId);
                }
                else if (filters.RecommendedAge.HasValue)
                {
                    age = filters.RecommendedAge.Value;
                }

                query = ApplyAgeFilter(query, age);

                // (اختياري) Sorting
                query = sortColumn switch
                {
                    TaskSortingColumn.Title =>
                        sortDirection == SortDirection.DESC
                            ? query.OrderByDescending(t => t.TitleEn)
                            : query.OrderBy(t => t.TitleEn),

                    TaskSortingColumn.CreatedAt =>
                        sortDirection == SortDirection.DESC
                            ? query.OrderByDescending(t => t.CreatedAt)
                            : query.OrderBy(t => t.CreatedAt),

                    _ => query.OrderByDescending(t => t.CreatedAt)
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
                        SubCategoryNameEn = t.SubCategory!.NameEn,
                        TemplateType = t.TemplateType,
                        CreatedAt = t.CreatedAt,
                        DescriptionAr = t.DescriptionAr,
                        DescriptionEn = t.DescriptionEn,
                        IconUrl = t.IconUrl,
                        SubCategoryId = t.SubCategoryId.ToString(),
                        Difficulty = t.Difficulty,
                        BasePoints = t.BasePoints,
                        RecommendedAgeFrom = t.RecommendedAgeFrom,
                        RecommendedAgeTo = t.RecommendedAgeTo,
                        CategoryId = t.SubCategory.CategoryId.ToString(),
                        CategoryNameAr = t.SubCategory.Category.NameAr,
                        CategoryNameEn = t.SubCategory.Category.NameEn,
                        SubCategoryNameAr = t.SubCategory.NameAr
                    })
                    .ToListAsync();

                return _response.Success(
                    new PaginatedList<TaskTemplateListItemResponse>(
                        items, filters.PageNumber, filters.PageSize, total),
                    "Task templates retrieved successfully"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving task templates");
                return _response.ServerError<PaginatedList<TaskTemplateListItemResponse>>(
                    "An error occurred while retrieving task templates");
            }
        }

        public async Task<Response<object>> GetByIdAsync(string id, TaskTemplateType taskType)
        {
            try
            {
                // Id is the tempateId
                var baseTemplate = await _context.TaskTemplates
                        .Include(t => t.SubCategory)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Id == id);

                if (baseTemplate == null)
                    return _response.NotFound<object>("Task template not found");

                // Verify type matches
                if (baseTemplate.TemplateType != taskType)
                    return _response.BadRequest<object>(
                        $"Task type mismatch. Expected {taskType}, but found {baseTemplate.TemplateType}");

                object response = taskType switch
                {
                    TaskTemplateType.InstantReward => new InstantRewardTaskResponse
                    {
                        Id = baseTemplate.Id,
                        TitleAr = baseTemplate.TitleAr,
                        TitleEn = baseTemplate.TitleEn,
                        DescriptionAr = baseTemplate.DescriptionAr,
                        DescriptionEn = baseTemplate.DescriptionEn,
                        IconUrl = baseTemplate.IconUrl,
                        TaskImageUrl = baseTemplate.TaskImageUrl,
                        SubCategoryId = baseTemplate.SubCategoryId?.ToString() ?? "No Category",
                        SubCategoryNameEn = baseTemplate.SubCategory?.NameEn ?? "No Category",
                        Difficulty = baseTemplate.Difficulty,
                        BasePoints = baseTemplate.BasePoints,
                        RecommendedAgeFrom = baseTemplate.RecommendedAgeFrom,
                        RecommendedAgeTo = baseTemplate.RecommendedAgeTo,
                        CreatedAt = baseTemplate.CreatedAt
                    },

                    TaskTemplateType.TextQuestion => new TextQuestionTaskResponse
                    {
                        Id = baseTemplate.Id,
                        TitleAr = baseTemplate.TitleAr,
                        TitleEn = baseTemplate.TitleEn,
                        DescriptionAr = baseTemplate.DescriptionAr,
                        DescriptionEn = baseTemplate.DescriptionEn,
                        IconUrl = baseTemplate.IconUrl,
                        SubCategoryId = baseTemplate.SubCategoryId?.ToString() ?? "No Category",
                        SubCategoryNameEn = baseTemplate.SubCategory?.NameEn ?? "No Category",
                        Difficulty = baseTemplate.Difficulty,
                        BasePoints = baseTemplate.BasePoints,
                        RecommendedAgeFrom = baseTemplate.RecommendedAgeFrom,
                        RecommendedAgeTo = baseTemplate.RecommendedAgeTo,
                        CreatedAt = baseTemplate.CreatedAt,
                        QuestionText = baseTemplate.QuestionText!,
                        TaskImageUrl = baseTemplate.TaskImageUrl,
                        ExpectedCorrectAnswer = baseTemplate.ExpectedCorrectAnswer!,
                        CaseSensitive = baseTemplate.CaseSensitive
                    },

                    TaskTemplateType.VoiceQuestion => new VoiceQuestionTaskResponse
                    {
                        Id = baseTemplate.Id,
                        TitleAr = baseTemplate.TitleAr,
                        TitleEn = baseTemplate.TitleEn,
                        DescriptionAr = baseTemplate.DescriptionAr,
                        DescriptionEn = baseTemplate.DescriptionEn,
                        IconUrl = baseTemplate.IconUrl,
                        SubCategoryId = baseTemplate.SubCategoryId?.ToString() ?? "No Category",
                        SubCategoryNameEn = baseTemplate.SubCategory?.NameEn ?? "No Category",
                        Difficulty = baseTemplate.Difficulty,
                        BasePoints = baseTemplate.BasePoints,
                        RecommendedAgeFrom = baseTemplate.RecommendedAgeFrom,
                        RecommendedAgeTo = baseTemplate.RecommendedAgeTo,
                        CreatedAt = baseTemplate.CreatedAt,
                        QuestionText = baseTemplate.QuestionText!,
                        TaskImageUrl = baseTemplate.TaskImageUrl,
                        ExpectedCorrectAnswer = baseTemplate.ExpectedCorrectAnswer!,
                        VoicePrompt = baseTemplate.VoicePrompt,
                        MaxVoiceAttempts = baseTemplate.MaxVoiceAttempts,
                        MaxVoiceDurationSeconds = baseTemplate.MaxVoiceDurationSeconds,
                    },

                    TaskTemplateType.EvidenceSubmission => new EvidenceSubmissionTaskResponse
                    {
                        Id = baseTemplate.Id,
                        TitleAr = baseTemplate.TitleAr,
                        TitleEn = baseTemplate.TitleEn,
                        DescriptionAr = baseTemplate.DescriptionAr,
                        DescriptionEn = baseTemplate.DescriptionEn,
                        IconUrl = baseTemplate.IconUrl,
                        SubCategoryId = baseTemplate.SubCategoryId?.ToString() ?? "No Category",
                        SubCategoryNameEn = baseTemplate.SubCategory?.NameEn ?? "No Category",
                        Difficulty = baseTemplate.Difficulty,
                        BasePoints = baseTemplate.BasePoints,
                        RecommendedAgeFrom = baseTemplate.RecommendedAgeFrom,
                        RecommendedAgeTo = baseTemplate.RecommendedAgeTo,
                        CreatedAt = baseTemplate.CreatedAt,
                        InstructionsText = baseTemplate.InstructionsText!,
                        TaskImageUrl = baseTemplate.TaskImageUrl,
                        EvidenceType = baseTemplate.EvidenceType,
                        ReviewBy = baseTemplate.ReviewBy
                    },

                    _ => throw new ArgumentException($"Unsupported template type: {taskType}")
                };

                _logger.LogInformation("Successfully retrieved {Type} task template with ID {Id}", taskType, id);
                return _response.Success(response, "Task template retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving task template with ID {Id} and Type {Type}", id, taskType);
                return _response.ServerError<object>("An error occurred while retrieving task template");
            }
        }
    
        public async Task<Response<PaginatedList<TaskTemplateListItemResponse>>> GetBySubCategoryAsync(
            string subCategoryId, DifficultyLevel? difficulty, RequestFilters<TaskSortingColumn> filters, int? recommendedAge = null)
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

                query = ApplyAgeFilter(query, recommendedAge);

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
                        RecommendedAgeFrom = t.RecommendedAgeFrom,
                        RecommendedAgeTo = t.RecommendedAgeTo,
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
            string categoryId, RequestFilters<TaskSortingColumn> filters, int? recommendedAge = null)
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

                query = ApplyAgeFilter(query, recommendedAge);

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
                        RecommendedAgeFrom = t.RecommendedAgeFrom,
                        RecommendedAgeTo = t.RecommendedAgeTo,
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

        public async Task<Response<TaskTemplateListItemResponse>> UpdateRecommendedAgeAsync(
            string id, UpdateRecommendedAgeRequest request)
        {
            var template = await _context.TaskTemplates
                .Include(t => t.SubCategory)
                .ThenInclude(sc => sc.Category)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (template == null)
                return _response.NotFound<TaskTemplateListItemResponse>("Task template not found");

            template.RecommendedAgeFrom = request.RecommendedAgeFrom;
            template.RecommendedAgeTo = request.RecommendedAgeTo;

            await _context.SaveChangesAsync();

            var response = new TaskTemplateListItemResponse
            {
                Id = template.Id,
                TitleAr = template.TitleAr,
                TitleEn = template.TitleEn,
                DescriptionAr = template.DescriptionAr,
                DescriptionEn = template.DescriptionEn,
                IconUrl = template.IconUrl,
                SubCategoryId = template.SubCategoryId?.ToString() ?? "No Category",
                SubCategoryNameEn = template.SubCategory?.NameEn ?? "No Category",
                SubCategoryNameAr = template.SubCategory?.NameAr ?? "No Category",
                CategoryId = template.SubCategory?.CategoryId ?? "No Category",
                CategoryNameAr = template.SubCategory?.Category?.NameAr ?? "No Category",
                CategoryNameEn = template.SubCategory?.Category?.NameEn ?? "No Category",
                Difficulty = template.Difficulty,
                BasePoints = template.BasePoints,
                RecommendedAgeFrom = template.RecommendedAgeFrom,
                RecommendedAgeTo = template.RecommendedAgeTo,
                TemplateType = template.TemplateType,
                CreatedAt = template.CreatedAt
            };

            return _response.Success(response, "Recommended age range updated successfully");
        }

        // Tasks with no recommended range (null) are treated as suitable for every age.
        private static IQueryable<TaskTemplateBase> ApplyAgeFilter(IQueryable<TaskTemplateBase> query, int? age)
        {
            if (!age.HasValue)
                return query;

            return query.Where(t =>
                (t.RecommendedAgeFrom <= age.Value) &&
                (t.RecommendedAgeTo >= age.Value));
        }

        private async Task<int?> GetParentActiveChildAgeAsync(string parentId)
        {
            var parent = await _context.Parents
                .Include(p => p.ActiveChild)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == parentId);

            return parent?.ActiveChild?.Age;
        }
    }
}

