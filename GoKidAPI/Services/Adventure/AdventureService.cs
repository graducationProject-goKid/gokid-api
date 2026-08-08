using GoKidAPI.Data;
using GoKidAPI.DTO.Adventures.Requests;
using GoKidAPI.DTO.Adventures.Responses;
using GoKidAPI.DTO.AdventureStory;
using GoKidAPI.Entity.Account.Identity;
using GoKidAPI.Entity.Institiution;
using GoKidAPI.Enums;
using GoKidAPI.Enums.Adventures;
using GoKidAPI.Enums.Shared;
using GoKidAPI.Jobs;
using GoKidAPI.Services.ImageUploading;
using GoKidAPI.Services.Notifications;
using GoKidAPI.Services.StoryGeneration;
using GoKidAPI.Services.TTSService;
using GoKidAPI.Shared;

using Hangfire;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GoKidAPI.Services.Adventure
{
    public class AdventureService : IAdventureService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IFileUploader _fileUploader;           // Cloudinary uploader
        private readonly ITextToSpeechService _textToSpeechService; // Text-to-Voice service
        private readonly IStoryGenerationService _storyGenerationService;

        private readonly ResponseHandler _response;
        private readonly ILogger<AdventureService> _logger;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly INotificationService _notificationService;


        public AdventureService(
            AppDbContext context,
            UserManager<AppUser> userManager,
            IFileUploader fileUploader,
            ITextToSpeechService textToSpeechService,
            ResponseHandler response,
            ILogger<AdventureService> logger,
            IBackgroundJobClient backgroundJobClient,
            IStoryGenerationService storyGenerationService,
            INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _fileUploader = fileUploader;
            _textToSpeechService = textToSpeechService;
            _response = response;
            _logger = logger;
            _backgroundJobClient = backgroundJobClient;
            _storyGenerationService = storyGenerationService;
            _notificationService = notificationService;
        }

        public async Task<Shared.Response<CreateAdventureResponse>> CreateAdventureAsync(
            string institutionAdminId,
            CreateAdventureRequest request)
        {
            // 1. التحقق من صلاحية الـ InstitutionAdmin
            var institutionAdmin = await _context.InstitutionAdmins
                .Include(a => a.Institution)
                .FirstOrDefaultAsync(a => a.Id == institutionAdminId);

            if (institutionAdmin?.Institution == null)
                return _response.NotFound<CreateAdventureResponse>("No institution found for this admin");

            // 2. إنشاء Adventure
            var adventure = new Entity.Institiution.Adventure
            {
                Id = Guid.NewGuid().ToString(),
                InstitutionId = institutionAdmin.Institution.Id,
                TitleEn = request.TitleEn,
                TitleAr = request.TitleAr,
                WeekDuration = request.WeekDuration,
                DescriptionEn = request.DescriptionEn,
                DescriptionAr = request.DescriptionAr,
                GoalEn = request.GoalEn,
                
                GoalAr = request.GoalAr,
                BonusPoints = request.BonusPoints,
                Status = AdventureStatus.Active,
                CreatedBy = institutionAdminId
            };


            if (request.BannerImage != null)
            {
                var uploadedResponse = await _fileUploader.UploadAsync(request.BannerImage);
                adventure.BannerImageUrl = uploadedResponse.Url;
                adventure.BannerImagePublicId = uploadedResponse.PublicId;
            }


            // 3. Text-to-Voice للـ Description (لو ما رفعش ملف جاهز)
            if (request.DescriptionVoiceFile != null)
            {
                var uploadResult = await _fileUploader.UploadAsync(request.DescriptionVoiceFile);
                adventure.DescriptionVoiceUrl = uploadResult.Url;
                adventure.DescriptionVoicePublicId = uploadResult.PublicId;
            }
            // I handle it with the background job
            //else if (!string.IsNullOrWhiteSpace(request.DescriptionEn))
            //{
            //    var voiceBytes = await _textToSpeechService.ConvertTextToSpeechAsync(request.DescriptionEn);

            //    var formFile = ConvertToFormFile(voiceBytes, "description.mp3");

            //    var voiceUpload = await _fileUploader.UploadAsync(formFile);
            //    adventure.DescriptionVoiceUrl = voiceUpload.Url;
            //    adventure.DescriptionVoicePublicId = voiceUpload.PublicId;
            //}

            _context.Adventures.Add(adventure);

            // 4. إضافة الـ Tasks (مع Drag & Drop order)
            foreach (var taskReq in request.Tasks.OrderBy(t => t.DayNumber))
            {
                var taskTemplate = await _context.TaskTemplates
                    .FindAsync(taskReq.TaskTemplateId);

                if (taskTemplate == null)
                    return _response.NotFound<CreateAdventureResponse>($"Task template {taskReq.TaskTemplateId} not found");

                var adventureTask = new AdventureTask
                {
                    Id = Guid.NewGuid().ToString(),
                    AdventureId = adventure.Id,
                    TaskTemplateId = taskReq.TaskTemplateId,
                    DayNumber = taskReq.DayNumber,
                    StoryText = taskReq.StoryText,
                    Stars = 3
                };

                // Text-to-Voice للـ StoryText لو موجود
                if (taskReq.StoryVoiceFile != null)
                {
                    var upload = await _fileUploader.UploadAsync(taskReq.StoryVoiceFile);
                    adventureTask.StoryVoiceUrl = upload.Url;
                    adventureTask.StoryVoicePublicId = upload.PublicId;
                }
                
                // لو مفيش → الـ background job هيعملها


                //if (!string.IsNullOrWhiteSpace(taskReq.StoryText))
                //{
                //    if (taskReq.StoryVoiceFile != null)
                //    {
                //        var upload = await _fileUploader.UploadAsync(taskReq.StoryVoiceFile);
                //        adventureTask.StoryVoiceUrl = upload.Url;
                //        adventureTask.StoryVoicePublicId = upload.PublicId;
                //    }
                //    else
                //    {
                //        var voicebytes = await _textToSpeechService.ConvertTextToSpeechAsync(taskReq.StoryText);

                //        var formFile = ConvertToFormFile(voicebytes, "description.mp3");

                //        var upload = await _fileUploader.UploadAsync(formFile);
                //        adventureTask.StoryVoiceUrl = upload.Url;
                //        adventureTask.StoryVoicePublicId = upload.PublicId;
                //    }
                //}

                _context.AdventureTasks.Add(adventureTask);
            }

            await _context.SaveChangesAsync();

            bool needsTts = (request.DescriptionVoiceFile == null && !string.IsNullOrWhiteSpace(request.DescriptionEn))
                 || request.Tasks.Any(t => t.StoryVoiceFile == null && !string.IsNullOrWhiteSpace(t.StoryText));

            if (needsTts)
            {
                _backgroundJobClient.Enqueue<AdventureTtsJob>(
                    job => job.ProcessAdventureTtsAsync(adventure.Id, institutionAdminId)
                );
            }

            var responseData = new CreateAdventureResponse
            {
                AdventureId = adventure.Id,
                TitleAr = adventure.TitleAr,
                TitleEn = adventure.TitleEn,
                DescriptionVoiceUrl = adventure.DescriptionVoiceUrl ?? "",
                TasksCount = request.Tasks.Count,
                VoiceProcessingInBackground = needsTts  // ← flag للـ frontend يعرف يشيل loading
            };

            _logger.LogInformation("Adventure created: {Title} by InstitutionAdmin {AdminId}", request.Title, institutionAdminId);

            return _response.Created(responseData, "Adventure created successfully with tasks");
        }

        public async Task<Shared.Response<PaginatedList<AdventureListItemResponse>>> GetAllAdventuresAsync(
            string institutionAdminId,
            GetAdventuresFilters filters)
        {
            var institution = await _context.Institutions
                .FirstOrDefaultAsync(i => i.InstitutionAdminId == institutionAdminId);

            if (institution == null)
                return _response.NotFound<PaginatedList<AdventureListItemResponse>>("No institution found for this admin");

            var query = _context.Adventures
                .Where(a => a.InstitutionId == institution.Id && !a.IsDeleted);

            // Filter by status
            if (filters.Status.HasValue)
                query = query.Where(a => a.Status == filters.Status.Value);

            // Search by title
            var term = filters.SearchTitle?.Trim().ToLower();

            if (!string.IsNullOrWhiteSpace(term))
            {
                query = query.Where(a =>
                    EF.Functions.Like(a.TitleEn, $"%{term}%") ||
                    EF.Functions.Like(a.TitleAr, $"%{term}%")
                );
            }

            // Sorting by english
            query = filters.SortColumn switch
            {
                AdventureSortingColumn.Title => filters.SortDirection == SortDirection.ASC
                    ? query.OrderBy(a => a.TitleEn)
                    : query.OrderByDescending(a => a.TitleEn),

                AdventureSortingColumn.CreatedAt => filters.SortDirection == SortDirection.ASC
                    ? query.OrderBy(a => a.CreatedAt)
                    : query.OrderByDescending(a => a.CreatedAt),

                _ => query.OrderByDescending(a => a.CreatedAt)
            };

            var totalCount = await query.CountAsync();

            var adventures = await query
                .Skip((filters.PageNumber - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .Select(a => new AdventureListItemResponse
                {
                    Id = a.Id,
                    TitleEn = a.TitleEn,
                    TitleAr = a.TitleAr,
                    DescriptionEn = a.DescriptionEn,
                    DescriptionAr = a.DescriptionAr,
                    DescriptionVoiceUrl = a.DescriptionVoiceUrl,
                    WeekDuration = a.WeekDuration,
                    BonusPoints = a.BonusPoints,
                    Status = a.Status,
                    TasksCount = a.Tasks.Count,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            var paginated = new PaginatedList<AdventureListItemResponse>(
                adventures, filters.PageNumber, filters.PageSize, totalCount);

            return _response.Success(paginated, "Adventures retrieved successfully");
        }


        public async Task<Response<AdventureDetailsResponse>> GetAdventureDetailsAsync(
            string currentUserId,
            string adventureId)
        {
            // جيب الـ User عشان نعرف دوره
            var user = await _userManager.FindByIdAsync(currentUserId);
            if (user == null)
                return _response.NotFound<AdventureDetailsResponse>("User not found");

            Entity.Institiution.Adventure? adventure = null;
            bool isAccessible = true;
            string? accessMessage = null;

            if (user.UserType == UserType.InstitutionAdmin)
            {
                // InstitutionAdmin يشوف أي Adventure في مؤسسته
                adventure = await _context.Adventures
                    .Include(a => a.Institution)
                    .Include(a => a.Tasks)
                        .ThenInclude(t => t.TaskTemplate)
                    .FirstOrDefaultAsync(a => a.Id == adventureId
                                           && a.Institution.InstitutionAdminId == currentUserId
                                           && !a.IsDeleted);
            }
            else if (user.UserType == UserType.Child)
            {
                var child = await _context.Childrens
                    .Include(c => c.Class)
                    .FirstOrDefaultAsync(c => c.Id == currentUserId);

                // الطفل يشوف فقط لو الـ Adventure مفعلة (Active)
                adventure = await _context.Adventures
                    .Include(a => a.Tasks)
                        .ThenInclude(t => t.TaskTemplate)
                    .Include(a => a.WeeklyAssignments)   // عشان نعرف لو مفعلة على كلاسه
                    .FirstOrDefaultAsync(a => a.Id == adventureId && !a.IsDeleted);

                if (adventure == null)
                    return _response.NotFound<AdventureDetailsResponse>("Adventure not found");

                if (adventure.Status != AdventureStatus.Active)
                {
                    isAccessible = false;
                    accessMessage = "هذه المغامرة غير متاحة حاليًا";
                }
                else
                {
                    // تحقق إن الـ Adventure مفعلة على كلاس الطفل (WeeklyAdventure)
                    var isAssignedToChildClass = await _context.WeeklyAdventures
                        .AnyAsync(w => w.AdventureId == adventureId
                                    && w.ClassId == child.ClassId);   // افترض إن عندك navigation من AppUser لـ Child

                    if (!isAssignedToChildClass)
                    {
                        isAccessible = false;
                        accessMessage = "هذه المغامرة غير متاحة لك حاليًا";
                    }
                }
            }
            else
            {
                return _response.Forbidden<AdventureDetailsResponse>("You do not have permission to view this adventure");
            }

            if (adventure == null)
                return _response.NotFound<AdventureDetailsResponse>("Adventure not found or you do not have access");

            // بناء الـ Response
            var response = new AdventureDetailsResponse
            {
                Id = adventure.Id,
                TitleEn = adventure.TitleEn,
                TitleAr = adventure.TitleAr,
                DescriptionEn = adventure.DescriptionEn,
                DescriptionAr = adventure.DescriptionAr,
                GoalEn = adventure.GoalEn,
                GoalAr = adventure.GoalAr,
                DescriptionVoiceUrl = adventure.DescriptionVoiceUrl,
                WeekDuration = adventure.WeekDuration,
                BonusPoints = adventure.BonusPoints,
                Status = adventure.Status,
                CreatedAt = adventure.CreatedAt,
                UpdatedAt = adventure.UpdatedAt,
                IsAccessible = isAccessible,
                AccessMessage = accessMessage,
                Tasks = adventure.Tasks
                    .OrderBy(t => t.DayNumber)
                    .Select(t => new AdventureTaskDetailResponse
                    {
                        Id = t.Id,
                        DayNumber = t.DayNumber,
                        TitleAr = t.TaskTemplate.TitleAr,
                        TitleEn = t.TaskTemplate.TitleEn,
                        StoryText = t.StoryText,
                        StoryVoiceUrl = t.StoryVoiceUrl,
                        Stars = t.Stars,
                        templateType = t.TaskTemplate.TemplateType.ToString()
                    })
                    .ToList()
            };

            return _response.Success(response, "Adventure details retrieved successfully");
        }


        #region Update Adventure

        public async Task<Shared.Response<AdventureDetailsResponse>> UpdateAdventureAsync(
    string institutionAdminId,
    string adventureId,
    UpdateAdventureRequest request)
        {
            var adventure = await _context.Adventures
                .Include(a => a.Tasks)
                .FirstOrDefaultAsync(a => a.Id == adventureId
                                       && a.Institution.InstitutionAdminId == institutionAdminId
                                       && !a.IsDeleted);

            if (adventure == null)
                return _response.NotFound<AdventureDetailsResponse>("Adventure not found or you do not have access");

            // Update basic fields
            if (!string.IsNullOrWhiteSpace(request.TitleEn))
                adventure.TitleEn = request.TitleEn;

            if (!string.IsNullOrWhiteSpace(request.TitleAr))
                adventure.TitleAr = request.TitleAr;

            if (!string.IsNullOrWhiteSpace(request.DescriptionEn))
                adventure.DescriptionEn = request.DescriptionEn;

            if (!string.IsNullOrWhiteSpace(request.DescriptionAr))
                adventure.DescriptionAr = request.DescriptionAr;

            if (!string.IsNullOrWhiteSpace(request.GoalEn))
                adventure.GoalEn = request.GoalEn;

            if (!string.IsNullOrWhiteSpace(request.GoalAr))
                adventure.GoalAr = request.GoalAr;

            if (request.WeekDuration.HasValue)
                adventure.WeekDuration = request.WeekDuration.Value;

            if (request.BonusPoints.HasValue)
                adventure.BonusPoints = request.BonusPoints.Value;

            if (request.DescriptionVoiceFile != null)
            {
                var upload = await _fileUploader.UploadAsync(request.DescriptionVoiceFile);
                adventure.DescriptionVoiceUrl = upload.Url;
                adventure.DescriptionVoicePublicId = upload.PublicId;
            }

            // ✅ Update Tasks لو موجودة في الـ Request
            if (request.Tasks != null && request.Tasks.Any())
            {
                // تحقق إن كل الـ TaskTemplateIds موجودة
                var templateIds = request.Tasks.Select(t => t.TaskTemplateId).ToList();
                var existingTemplates = await _context.TaskTemplates
                    .Where(t => templateIds.Contains(t.Id))
                    .Select(t => t.Id)
                    .ToListAsync();

                var missingTemplate = templateIds.FirstOrDefault(id => !existingTemplates.Contains(id));
                if (missingTemplate != null)
                    return _response.NotFound<AdventureDetailsResponse>(
                        $"Task template {missingTemplate} not found");

                // احذف القديمة
                _context.AdventureTasks.RemoveRange(adventure.Tasks);

                // أضف الجديدة
                foreach (var taskReq in request.Tasks.OrderBy(t => t.DayNumber))
                {
                    var adventureTask = new AdventureTask
                    {
                        Id = Guid.NewGuid().ToString(),
                        AdventureId = adventure.Id,
                        TaskTemplateId = taskReq.TaskTemplateId,
                        DayNumber = taskReq.DayNumber,
                        StoryText = taskReq.StoryText,
                        Stars = 3,
                        CreatedBy = institutionAdminId
                    };

                    if (taskReq.StoryVoiceFile != null)
                    {
                        var upload = await _fileUploader.UploadAsync(taskReq.StoryVoiceFile);
                        adventureTask.StoryVoiceUrl = upload.Url;
                        adventureTask.StoryVoicePublicId = upload.PublicId;
                    }

                    _context.AdventureTasks.Add(adventureTask);
                }

                // لو في tasks جديدة محتاجة TTS → Enqueue
                bool needsTts = request.Tasks.Any(t => t.StoryVoiceFile == null
                                                    && !string.IsNullOrWhiteSpace(t.StoryText));
                if (needsTts)
                {
                    _backgroundJobClient.Enqueue<AdventureTtsJob>(
                        job => job.ProcessAdventureTtsAsync(adventure.Id, institutionAdminId)
                    );
                }
            }

            adventure.UpdatedAt = DateTime.UtcNow;
            adventure.UpdatedBy = institutionAdminId;

            await _context.SaveChangesAsync();

            return await GetAdventureDetailsAsync(institutionAdminId, adventureId);
        }

        #endregion

        #region Delete Adventure (Soft Delete)
        public async Task<Shared.Response<object>> DeleteAdventureAsync(string institutionAdminId, string adventureId)
        {
            var adventure = await _context.Adventures
                .FirstOrDefaultAsync(a => a.Id == adventureId
                                       && a.Institution.InstitutionAdminId == institutionAdminId
                                       && !a.IsDeleted);

            if (adventure == null)
                return _response.NotFound<object>("Adventure not found or you do not have access");

            adventure.IsDeleted = true;
            adventure.UpdatedAt = DateTime.UtcNow;
            adventure.UpdatedBy = institutionAdminId;

            await _context.SaveChangesAsync();

            return _response.Success<object>(default, "Adventure deleted successfully (soft delete)");
        }
        #endregion

        #region Change Adventure Status
        public async Task<Shared.Response<object>> ChangeAdventureStatusAsync(
            string institutionAdminId,
            string adventureId,
            AdventureStatus status)
        {
            var adventure = await _context.Adventures
                .FirstOrDefaultAsync(a => a.Id == adventureId
                                       && a.Institution.InstitutionAdminId == institutionAdminId
                                       && !a.IsDeleted);

            var weeklyAdventureThatActivated = await _context.WeeklyAdventures
                .Where(w => w.AdventureId == adventureId && !w.IsDeleted)
                .ToListAsync();


            if (adventure == null)
                return _response.NotFound<object>("Adventure not found or you do not have access");

            adventure.Status = status;
            weeklyAdventureThatActivated.ForEach(w => w.Status = status == AdventureStatus.Active ? WeeklyAdventureStatus.Active : WeeklyAdventureStatus.Inactive);
            adventure.UpdatedAt = DateTime.UtcNow;
            adventure.UpdatedBy = institutionAdminId;

            await _context.SaveChangesAsync();

            return _response.Success<object>(adventure, $"Adventure status changed to {status}");
        }
        #endregion

        public async Task<Response<AssignAdventureToClassResponse>> AssignAdventureToClassAsync(
            string institutionAdminId,
            AssignAdventureToClassRequest request)
        {
            // 1. التحقق من الـ InstitutionAdmin ومؤسسته
            var institutionAdmin = await _context.InstitutionAdmins
                .Include(a => a.Institution)
                .FirstOrDefaultAsync(a => a.Id == institutionAdminId);

            if (institutionAdmin?.Institution == null)
                return _response.NotFound<AssignAdventureToClassResponse>("No institution found for this admin");

            // 2. التحقق من الـ Adventure موجودة وتابعة للمؤسسة
            var adventure = await _context.Adventures

                .FirstOrDefaultAsync(a => a.Id == request.AdventureId
                                       && a.InstitutionId == institutionAdmin.Institution.Id
                                       && !a.IsDeleted);

            if (adventure == null)
                return _response.NotFound<AssignAdventureToClassResponse>("Adventure not found or does not belong to your institution");

            // 3. التحقق من الـ Class موجودة وتابعة للمؤسسة
            var classEntity = await _context.Classes
                .FirstOrDefaultAsync(c => c.Id == request.ClassId
                                       && c.InstitutionId == institutionAdmin.Institution.Id
                                       && !c.IsDeleted);

            if (classEntity == null)
                return _response.NotFound<AssignAdventureToClassResponse>("Class not found or does not belong to your institution");

            // 4. التحقق من عدم وجود تكرار (نفس Adventure على نفس Class في نفس الأسبوع)
            var startOfWeek = request.StartDate.Date;
            var existingAssignment = await _context.WeeklyAdventures
                .AnyAsync(w => w.AdventureId == request.AdventureId
                            && w.ClassId == request.ClassId
                            && w.StartDate.Date == startOfWeek);

            if (existingAssignment)
                return _response.Conflict<AssignAdventureToClassResponse>("This adventure is already assigned to this class in the selected week");

            // 5. إنشاء WeeklyAdventure
            var endDate = startOfWeek.AddDays(adventure.WeekDuration - 1);

            var weeklyAdventure = new WeeklyAdventure
            {
                Id = Guid.NewGuid().ToString(),
                ClassId = request.ClassId,
                AdventureId = request.AdventureId,
                StartDate = startOfWeek,
                EndDate = endDate,
                Status = WeeklyAdventureStatus.Active,
                CreatedBy = institutionAdminId
            };

            _context.WeeklyAdventures.Add(weeklyAdventure);
            await _context.SaveChangesAsync();

            // Get the list of supervisor user IDs for the class to notify them
            var supervisorUserIds = await _context.ClassSupervisors
                .Where(cs => cs.ClassId == request.ClassId && !cs.IsDeleted)
                .Select(cs => cs.Supervisor.Id)
                .ToListAsync();

            foreach (var supervisorUserId in supervisorUserIds)
            {
                await _notificationService.SendAsync(
                    userId: supervisorUserId,
                    type: NotificationType.WeeklyAdventureStarted,
                    title: "Weekly Adventure Started",
                    body: $"A new weekly adventure '{adventure.TitleEn}' has been assigned and started to your class '{classEntity.Name}'.",
                    relatedEntityId: weeklyAdventure.Id
                );
            }

            // After assignation 
            _backgroundJobClient.Enqueue<AdventureAssignmentJob>(
                job => job.AssignDayOneTasksAsync(weeklyAdventure.Id)
            );

            var responseData = new AssignAdventureToClassResponse
            {
                WeeklyAdventureId = weeklyAdventure.Id,
                AdventureTitleEn = adventure.TitleEn,
                AdventureTitleAr = adventure.TitleAr,
                ClassName = classEntity.Name,
                ClassId = classEntity.Id,
                StartDate = weeklyAdventure.StartDate,
                EndDate = weeklyAdventure.EndDate
            };

            _logger.LogInformation("Adventure {AdventureId} assigned to Class {ClassId} starting {StartDate} by Admin {AdminId}",
                request.AdventureId, request.ClassId, startOfWeek, institutionAdminId);

            return _response.Created(responseData, "Adventure assigned to class successfully");
        }

        public async Task<Response<List<WeeklyAdventureClassResponse>>> GetClassesByWeeklyAdventureAsync(
    string userId,
    string weeklyAdventureId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return _response.NotFound<List<WeeklyAdventureClassResponse>>("User not found");

            var weeklyAdventure = await _context.WeeklyAdventures
                .Include(wa => wa.Adventure)
                    .ThenInclude(a => a.Institution)
                .Include(wa => wa.Class)
                    .ThenInclude(c => c.Children)
                .FirstOrDefaultAsync(wa => wa.Id == weeklyAdventureId && !wa.IsDeleted);

            if (weeklyAdventure == null)
                return _response.NotFound<List<WeeklyAdventureClassResponse>>("Weekly adventure not found");

            // تحقق من الـ Role
            if (user.UserType == UserType.InstitutionAdmin)
            {
                // تحقق إن الـ Adventure تابعة لمؤسسته
                var isHisInstitution = weeklyAdventure.Adventure.Institution.InstitutionAdminId == userId;
                if (!isHisInstitution)
                    return _response.Forbidden<List<WeeklyAdventureClassResponse>>(
                        "You do not have access to this adventure");
            }
            else if (user.UserType == UserType.Supervisor)
            {
                // تحقق إن الـ Supervisor مسؤول عن الكلاس دي
                var supervisor = await _context.Supervisors
                    .FirstOrDefaultAsync(s => s.Id == userId && !s.IsDeleted);

                if (supervisor == null)
                    return _response.NotFound<List<WeeklyAdventureClassResponse>>("Supervisor not found");

                var hasAccess = await _context.ClassSupervisors
                    .AnyAsync(cs => cs.SupervisorId == supervisor.Id
                                 && cs.ClassId == weeklyAdventure.ClassId
                                 && !cs.IsDeleted);

                if (!hasAccess)
                    return _response.Forbidden<List<WeeklyAdventureClassResponse>>(
                        "You do not have access to this adventure");
            }
            else
            {
                return _response.Forbidden<List<WeeklyAdventureClassResponse>>(
                    "You do not have permission to view this");
            }

            var result = new List<WeeklyAdventureClassResponse>
    {
        new()
        {
            ClassId = weeklyAdventure.Class.Id,
            ClassName = weeklyAdventure.Class.Name,
            ChildrenCount = weeklyAdventure.Class.Children.Count(c => !c.IsDeleted),
            StartDate = weeklyAdventure.StartDate,
            EndDate = weeklyAdventure.EndDate
        }
    };

            return _response.Success(result, "Classes retrieved successfully");
        }

        public async Task<Shared.Response<GenerateStoryResponse>> GenerateStoryAsync(
            string institutionAdminId,
            string adventureId)
        {
            var adventure = await _context.Adventures
                .Include(a => a.Tasks)
                    .ThenInclude(t => t.TaskTemplate)
                .FirstOrDefaultAsync(a => a.Id == adventureId
                                       && a.Institution.InstitutionAdminId == institutionAdminId
                                       && !a.IsDeleted);

            if (adventure == null)
                return _response.NotFound<GenerateStoryResponse>("Adventure not found");

            if (adventure.Tasks == null || !adventure.Tasks.Any())
                return _response.BadRequest<GenerateStoryResponse>("Adventure has no tasks");

            var tasksOrdered = adventure.Tasks.OrderBy(t => t.DayNumber).ToList();

            // ✅ استخدم الـ Service بدل الـ HttpClient مباشرة
            LlmStoryResponse llmResponse;
            try
            {
                llmResponse = await _storyGenerationService.GenerateAsync(
                    theme: adventure.DescriptionEn,
                    goal: adventure.GoalEn,
                    tasks: tasksOrdered.Select(t => t.TaskTemplate.DescriptionEn).ToList()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Story generation failed for adventure {Id}", adventureId);
                return _response.ServerError<GenerateStoryResponse>("Story generation service unavailable");
            }

            // حفظ الـ Intro
            adventure.IntroTitle = llmResponse.Intro.Title;
            adventure.IntroStory = llmResponse.Intro.Story;
            adventure.IntroVoiceUrl = null;
            adventure.IntroVoicePublicId = null;

            // حفظ الـ Outro
            adventure.OutroTitle = llmResponse.Outro.Title;
            adventure.OutroStory = llmResponse.Outro.Story;
            adventure.OutroVoiceUrl = null;
            adventure.OutroVoicePublicId = null;

            // حفظ الـ Story لكل Task
            foreach (var day in llmResponse.Days)
            {
                var task = tasksOrdered.FirstOrDefault(t => t.DayNumber == day.Day);
                if (task == null) continue;

                task.StoryTitle = day.Title;
                task.StoryText = day.Story;
                task.StoryVoiceUrl = null;
                task.StoryVoicePublicId = null;
            }

            adventure.UpdatedAt = DateTime.UtcNow;
            adventure.UpdatedBy = institutionAdminId;

            await _context.SaveChangesAsync();

            // Enqueue الـ TTS Job
            _backgroundJobClient.Enqueue<StoryTtsJob>(
                job => job.ProcessStoryTtsAsync(adventure.Id, institutionAdminId)
            );

            var result = new GenerateStoryResponse
            {
                AdventureId = adventure.Id,
                VoiceProcessingInBackground = true,
                Intro = new StoryIntroOutro
                {
                    Title = adventure.IntroTitle,
                    Story = adventure.IntroStory,
                    VoiceUrl = null, 
                },
                Outro = new StoryIntroOutro
                {
                    Title = adventure.OutroTitle,
                    Story = adventure.OutroStory,
                    VoiceUrl = null
                },
                Days = tasksOrdered.Select(t => new StoryDayItem
                {
                    DayNumber = t.DayNumber,
                    AdventureTaskId = t.Id,
                    Title = t.StoryTitle ?? "",
                    Story = t.StoryText ?? "",
                    VoiceUrl = null
                }).ToList()
            };

            return _response.Success(result,
                "Story generated successfully. Voice is being processed in background.");
        }

        public async Task<Response<AdventureStoryDetailsResponse>> GetAdventureStoryDetailsAsync(string adventureId)
        {
            var adventure = await _context.Adventures
                .Include(a => a.Tasks)
                    .ThenInclude(t => t.TaskTemplate)
                .FirstOrDefaultAsync(a => a.Id == adventureId && !a.IsDeleted);

            if (adventure == null)
                return _response.NotFound<AdventureStoryDetailsResponse>("Adventure not found");

            var storyDetails = new AdventureStoryDetailsResponse
            {
                AdventureId = adventure.Id,
                Title = adventure.TitleEn,
            };

            // Intro
            storyDetails.Intro = new StoryPart
            {
                Title = adventure.IntroTitle ?? "Adventure Introduction",
                Story = adventure.IntroStory ?? adventure.DescriptionEn,
                VoiceUrl = adventure.IntroVoiceUrl
            };

            // Days / Tasks
            foreach (var task in adventure.Tasks.OrderBy(t => t.DayNumber))
            {
                storyDetails.Days.Add(new StoryDay
                {
                    DayNumber = task.DayNumber,
                    AdventureTaskId = task.Id,
                    Title = task.StoryTitle ?? $"Day {task.DayNumber}",
                    Story = task.StoryText ?? "",
                    VoiceUrl = task.StoryVoiceUrl
                });
            }

            // Outro
            storyDetails.Outro = new StoryPart
            {
                Title = adventure.OutroTitle ?? "Adventure Conclusion",
                Story = adventure.OutroStory ?? "And so our adventure ends...",
                VoiceUrl = adventure.OutroVoiceUrl
            };

            return _response.Success(storyDetails, "Adventure story details retrieved successfully");
        }
    }
}
