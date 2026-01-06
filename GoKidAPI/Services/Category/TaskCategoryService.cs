using AutoMapper;

using GoKidAPI.Data;
using GoKidAPI.DTO.Category.Requests;
using GoKidAPI.DTO.Category.Responses;
using GoKidAPI.DTO.ImageUploading;
using GoKidAPI.Entity.Tasks;
using GoKidAPI.Services.ImageUploading;
using GoKidAPI.Shared;

using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Services.Category
{
    public class TaskCategoryService : ITaskCategoryService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ResponseHandler _response;
        private readonly ILogger<TaskCategoryService> _logger;
        private readonly IFileUploader _fileUploadService;

        public TaskCategoryService(AppDbContext context, IMapper mapper, ResponseHandler response, ILogger<TaskCategoryService> logger, IFileUploader fileUploadService)
        {
            _context = context;
            _mapper = mapper;
            _response = response;
            _logger = logger;
            _fileUploadService = fileUploadService;
        }

        public async Task<Response<List<CategoryResponse>>> GetAllAsync()
        {
            var categories = await _context.TaskCategories
                .AsNoTracking()
                .OrderBy(c => c.NameEn)
                .Select(c => new CategoryResponse
                {
                    Id = c.Id,
                    NameAr = c.NameAr,
                    NameEn = c.NameEn,
                    Icon = new UploadImageResponse
                    {
                        Url = c.IconUrl,
                        PublicId = c.IconPublicId
                    },
                    ColorHex = c.ColorHex,
                    SubCategoriesCount = c.SubCategories.Count()
                })
                .ToListAsync();

            return _response.Success<List<CategoryResponse>>(categories, "Categories retrived succsessfully.");
        }
        public async Task<Response<CategoryResponse>> GetByIdAsync(string id)
        {
            var category = await _context.TaskCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return _response.NotFound<CategoryResponse>("Category not found");

            var response = new CategoryResponse
            {
                Id = category.Id,
                NameAr = category.NameAr,
                Icon = new UploadImageResponse
                {
                    Url = category.IconUrl,
                    PublicId = category.IconPublicId
                },
                NameEn = category.NameEn,
                ColorHex = category.ColorHex
            };

            response.SubCategoriesCount = await _context.SubCategories
                .CountAsync(s => s.CategoryId == id);

            return _response.Success(response, $"Category {response.NameEn} retrived succsessfully with {response.SubCategoriesCount}.");
        }
        public async Task<Response<CategoryResponse>> CreateAsync(CreateCategoryRequest request)
        {
            var exists = await _context.TaskCategories
                .AnyAsync(c => c.NameEn.ToLower() == request.NameEn.ToLower() || c.NameAr == request.NameAr);

            if (exists)
                return _response.BadRequest<CategoryResponse>("Category with this name already exists");

            UploadImageResponse uploadResult = null;

            if (request.IconFile != null && request.IconFile.Length > 0)
            {
                uploadResult = await _fileUploadService.UploadAsync(request.IconFile);
            }
            var category = new TaskCategory
            {
                NameAr = request.NameAr,
                NameEn = request.NameEn,
                ColorHex = request.ColorHex,
                IconUrl = uploadResult.Url,
                IconPublicId = uploadResult.PublicId,
                CreatedAt = DateTime.UtcNow,
            };
            category.CreatedBy = "platform-admin"; // بعدين هناخده من الـ User

            _context.TaskCategories.Add(category);
            await _context.SaveChangesAsync();

            var response = new CategoryResponse
            {
                Id = category.Id,
                NameAr = category.NameAr,
                NameEn = category.NameEn,
                ColorHex = category.ColorHex,
                Icon = new UploadImageResponse
                {
                    PublicId = category.IconPublicId,
                    Url = category.IconUrl
                }
            };
            response.SubCategoriesCount = 0;

            _logger.LogInformation("Category created: {NameEn}", request.NameEn);
            return _response.Created(response, "Category created successfully");
        }
        public async Task<Response<CategoryResponse>> UpdateAsync(string id, UpdateCategoryRequest request)
        {
            var category = await _context.TaskCategories
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return _response.NotFound<CategoryResponse>("Category not found");


            if (!string.IsNullOrWhiteSpace(request.NameAr))
                category.NameAr = request.NameAr;

            if (!string.IsNullOrWhiteSpace(request.NameEn))
                category.NameEn = request.NameEn;

            if (!string.IsNullOrWhiteSpace(request.ColorHex))
                category.ColorHex = request.ColorHex;

            UploadImageResponse uploadResult = null;

            if (request.IconFile != null && request.IconFile.Length > 0)
            {
                await _fileUploadService.DeleteAsync(category.IconPublicId);

                uploadResult = await _fileUploadService.UploadAsync(request.IconFile);
                category.IconUrl = uploadResult.Url;
                category.IconPublicId = uploadResult.PublicId;
            }

            category.UpdatedAt = DateTime.UtcNow;
            category.UpdatedBy = "platform-admin";

            await _context.SaveChangesAsync();

            var response = new CategoryResponse
            {
                Id = category.Id,
                NameAr = category.NameAr,
                NameEn = category.NameEn,
                ColorHex = category.ColorHex,
                Icon = new UploadImageResponse
                {
                    Url = uploadResult?.Url ?? category.IconUrl,
                    PublicId = uploadResult?.PublicId
                },
                SubCategoriesCount = await _context.SubCategories
                    .CountAsync(s => s.CategoryId == id)
            };

            return _response.Success(response, "Category updated successfully");
        }

        public async Task<Response<string>> DeleteAsync(string id)
        {
            var category = await _context.TaskCategories
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return _response.NotFound<string>("Category not found");

            if (category.SubCategories.Any())
                return _response.BadRequest<string>("Cannot delete category with subcategories");

            _context.TaskCategories.Remove(category);
            await _context.SaveChangesAsync();

            return _response.Deleted<string>("Category deleted successfully");
        }
    }
}
