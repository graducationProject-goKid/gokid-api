using GoKidAPI.Data;
using GoKidAPI.DTO.Levels.Requests;
using GoKidAPI.DTO.Levels.Responses;
using GoKidAPI.Entity.Levels;
using GoKidAPI.Services.ImageUploading;
using GoKidAPI.Shared;

using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Services.Levels
{
    public class LevelService : ILevelService
    {
        private readonly AppDbContext _context;
        private readonly IFileUploader _fileUploader;
        private readonly ResponseHandler _response;
        private readonly ILogger<LevelService> _logger;

        public LevelService(
            AppDbContext context,
            IFileUploader fileUploader,
            ResponseHandler response,
            ILogger<LevelService> logger)
        {
            _context = context;
            _fileUploader = fileUploader;
            _response = response;
            _logger = logger;
        }

        public async Task<Response<List<LevelResponse>>> GetAllAsync()
        {
            var levels = await _context.Levels
                .AsNoTracking()
                .OrderBy(l => l.Order)
                .Select(l => MapToResponse(l))
                .ToListAsync();

            return _response.Success(levels, "Levels retrieved successfully.");
        }

        public async Task<Response<LevelResponse>> GetByIdAsync(string levelId)
        {
            var level = await _context.Levels
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == levelId);

            if (level == null)
                return _response.NotFound<LevelResponse>("Level not found.");

            return _response.Success(MapToResponse(level), "Level retrieved successfully.");
        }

        public async Task<Response<LevelResponse>> CreateAsync(CreateLevelRequest request, string adminId)
        {
            var orderConflict = await _context.Levels.AnyAsync(l => l.Order == request.Order);
            if (orderConflict)
                return _response.BadRequest<LevelResponse>($"A level with order {request.Order} already exists.");

            string? badgeUrl = null;
            string? badgePublicId = null;

            if (request.Badge != null)
            {
                try
                {
                    var upload = await _fileUploader.UploadAsync(request.Badge);
                    badgeUrl = upload.Url;
                    badgePublicId = upload.PublicId;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Badge upload failed for level {Name}", request.Name);
                }
            }

            var level = new Level
            {
                Name = request.Name,
                Order = request.Order,
                MinPoints = request.MinPoints,
                BadgeUrl = badgeUrl,
                BadgePublicId = badgePublicId,
                CreatedBy = adminId
            };

            _context.Levels.Add(level);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Level created: {Name} (Order {Order})", level.Name, level.Order);
            return _response.Created(MapToResponse(level), "Level created successfully.");
        }

        public async Task<Response<LevelResponse>> UpdateAsync(string levelId, UpdateLevelRequest request, string adminId)
        {
            var level = await _context.Levels.FirstOrDefaultAsync(l => l.Id == levelId);

            if (level == null)
                return _response.NotFound<LevelResponse>("Level not found.");

            if (request.Order.HasValue && request.Order.Value != level.Order)
            {
                var orderConflict = await _context.Levels
                    .AnyAsync(l => l.Order == request.Order.Value && l.Id != levelId);
                if (orderConflict)
                    return _response.BadRequest<LevelResponse>($"A level with order {request.Order.Value} already exists.");

                level.Order = request.Order.Value;
            }

            if (!string.IsNullOrWhiteSpace(request.Name)) level.Name = request.Name;
            if (request.MinPoints.HasValue) level.MinPoints = request.MinPoints.Value;

            if (request.Badge != null)
            {
                try
                {
                    var upload = await _fileUploader.UploadAsync(request.Badge);
                    level.BadgeUrl = upload.Url;
                    level.BadgePublicId = upload.PublicId;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Badge upload failed during level update. LevelId: {Id}", levelId);
                }
            }

            level.UpdatedAt = DateTime.UtcNow;
            level.UpdatedBy = adminId;
            await _context.SaveChangesAsync();

            return _response.Success(MapToResponse(level), "Level updated successfully.");
        }

        public async Task<Response<object>> DeleteAsync(string levelId, string adminId)
        {
            var level = await _context.Levels.FirstOrDefaultAsync(l => l.Id == levelId);

            if (level == null)
                return _response.NotFound<object>("Level not found.");

            var childrenCount = await _context.Childrens
                .IgnoreQueryFilters()
                .CountAsync(c => c.LevelId == levelId);

            if (childrenCount > 0)
                return _response.BadRequest<object>(
                    $"Cannot delete this level — {childrenCount} child(ren) are currently assigned to it.");

            level.IsDeleted = true;
            level.UpdatedAt = DateTime.UtcNow;
            level.UpdatedBy = adminId;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Level soft-deleted: {Name} by admin {AdminId}", level.Name, adminId);
            return _response.Deleted<object>("Level deleted successfully.");
        }

        private static LevelResponse MapToResponse(Level level) => new()
        {
            Id = level.Id,
            Name = level.Name,
            Order = level.Order,
            MinPoints = level.MinPoints,
            BadgeUrl = level.BadgeUrl,
            CreatedAt = level.CreatedAt
        };
    }
}
