using AutoMapper;

using GoKidAPI.Data;
using GoKidAPI.DTO.Category.Requests;
using GoKidAPI.DTO.Category.Responses;
using GoKidAPI.DTO.ImageUploading;
using GoKidAPI.Entity.Tasks;
using GoKidAPI.Services.ImageUploading;
using GoKidAPI.Shared;

using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Services.SubCategory
{
    public class TaskSubCategoryService : ITaskSubCategoryService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IFileUploader _fileUploader;
        private readonly ResponseHandler _response;
        private readonly ILogger<TaskSubCategoryService> _logger;

        public TaskSubCategoryService(AppDbContext context, IMapper mapper, IFileUploader fileUploader,
            ResponseHandler response, ILogger<TaskSubCategoryService> logger)
        {
            _context = context;
            _mapper = mapper;
            _fileUploader = fileUploader;
            _response = response;
            _logger = logger;
        }

        public async Task<Response<List<SubCategoryResponse>>> GetAllAsync()
        {
            var subs = await _context.SubCategories
                .AsNoTracking()
                .Include(s => s.Category)
                .OrderBy(s => s.NameEn)
                .Select(s => new SubCategoryResponse
                {
                    Id = s.Id,
                    NameAr = s.NameAr,
                    NameEn = s.NameEn,
                    Icon = s.IconUrl != null ? new UploadImageResponse { Url = s.IconUrl, PublicId = s.IconPublicId! } : null,
                    CategoryId = s.CategoryId,
                    CategoryNameEn = s.Category.NameEn,
                })
                .ToListAsync();

            return _response.Success(subs, "SubCategories retrieved successfully");
        }

        public async Task<Response<List<SubCategoryResponse>>> GetByCategoryIdAsync(string categoryId)
        {
            var exists = await _context.TaskCategories.AnyAsync(c => c.Id == categoryId);
            if (!exists)
                return _response.NotFound<List<SubCategoryResponse>>("Category not found");

            var subs = await _context.SubCategories
                .AsNoTracking()
                .Where(s => s.CategoryId == categoryId)
                .OrderBy(s => s.NameEn)
                .Select(s => new SubCategoryResponse
                {
                    Id = s.Id,
                    NameAr = s.NameAr,
                    NameEn = s.NameEn,
                    Icon = s.IconUrl != null ? new UploadImageResponse { Url = s.IconUrl, PublicId = s.IconPublicId! } : null,
                    CategoryId = s.CategoryId,
                    CategoryNameEn = s.Category.NameEn,
                })
                .ToListAsync();

            return _response.Success(subs,"Sub Categories retrived succsessfully");
        }

        public async Task<Response<SubCategoryResponse>> CreateAsync(CreateSubCategoryRequest request)
        {
            var categoryExists = await _context.TaskCategories.AnyAsync(c => c.Id == request.CategoryId);
            if (!categoryExists)
                return _response.BadRequest<SubCategoryResponse>("Invalid CategoryId");

            var exists = await _context.SubCategories
                .AnyAsync(s => s.CategoryId == request.CategoryId &&
                              (s.NameEn.ToLower() == request.NameEn.ToLower() || s.NameAr == request.NameAr));

            if (exists)
                return _response.BadRequest<SubCategoryResponse>("SubCategory with this name already exists in this category");

            var uploadResult = await _fileUploader.UploadAsync(request.IconFile);

            var sub = new TaskSubCategory
            {
                NameAr = request.NameAr,
                NameEn = request.NameEn,
                IconUrl = uploadResult.Url,
                IconPublicId = uploadResult.PublicId,
                CategoryId = request.CategoryId,
                CreatedBy = "platform-admin"
            };

            _context.SubCategories.Add(sub);
            await _context.SaveChangesAsync();

            var resp = new SubCategoryResponse
            {
                Id = sub.Id,
                NameAr = sub.NameAr,
                NameEn = sub.NameEn,
                Icon = uploadResult,
                CategoryId = sub.CategoryId,
                CategoryNameEn = (await _context.TaskCategories.FindAsync(request.CategoryId))!.NameEn,
            };

            return _response.Created(resp, "SubCategory created successfully");
        }

        public async Task<Response<SubCategoryResponse>> UpdateAsync(string id, UpdateSubCategoryRequest request)
        {
            var sub = await _context.SubCategories.Include(sc=>sc.Category).FirstOrDefaultAsync(sc=>sc.Id == id);
            if (sub == null)
                return _response.NotFound<SubCategoryResponse>("SubCategory not found");

            if (!string.IsNullOrEmpty(request.NameAr)) sub.NameAr = request.NameAr;
            if (!string.IsNullOrEmpty(request.NameEn)) sub.NameEn = request.NameEn;
            if (!string.IsNullOrEmpty(request.CategoryId))
            {
                var categoryExists = await _context.TaskCategories.AnyAsync(c => c.Id == request.CategoryId);
                if (!categoryExists)
                    return _response.BadRequest<SubCategoryResponse>("Invalid CategoryId");
                sub.CategoryId = request.CategoryId;
            }
            if (request.IconFile != null)
            {
                if (!string.IsNullOrEmpty(sub.IconPublicId))
                    await _fileUploader.DeleteAsync(sub.IconPublicId);

                var upload = await _fileUploader.UploadAsync(request.IconFile);
                sub.IconUrl = upload.Url;
                sub.IconPublicId = upload.PublicId;
            }

            sub.UpdatedAt = DateTime.UtcNow;
            sub.UpdatedBy = "platform-admin";

            await _context.SaveChangesAsync();

            var resp = new SubCategoryResponse
            {
                Id = sub.Id,
                NameAr = sub.NameAr,
                NameEn = sub.NameEn,
                Icon = sub.IconUrl != null ? new UploadImageResponse { Url = sub.IconUrl, PublicId = sub.IconPublicId! } : null,
                CategoryId = sub.CategoryId,
                CategoryNameEn = sub.Category.NameEn,
                CategoryNameAr = sub.Category.NameAr,
            };

            return _response.Success(resp, "SubCategory updated successfully");
        }

        public async Task<Response<string>> DeleteAsync(string id)
        {
            var sub = await _context.SubCategories.FirstOrDefaultAsync(s => s.CategoryId == id);

            if (sub == null)
                return _response.NotFound<string>("SubCategory not found");

            //if (sub.Templates.Any())
            //    return _response.BadRequest<string>("Cannot delete subcategory that has templates");

            if (!string.IsNullOrEmpty(sub.IconPublicId))
                await _fileUploader.DeleteAsync(sub.IconPublicId);

            _context.SubCategories.Remove(sub);
            await _context.SaveChangesAsync();

            return _response.Deleted<string>("SubCategory deleted successfully");
        }

        public async Task<Response<SubCategoryResponse>> GetByIdAsync(string id)
        {
            var sub = await _context.SubCategories.Include(s => s.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sub == null)
                return _response.NotFound<SubCategoryResponse>("SubCategory not found");

            var resp = new SubCategoryResponse
            {
                Id = sub.Id,
                NameAr = sub.NameAr,
                NameEn = sub.NameEn,
                Icon = sub.IconUrl != null ? 
                new UploadImageResponse { 
                    Url = sub.IconUrl!, 
                    PublicId = sub.IconPublicId! } : null,
                CategoryId = sub.CategoryId,
                CategoryNameEn = sub.Category.NameEn,
                CategoryNameAr = sub.Category.NameAr    
            };

            return _response.Success(resp, "Sub Category retrived succsessfully");
        }
    }
}
