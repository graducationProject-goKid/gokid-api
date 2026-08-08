using DocumentFormat.OpenXml.Spreadsheet;
using FluentValidation;

using GoKidAPI.DTO.Tasks.Requests;
using GoKidAPI.DTO.Tasks.Responses;
using GoKidAPI.Enums.Tasks;
using GoKidAPI.Services.TaskTemplate;
using GoKidAPI.Services.TaskTemplate.Interfaces;
using GoKidAPI.Shared;

using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GoKidAPI.Controllers
{
    [Route("api/task-templates")]
    [ApiController]
    //[Authorize(Roles = "PlatformAdmin")]
    public class TaskTemplateController : ControllerBase
    {
        private readonly IInstantRewardTaskService _instantRewardService;
        private readonly ITextQuestionTaskService _textQuestionService;
        private readonly IVoiceQuestionTaskService _voiceQuestionService;
        private readonly IEvidenceSubmissionTaskService _evidenceSubmissionService;
        private readonly ITaskTemplateQueryService _taskTemplateQueryService;

        private readonly IValidator<CreateTextQuestionRequest> _textQuestionValidator;
        private readonly IValidator<CreateVoiceQuestionRequest> _voiceQuestionValidator;
        private readonly IValidator<CreateEvidenceSubmissionRequest> _evidenceSubmissionValidator;
        private readonly IValidator<UpdateRecommendedAgeRequest> _updateRecommendedAgeValidator;

        private readonly ResponseHandler _response;

        public TaskTemplateController(
            IInstantRewardTaskService instantRewardService,
            ITextQuestionTaskService textQuestionService,
            IVoiceQuestionTaskService voiceQuestionService,
            IEvidenceSubmissionTaskService evidenceSubmissionService,

            IValidator<CreateTextQuestionRequest> textQuestionValidator,
            IValidator<CreateVoiceQuestionRequest> voiceQuestionValidator,
            IValidator<CreateEvidenceSubmissionRequest> evidenceSubmissionValidator,
            IValidator<UpdateRecommendedAgeRequest> updateRecommendedAgeValidator,
            ResponseHandler response,
            ITaskTemplateQueryService taskTemplateQueryService)
        {
            _instantRewardService = instantRewardService;
            _textQuestionService = textQuestionService;
            _voiceQuestionService = voiceQuestionService;
            _evidenceSubmissionService = evidenceSubmissionService;

            _textQuestionValidator = textQuestionValidator;
            _voiceQuestionValidator = voiceQuestionValidator;
            _evidenceSubmissionValidator = evidenceSubmissionValidator;
            _updateRecommendedAgeValidator = updateRecommendedAgeValidator;

            _response = response;
            _taskTemplateQueryService = taskTemplateQueryService;
        }



        /// <summary>
        /// Create an Instant Reward Task
        /// </summary>
        /// <remarks>
        /// **Instant Reward** – Child presses Start → Done → gets points instantly (no verification needed)
        ///
        /// ### Example Request
        /// ```json
        /// {
        ///   "titleAr": "اشرب كوباية مية",
        ///   "titleEn": "Drink a glass of water",
        ///   "descriptionAr": "مهمة سريعة للتشجيع",
        ///   "descriptionEn": "Quick encouragement task",
        ///   "iconFile": [file],
        ///   "taskImageFile": [file],

        ///   "subCategoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        ///   "difficulty": "Easy",
        ///   "basePoints": 10
        /// }
        /// ```
        /// </remarks>
        /// <response code="201">Instant reward task created successfully</response>
        /// <response code="400">Validation error (invalid request data)</response>
        /// <response code="404">SubCategory not found</response>
        /// <response code="401">Unauthorized</response>
        ///<response code = "403" > Forbidden </response>
        /// <response code="500">Internal server error</response>
        [HttpPost("instant-reward")]
        [ProducesResponseType(typeof(Shared.Response<InstantRewardTaskResponse>), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateInstantReward([FromForm] CreateInstantRewardRequest request)
        {
            //var validationResult = await _instantRewardValidator.ValidateAsync(request);
            //if (!validationResult.IsValid)
            //{
            //    var errors = validationResult.Errors.Select(e => e.ErrorMessage);
            //    return BadRequest(_response.BadRequest<object>(string.Join(", ", errors)));
            //}

            var result = await _instantRewardService.CreateAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Create a Text Question Task
        /// </summary>
        /// <remarks>
        /// **Text Question** – Child answers a text-based question. Answer is verified automatically.
        ///
        /// ### Example Request
        /// ```json
        /// {
        ///   "titleAr": "ما عاصمة مصر؟",
        ///   "titleEn": "What is the capital of Egypt?",
        ///   "descriptionAr": "سؤال تعليمي",
        ///   "descriptionEn": "Educational question",
        ///   "iconFile": [file],
        ///   "taskImageFile": [file],
        ///   "subCategoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        ///   "difficulty": "Medium",
        ///   "basePoints": 20,
        ///   "questionText": "ما عاصمة مصر؟",
        ///   "expectedCorrectAnswer": "القاهرة",
        ///   "caseSensitive": false
        /// }
        /// ```
        /// </remarks>
        /// <response code="201">Text question task created successfully</response>
        /// <response code="400">Validation error (invalid request data)</response>
        /// <response code="404">SubCategory not found</response>
        /// <response code="401">Unauthorized</response>
        ///<response code = "403" > Forbidden </response>
        /// <response code="500">Internal server error</response>
        [HttpPost("text-question")]
        [ApiExplorerSettings(IgnoreApi =true)]
        [ProducesResponseType(typeof(Shared.Response<TextQuestionTaskResponse>), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateTextQuestion([FromForm] CreateTextQuestionRequest request)
        {
            var validationResult = await _textQuestionValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage);
                return BadRequest(_response.BadRequest<object>(string.Join(", ", errors)));
            }

            var result = await _textQuestionService.CreateAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Create a Voice Question Task
        /// </summary>
        /// <remarks>
        /// **Voice Question** – Child records a voice answer. Validated by AI or manual review.
        ///
        /// ### Example Request
        /// ```json
        /// {
        ///   "titleAr": "اقرأ سورة الفاتحة",
        ///   "titleEn": "Recite Surah Al-Fatihah",
        ///   "descriptionAr": "قراءة صوتية",
        ///   "descriptionEn": "Voice recitation",
        ///   "iconFile": [file],
        ///   "taskImageFile": [file],
        ///   "subCategoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        ///   "difficulty": "Hard",
        ///   "basePoints": 50,
        ///   "questionText": "اقرأ سورة الفاتحة بصوتك",
        ///   "expectedCorrectAnswer": "الفاتحة",
        ///   "voicePrompt": "Please start recording now",
        ///   "maxVoiceAttempts": 3,
        ///   "maxVoiceDurationSeconds": 30,
        /// }
        /// ```
        /// </remarks>
        /// <response code="201">Voice question task created successfully</response>
        /// <response code="400">Validation error (invalid request data)</response>
        /// <response code="404">SubCategory not found</response>
        /// <response code="401">Unauthorized</response>
        ///<response code = "403" > Forbidden </response>
        /// <response code="500">Internal server error</response>

        [HttpPost("voice-question")]
        [ProducesResponseType(typeof(Shared.Response<VoiceQuestionTaskResponse>), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateVoiceQuestion([FromForm] CreateVoiceQuestionRequest request)
        {
            var validationResult = await _voiceQuestionValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage);
                return BadRequest(_response.BadRequest<object>(string.Join(", ", errors)));
            }

            var result = await _voiceQuestionService.CreateAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>
        /// Create an Evidence Submission Task
        /// </summary>
        /// <remarks>
        /// **Evidence Submission** – Child uploads photo/video proof. Reviewed by Parent, Platform Admin, or AI.
        ///
        /// ### Example Request
        /// ```json
        /// {
        ///   "titleAr": "نظف غرفتك",
        ///   "titleEn": "Clean your room",
        ///   "descriptionAr": "التقط صورة بعد التنظيف",
        ///   "descriptionEn": "Take a photo after cleaning",
        ///   "iconFile": [file],
        ///   "taskImageFile": [file],
        ///   "subCategoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        ///   "difficulty": "Medium",
        ///   "basePoints": 30,
        ///   "instructionsText": "التقط صورة واضحة لغرفتك بعد التنظيف",
        ///   "evidenceType": "Image",
        ///   "reviewBy": "Parent"
        /// }
        /// ```
        ///
        /// **EvidenceType:** Image, Video  
        /// **ReviewBy:** Parent, PlatformAdmin, AI
        /// </remarks>
        /// <response code="201">Evidence submission task created successfully</response>
        /// <response code="400">Validation error (invalid request data)</response>
        /// <response code="404">SubCategory not found</response>
        /// <response code="401">Unauthorized</response>
        ///<response code = "403" > Forbidden </response>
        /// <response code="500">Internal server error</response>
        [HttpPost("evidence-submission")]
        [ProducesResponseType(typeof(Shared.Response<EvidenceSubmissionTaskResponse>), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateEvidenceSubmission([FromForm] CreateEvidenceSubmissionRequest request)
        {
            var validationResult = await _evidenceSubmissionValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage);
                return BadRequest(_response.BadRequest<object>(string.Join(", ", errors)));
            }

            var result = await _evidenceSubmissionService.CreateAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }


    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] TaskRequestFilters filters)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var result = await _taskTemplateQueryService.GetAllAsync(filters, role, requesterId);

        return Ok(result);
    }

    /// <summary>
    /// Update the recommended age range of a task template
    /// </summary>
    /// <remarks>
    /// Platform Admin sets/edits the recommended age range (e.g. 5-7 years) used to suggest
    /// age-appropriate tasks when assigning to a child or building an Adventure for a class.
    /// </remarks>
    /// <response code="200">Recommended age range updated successfully</response>
    /// <response code="400">Validation error (RecommendedAgeFrom must be &lt;= RecommendedAgeTo)</response>
    /// <response code="404">Task template not found</response>
    [HttpPatch("{id}/recommended-age")]
    [ProducesResponseType(typeof(Response<TaskTemplateListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRecommendedAge(string id, [FromBody] UpdateRecommendedAgeRequest request)
    {
        var validationResult = await _updateRecommendedAgeValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage);
            return BadRequest(_response.BadRequest<object>(string.Join(", ", errors)));
        }

        var result = await _taskTemplateQueryService.UpdateRecommendedAgeAsync(id, request);
        return StatusCode((int)result.StatusCode, result);
    }

    /// <summary>
    /// Get task template by ID and Type
    /// </summary>
    /// <param name="id">Task template ID</param>
    /// <param name="type">Task template type (InstantReward, TextQuestion, VoiceQuestion, EvidenceSubmission)</param>
    [HttpGet("{id}")]
        [ProducesResponseType(typeof(Response<InstantRewardTaskResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response<TextQuestionTaskResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response<VoiceQuestionTaskResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response<EvidenceSubmissionTaskResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetById(string id, TaskTemplateType taskType)
        {
            var result = await _taskTemplateQueryService.GetByIdAsync(id, taskType);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("subcategory/{subCategoryId}")]
        [ProducesResponseType(typeof(Shared.Response<PaginatedList<TaskTemplateListItemResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBySubCategory(
            string subCategoryId,
            [FromQuery] DifficultyLevel? difficulty,
            [FromQuery] int? recommendedAge,
            [FromQuery] RequestFilters<TaskSortingColumn> filters)
        {
            var result = await _taskTemplateQueryService.GetBySubCategoryAsync(subCategoryId, difficulty, filters, recommendedAge);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("category/{categoryId}")]
        [ProducesResponseType(typeof(Shared.Response<PaginatedList<TaskTemplateListItemResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByCategory(
            string categoryId,
            [FromQuery] int? recommendedAge,
            [FromQuery] RequestFilters<TaskSortingColumn> filters)
        {
            var result = await _taskTemplateQueryService.GetByCategoryAsync(categoryId, filters, recommendedAge);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}

