using GoKidAPI.Data;
using GoKidAPI.DTO.Tasks.Requests;
using GoKidAPI.DTO.Tasks.Responses;
using GoKidAPI.Entity.Tasks;
using GoKidAPI.Enums.Tasks;
using GoKidAPI.Helpers;
using GoKidAPI.Services.ImageUploading;
using GoKidAPI.Services.TaskTemplate.Interfaces;
using GoKidAPI.Shared;

using Google;

namespace GoKidAPI.Services.TaskTemplate
{
    public class TextQuestionTaskService : ITextQuestionTaskService
    {
        private readonly AppDbContext _context;
        private readonly IFileUploader _fileUploader;
        private readonly ResponseHandler _response;

        public TextQuestionTaskService(
            AppDbContext context,
            IFileUploader fileUploader,
            ResponseHandler response)
        {
            _context = context;
            _fileUploader = fileUploader;
            _response = response;
        }

        public async Task<Response<TextQuestionTaskResponse>> CreateAsync(CreateTextQuestionRequest request)
        {
            var subCategory = await _context.SubCategories.FindAsync(request.SubCategoryId);
            if (subCategory == null)
                return _response.NotFound<TextQuestionTaskResponse>("Invalid SubCategoryId");

            string? iconUrl = null;
            string? iconPublicId = null;
            if (request.IconFile != null)
            {
                var upload = await _fileUploader.UploadAsync(request.IconFile);
                iconUrl = upload.Url;
                iconPublicId = upload.PublicId;
            }

            string? taskImageUrl = null;
            string? taskImagePublicId = null;
            if (request.TaskImageFile != null)
            {
                var upload = await _fileUploader.UploadAsync(request.TaskImageFile);
                taskImageUrl = upload.Url;
                taskImagePublicId = upload.PublicId;
            }

            var template = new TaskTemplateBase
            {
                TitleAr = request.TitleAr,
                TitleEn = request.TitleEn,
                DescriptionAr = request.DescriptionAr,
                DescriptionEn = request.DescriptionEn,
                IconUrl = iconUrl,
                IconPublicId = iconPublicId,
                SubCategoryId = request.SubCategoryId,
                Difficulty = request.Difficulty,
                BasePoints = request.BasePoints,
                RecommendedAgeFrom = request.RecommendedAgeFrom,
                RecommendedAgeTo = request.RecommendedAgeTo,
                TemplateType = TaskTemplateType.TextQuestion,
                CreatedBy = "platform-admin",
                QuestionText = request.QuestionText,
                TaskImageUrl = taskImageUrl,
                TaskImagePublicId = taskImagePublicId,
                ExpectedCorrectAnswer = request.ExpectedCorrectAnswer,
                CaseSensitive = request.CaseSensitive
            };

            _context.TaskTemplates.Add(template);
            await _context.SaveChangesAsync();

            var response = new TextQuestionTaskResponse
            {
                Id = template.Id,
                TitleAr = template.TitleAr,
                TitleEn = template.TitleEn,
                DescriptionAr = template.DescriptionAr,
                DescriptionEn = template.DescriptionEn,
                IconUrl = template.IconUrl,
                SubCategoryId = template.SubCategoryId.ToString(),
                SubCategoryNameEn = subCategory.NameEn,
                Difficulty = template.Difficulty,
                BasePoints = template.BasePoints,
                RecommendedAgeFrom = template.RecommendedAgeFrom,
                RecommendedAgeTo = template.RecommendedAgeTo,
                CreatedAt = template.CreatedAt,
                QuestionText = template.QuestionText,
                TaskImageUrl = template.TaskImageUrl,
                ExpectedCorrectAnswer = template.ExpectedCorrectAnswer,
                CaseSensitive = template.CaseSensitive
            };

            return _response.Created(response, "Text question task created successfully");
        }
    }
}
