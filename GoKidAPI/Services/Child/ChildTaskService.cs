using GoKidAPI.Data;
using GoKidAPI.DTO.Adventures.Requests;
using GoKidAPI.DTO.Adventures.Responses;
using GoKidAPI.DTO.Adventures.Responses.GoKidAPI.Enums.Adventures;
using GoKidAPI.DTO.ChidAdventure.Responses;
using GoKidAPI.DTO.Childs.Requests;
using GoKidAPI.DTO.Childs.Responses;
using GoKidAPI.DTO.Tasks.Responses;
using GoKidAPI.Entity;
using GoKidAPI.Entity.Institiution;
using GoKidAPI.Entity.Tasks;
using GoKidAPI.Enums;
using GoKidAPI.Enums.Adventures;
using GoKidAPI.Enums.Tasks;
using GoKidAPI.Services.ImageUploading;
using GoKidAPI.Services.Points;
using GoKidAPI.Services.Notifications;
using GoKidAPI.Shared;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using TaskStatus = GoKidAPI.Enums.Tasks.TaskStatus;

namespace GoKidAPI.Services.Child
{
    public class ChildTaskService : IChildTaskService
    {
        private readonly AppDbContext _context;
        private readonly ResponseHandler _response;
        private readonly IFileUploader _fileUploader;
        private readonly IHttpClientFactory _httpClientFactory;  // للـ AI API call
        private readonly ILogger<ChildTaskService> _logger;
        private readonly IPointsService _pointsService;
        private readonly INotificationService _notificationService;

        public ChildTaskService(
            AppDbContext context,
            ResponseHandler response,
            IFileUploader fileUploader,
            IHttpClientFactory httpClientFactory,
            ILogger<ChildTaskService> logger,
            INotificationService notificationService)
        {
            _context = context;
            _response = response;
            _fileUploader = fileUploader;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<Response<List<TaskTemplateListItemResponse>>> GetDailyGeneralTasksAsync(string childId, DateTime date)
        {
            var childExists = await _context.Childrens.AnyAsync(c => c.Id == childId);
            if (!childExists)
                return _response.NotFound<List<TaskTemplateListItemResponse>>("Child not found");

            var start = date.Date;
            var end = start.AddDays(1);

            var todayTasks = await _context.ChildTasks
                .Include(ct => ct.Template)
                    .ThenInclude(t => t.SubCategory)
                .Where(ct => ct.ChildId == childId &&
                             ct.AssignedAt >= start &&
                             ct.AssignedAt < end &&
                             ct.Source == TaskSource.SystemGeneral)
                .Take(5)
                .ToListAsync();

            var result = todayTasks.Select(ct => new TaskTemplateListItemResponse
            {
                Id = ct.Id,
                TitleAr = ct.Template.TitleAr,
                TitleEn = ct.Template.TitleEn,
                DescriptionAr = ct.Template.DescriptionAr,
                DescriptionEn = ct.Template.DescriptionEn,
                SubCategoryNameEn = ct.Template.SubCategory?.NameEn,
                SubCategoryId = ct.Template.SubCategoryId,
                Difficulty = ct.Template.Difficulty,
                IconUrl = ct.Template.IconUrl,
                BasePoints = ct.Template.BasePoints,
                TemplateType = ct.Template.TemplateType,
            }).ToList();

            return _response.Success(result, "Daily general tasks retrieved successfully");
        }

        public async Task<Response<SubmitTaskResponse>> SubmitTaskAsync(string childId, SubmitTaskRequest request)
        {
            var childTask = await _context.ChildTasks
                .Include(t => t.Template)
                .Include(t => t.Child)
                .FirstOrDefaultAsync(t => t.Id == request.TaskId && t.ChildId == childId && !t.IsDeleted);

            if (childTask == null)
                return _response.NotFound<SubmitTaskResponse>("Task not found or does not belong to this child");

            if (childTask.Status == TaskStatus.Completed || childTask.Status == TaskStatus.Rejected)
                return _response.BadRequest<SubmitTaskResponse>("Task already processed");

            var template = childTask.Template;
            var response = new SubmitTaskResponse { TaskId = childTask.Id };

            childTask.UpdatedAt = DateTime.Now;
            childTask.UpdatedBy = childId;

            switch (template.TemplateType)
            {
                case TaskTemplateType.InstantReward:
                    // Start → Done → نقاط فورًا
                    childTask.Status = TaskStatus.Completed;
                    childTask.CompletedAt = DateTime.UtcNow;
                    childTask.Child.TotalPoints += template.BasePoints;

                    // سجل النقاط
                    _context.PointsTransactions.Add(new PointsTransaction
                    {
                        ChildId = childId,
                        Points = template.BasePoints,
                        Reason = $"Completed Instant Reward: {template.TitleEn}",
                        SourceType = PointsSourceType.ChildTask,
                        SourceEntityId = childTask.Id
                    });

                    response.Status = Enums.Tasks.TaskStatus.Completed;
                    response.AwardedPoints = template.BasePoints;
                    response.Message = "Task completed! Points awarded.";
                    break;

                case TaskTemplateType.VoiceQuestion:

                    if (request.VoiceFile == null)
                        return _response.BadRequest<SubmitTaskResponse>("Voice file is required for VoiceQuestion");

                    // منع المحاولات لو وصل الحد الأقصى
                    if (childTask.AttemptCount >= template.MaxVoiceAttempts)
                        return _response.BadRequest<SubmitTaskResponse>("Maximum voice attempts reached for this task");

                    // رفع ملف الصوت
                    var voiceUploadedResponse = await _fileUploader.UploadAsync(request.VoiceFile);
                    if (voiceUploadedResponse == null)
                        return _response.ServerError<SubmitTaskResponse>("Failed to upload voice file");

                    childTask.AnswerMediaUrl = voiceUploadedResponse.Url;

                    // استدعاء AI API
                    var aiResult = await CallVoiceShadowingApi(
                        request.VoiceFile,
                        template.VoiceExpectedCorrectAnswer
                    );

                    // تحديث بيانات المحاولة
                    childTask.AttemptCount++;
                    childTask.LastVoiceSubmitAttempt = DateTime.UtcNow;

                    // تحليل النتيجة
                    if (aiResult.ScoreStatus == "Excellent" || aiResult.ScoreStatus == "Good")
                    {
                        childTask.Status = TaskStatus.Completed;
                        childTask.CompletedAt = DateTime.UtcNow;

                        childTask.Child.TotalPoints += template.BasePoints;

                        _context.PointsTransactions.Add(new PointsTransaction
                        {
                            ChildId = childId,
                            Points = template.BasePoints,
                            Reason = $"Completed VoiceQuestion: {template.TitleEn}",
                            SourceType = PointsSourceType.ChildTask,
                            SourceEntityId = childTask.Id
                        });

                        response.AwardedPoints = template.BasePoints;
                        response.Message = $"Great job! Score: {aiResult.ScoreStatus}";
                    }
                    else if (aiResult.ScoreStatus == "Poor")
                    {
                        if (childTask.AttemptCount >= template.MaxVoiceAttempts)
                        {
                            childTask.Status = TaskStatus.Rejected;
                            response.Message = "Maximum attempts reached. Task rejected.";
                        }
                        else
                        {
                            childTask.Status = TaskStatus.InProgress;
                            response.Message = $"Try again. Attempt {childTask.AttemptCount}/{template.MaxVoiceAttempts}";
                        }
                    }
                    else
                    {
                        // في حالة أي نتيجة غير متوقعة
                        childTask.Status = TaskStatus.InProgress;
                        response.Message = "Voice processed. Please try again.";
                    }

                    // إعداد الاستجابة
                    response.Status = childTask.Status;
                    response.ShadowingResult = aiResult;

                    break;

                case TaskTemplateType.EvidenceSubmission:

                    if (request.Comment != null)
                    {
                        childTask.ChildNote = request.Comment;
                    }

                    if (request.EvidenceFile == null)
                        return _response.BadRequest<SubmitTaskResponse>("Evidence file (image) is required");

                    // 1. رفع الصورة
                    var evidenceUrl = await _fileUploader.UploadAsync(request.EvidenceFile);
                    if (evidenceUrl == null)
                        return _response.ServerError<SubmitTaskResponse>("Failed to upload evidence image");

                    // 2. تغيير الحالة لـ ReviewRequested
                    childTask.Status = TaskStatus.ReviewRequested;
                    childTask.ReviewRequestedAt = DateTime.UtcNow;

                    // هنا ممكن نضيف حقل EvidenceUrl في ChildTask لو عايزين نخزن رابط الصورة
                    childTask.AnswerMediaUrl = evidenceUrl.Url;

                    // based on review authority (parent or supervisor review)
                    if (childTask.Source == TaskSource.Parent)
                    {
                        if (!string.IsNullOrEmpty(childTask.Child.ParentId))
                            _ = _notificationService.SendAsync(
                                childTask.Child.ParentId,
                                NotificationType.ReviewRequested,
                                "New Submission",
                                $"{childTask.Child.Name} submitted evidence for \"{childTask.Template.TitleEn}\".",
                                childTask.Id);

                        response.Status = TaskStatus.ReviewRequested;
                        response.Message = "Evidence submitted. Waiting for parent review.";
                        break;
                    }
                    else if (childTask.Source == TaskSource.InstitutionAdventure)
                    {
                        // review by supervisour
                        break;
                    }
                    break;

                default:
                    return _response.BadRequest<SubmitTaskResponse>("Unsupported task type");
            }

            await _context.SaveChangesAsync();
            return _response.Success(response, "Task submitted successfully");
        }


        public async Task<Response<List<ChildAdventureListItemResponse>>> GetMyAdventuresAsync(string childId)
        {
            var child = await _context.Childrens
                .FirstOrDefaultAsync(c => c.Id == childId && !c.IsDeleted);

            if (child == null)
                return _response.NotFound<List<ChildAdventureListItemResponse>>("Child not found");

            if (child.ClassId == null)
                return _response.Success(
                    new List<ChildAdventureListItemResponse>(),
                    "Child is not enrolled in any class");

            var today = DateTime.UtcNow.Date;

            var weeklyAdventures = await _context.WeeklyAdventures
                .Include(wa => wa.Adventure)
                    .ThenInclude(a => a.Tasks)
                .Where(wa => wa.ClassId == child.ClassId && !wa.IsDeleted)
                .OrderByDescending(wa => wa.StartDate)
                .ToListAsync();

            if (!weeklyAdventures.Any())
                return _response.Success(new List<ChildAdventureListItemResponse>(), "No adventures found");

            var weeklyAdventureIds = weeklyAdventures.Select(wa => wa.Id).ToList();

            // جيب الـ Progress
            var progresses = await _context.ChildAdventureProgresses
                .Where(p => p.ChildId == childId
                         && weeklyAdventureIds.Contains(p.WeeklyAdventureId))
                .ToDictionaryAsync(p => p.WeeklyAdventureId);

            // جيب كل الـ Submissions مرة واحدة
            var allSubmissions = await _context.ChildAdventureTasks
                .Where(cat => cat.ChildId == childId
                           && weeklyAdventureIds.Contains(cat.WeeklyAdventureId)
                           && !cat.IsDeleted)
                .Select(cat => new
                {
                    cat.WeeklyAdventureId,
                    cat.AdventureTaskId,
                    cat.Status
                })
                .ToListAsync();

            // نجمع الـ Submissions لكل WeeklyAdventure
            var submissionsMap = allSubmissions
                .GroupBy(s => s.WeeklyAdventureId)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToDictionary(s => s.AdventureTaskId, s => s.Status)
                );

            var result = weeklyAdventures.Select(wa =>
            {
                var currentDay = Math.Clamp(
                    (today - wa.StartDate.Date).Days + 1,
                    1,
                    wa.Adventure.WeekDuration);

                progresses.TryGetValue(wa.Id, out var progress);
                submissionsMap.TryGetValue(wa.Id, out var submissions);

                // ✅ نبني حالة كل يوم
                var daysStatus = wa.Adventure.Tasks
                    .OrderBy(t => t.DayNumber)
                    .Select(t =>
                    {
                        AdventureDayStatus dayStatus;

                        if (t.DayNumber > currentDay)
                        {
                            // اليوم لسه ما جاش
                            dayStatus = AdventureDayStatus.Locked;
                        }
                        else if (submissions != null && submissions.TryGetValue(t.Id, out var subStatus))
                        {
                            // عمل Submission
                            dayStatus = subStatus == AdventureChildTaskStatus.Missed
                                ? AdventureDayStatus.Missed
                                : AdventureDayStatus.Completed;
                        }
                        else if (t.DayNumber == currentDay)
                        {
                            // اليوم الحالي ومعملش Submit لسه
                            dayStatus = AdventureDayStatus.Unlocked;
                        }
                        else
                        {
                            // يوم فات ومعملش Submit → Missed
                            dayStatus = AdventureDayStatus.Missed;
                        }

                        return new AdventureDayStatusResponse
                        {
                            DayNumber = t.DayNumber,
                            Status = dayStatus
                        };
                    })
                    .ToList();

                return new ChildAdventureListItemResponse
                {
                    WeeklyAdventureId = wa.Id,
                    AdventureId = wa.AdventureId,
                    TitleEn = wa.Adventure.TitleEn,
                    TitleAr = wa.Adventure.TitleAr,
                    BannerImageUrl = wa.Adventure.BannerImageUrl,
                    DescriptionVoiceUrl = wa.Adventure.DescriptionVoiceUrl,
                    DescriptionEn = wa.Adventure.DescriptionEn,
                    DescriptionAr = wa.Adventure.DescriptionEn,
                    TotalDays = wa.Adventure.Tasks.Count,
                    CurrentDay = currentDay,
                    BonusPoints = wa.Adventure.BonusPoints,
                    Status = wa.Status,
                    StartDate = wa.StartDate,
                    EndDate = wa.EndDate,
                    CompletedTasksCount = daysStatus.Count(d => d.Status == AdventureDayStatus.Completed),
                    EarnedStars = progress?.EarnedStars ?? 0,
                    EarnedPoints = progress?.EarnedPoints ?? 0,
                    IsCompleted = progress?.IsCompleted ?? false,
                    DaysStatus = daysStatus
                };
            }).ToList();

            return _response.Success(result, "Adventures retrieved successfully");
        }
        
        public async Task<Response<ChildAdventureDetailsResponse>> GetAdventureDetailsAsync(
            string childId,
            string weeklyAdventureId)
        {
            var child = await _context.Childrens
                .FirstOrDefaultAsync(c => c.Id == childId && !c.IsDeleted);

            if (child == null)
                return _response.NotFound<ChildAdventureDetailsResponse>("Child not found");

            var weeklyAdventure = await _context.WeeklyAdventures
                .Include(wa => wa.Adventure)
                    .ThenInclude(a => a.Tasks)
                        .ThenInclude(t => t.TaskTemplate)
                .FirstOrDefaultAsync(wa => wa.Id == weeklyAdventureId
                                        && wa.ClassId == child.ClassId
                                        && !wa.IsDeleted);

            if (weeklyAdventure == null)
                return _response.NotFound<ChildAdventureDetailsResponse>(
                    "Adventure not found or not assigned to your class");

            var today = DateTime.UtcNow.Date;
            var currentDay = Math.Clamp(
                (today - weeklyAdventure.StartDate.Date).Days + 1,
                1,
                weeklyAdventure.Adventure.WeekDuration);

            // جيب الـ Progress
            var progress = await _context.ChildAdventureProgresses
                .FirstOrDefaultAsync(p => p.ChildId == childId
                                       && p.WeeklyAdventureId == weeklyAdventureId);

            // جيب كل الـ Submissions بتاعة الطفل
            var submissions = await _context.ChildAdventureTasks
                .Where(cat => cat.ChildId == childId
                           && cat.WeeklyAdventureId == weeklyAdventureId
                           && !cat.IsDeleted)
                .ToDictionaryAsync(cat => cat.AdventureTaskId);

            var tasks = weeklyAdventure.Adventure.Tasks
                .OrderBy(t => t.DayNumber)
                .Select(t =>
                {
                    ChildAdventureTaskAccessStatus accessStatus;
                    if (t.DayNumber > currentDay)
                        accessStatus = ChildAdventureTaskAccessStatus.Locked;
                    else if (submissions.ContainsKey(t.Id))
                        accessStatus = ChildAdventureTaskAccessStatus.Done;
                    else
                        accessStatus = ChildAdventureTaskAccessStatus.Unlocked;

                    submissions.TryGetValue(t.Id, out var submission);

                    ChildAdventureTaskSubmissionStatus? submissionStatus = null;
                    if (accessStatus != ChildAdventureTaskAccessStatus.Locked)
                    {
                        submissionStatus = submission == null
                            ? ChildAdventureTaskSubmissionStatus.NotSubmitted
                            : submission.IsApproved == true
                                ? ChildAdventureTaskSubmissionStatus.Approved
                                : submission.Status == AdventureChildTaskStatus.Missed
                                    ? ChildAdventureTaskSubmissionStatus.Missed
                                    : ChildAdventureTaskSubmissionStatus.Pending;
                    }

                    return new ChildAdventureTaskItemResponse
                    {
                        AdventureTaskId = t.Id,
                        TaskTemplateId = t.TaskTemplateId,
                        DayNumber = t.DayNumber,
                        
                        TitleEn = accessStatus != ChildAdventureTaskAccessStatus.Locked
                            ? t.TaskTemplate.TitleEn : null,
                        TitleAr = accessStatus != ChildAdventureTaskAccessStatus.Locked
                            ? t.TaskTemplate.TitleAr : null,
                        StoryText = accessStatus != ChildAdventureTaskAccessStatus.Locked
                            ? t.StoryText : null,
                        StoryVoiceUrl = accessStatus != ChildAdventureTaskAccessStatus.Locked
                            ? t.StoryVoiceUrl : null,
                        Stars = t.Stars,
                        AccessStatus = accessStatus,
                        SubmissionStatus = submissionStatus,
                        EvidenceUrl = submission?.EvidenceUrl,
                        EarnedStars = submission?.EarnedStars ?? 0,
                        SubmittedAt = submission?.SubmittedAt, 
                        Type = t.TaskTemplate.TemplateType.ToString()
                    };
                })
                .ToList();

            var result = new ChildAdventureDetailsResponse
            {
                WeeklyAdventureId = weeklyAdventure.Id,
                AdventureId = weeklyAdventure.AdventureId,
                TitleEn = weeklyAdventure.Adventure.TitleEn,
                TitleAr = weeklyAdventure.Adventure.TitleAr,
                BannerImageUrl = weeklyAdventure.Adventure.BannerImageUrl,
                DescriptionEn = weeklyAdventure.Adventure.DescriptionEn,
                DescriptionAr = weeklyAdventure.Adventure.DescriptionAr,
                GoalEn = weeklyAdventure.Adventure.GoalEn,
                GoalAr = weeklyAdventure.Adventure.GoalAr,
                DescriptionVoiceUrl = weeklyAdventure.Adventure.DescriptionVoiceUrl,
                BonusPoints = weeklyAdventure.Adventure.BonusPoints,
                TotalDays = weeklyAdventure.Adventure.Tasks.Count,
                CurrentDay = currentDay,
                StartDate = weeklyAdventure.StartDate,
                EndDate = weeklyAdventure.EndDate,
                Status = weeklyAdventure.Status,
                CompletedTasksCount = progress?.CompletedDaysCount ?? 0,
                EarnedStars = progress?.EarnedStars ?? 0,
                EarnedPoints = progress?.EarnedPoints ?? 0,
                IsCompleted = progress?.IsCompleted ?? false,
                Tasks = tasks
            };

            return _response.Success(result, "Adventure details retrieved successfully");
        }

        /// <summary>
        /// Retrieves all weekly adventures assigned to the logged-in child along with their progress and day-by-day status.
        /// </summary>
        /// <remarks>
        /// This method:
        /// 1. Validates the child and ensures they are enrolled in a class.
        /// 2. Retrieves all weekly adventures assigned to the child's class.
        /// 3. Loads progress data and task submissions in bulk for performance.
        /// 4. Calculates the current adventure day based on start date.
        /// 5. Builds a detailed status for each adventure day (Locked, Unlocked, Completed, Missed).
        /// 6. Returns a summary view including earned points, stars, and completion status.
        /// </remarks>
        public async Task<Response<ChildWeeklyAdventureResponse>> GetWeeklyAdventureTasksAsync(
            string childId,
            string weeklyAdventureId)
        {
            // تحقق إن الطفل في الكلاس بتاعة الـ WeeklyAdventure دي
            var child = await _context.Childrens
                .FirstOrDefaultAsync(c => c.Id == childId && !c.IsDeleted);

            if (child == null)
                return _response.NotFound<ChildWeeklyAdventureResponse>("Child not found");

            var weeklyAdventure = await _context.WeeklyAdventures
                .Include(wa => wa.Adventure)
                    .ThenInclude(a => a.Tasks)
                        .ThenInclude(t => t.TaskTemplate)
                .FirstOrDefaultAsync(wa => wa.Id == weeklyAdventureId
                                        && wa.ClassId == child.ClassId
                                        && !wa.IsDeleted);

            if (weeklyAdventure == null)
                return _response.NotFound<ChildWeeklyAdventureResponse>(
                    "Adventure not found or not assigned to your class");

            var today = DateTime.UtcNow.Date;
            var currentDay = (today - weeklyAdventure.StartDate.Date).Days + 1;

            // جيب كل الـ ChildAdventureTasks الموجودة للطفل ده في الـ Adventure دي
            var existingSubmissions = await _context.ChildAdventureTasks
                .Where(cat => cat.ChildId == childId
                           && cat.WeeklyAdventureId == weeklyAdventureId
                           && !cat.IsDeleted)
                .ToDictionaryAsync(cat => cat.AdventureTaskId);

            var tasks = weeklyAdventure.Adventure.Tasks
                .OrderBy(t => t.DayNumber)
                .Select(t =>
                {
                    // حدد الـ AccessStatus
                    ChildAdventureTaskAccessStatus accessStatus;
                    if (t.DayNumber > currentDay)
                        accessStatus = ChildAdventureTaskAccessStatus.Locked;
                    else if (existingSubmissions.ContainsKey(t.Id))
                        accessStatus = ChildAdventureTaskAccessStatus.Done;
                    else
                        accessStatus = ChildAdventureTaskAccessStatus.Unlocked;

                    // جيب الـ Submission لو موجودة
                    existingSubmissions.TryGetValue(t.Id, out var submission);

                    ChildAdventureTaskSubmissionStatus? submissionStatus = null;
                    if (accessStatus != ChildAdventureTaskAccessStatus.Locked)
                    {
                        submissionStatus = submission == null
                            ? ChildAdventureTaskSubmissionStatus.NotSubmitted
                            : submission.IsApproved == true
                                ? ChildAdventureTaskSubmissionStatus.Approved
                                : submission.Status == AdventureChildTaskStatus.Missed
                                    ? ChildAdventureTaskSubmissionStatus.Missed
                                    : ChildAdventureTaskSubmissionStatus.Pending;
                    }

                    return new ChildAdventureTaskItemResponse
                    {
                        AdventureTaskId = t.Id,
                        TaskTemplateId = t.TaskTemplateId,
                        DayNumber = t.DayNumber,

                        // بنبعت التفاصيل بس لو مش Locked
                        TitleEn = accessStatus != ChildAdventureTaskAccessStatus.Locked
                            ? t.TaskTemplate.TitleEn : null,
                        TitleAr = accessStatus != ChildAdventureTaskAccessStatus.Locked
                            ? t.TaskTemplate.TitleAr : null,
                        StoryText = accessStatus != ChildAdventureTaskAccessStatus.Locked
                            ? t.StoryText : null,
                        StoryVoiceUrl = accessStatus != ChildAdventureTaskAccessStatus.Locked
                            ? t.StoryVoiceUrl : null,

                        Stars = t.Stars,
                        AccessStatus = accessStatus,
                        SubmissionStatus = submissionStatus,
                        EvidenceUrl = submission?.EvidenceUrl,
                        EarnedStars = submission?.EarnedStars ?? 0,
                        SubmittedAt = submission?.SubmittedAt
                    };
                })
                .ToList();

            var result = new ChildWeeklyAdventureResponse
            {
                WeeklyAdventureId = weeklyAdventure.Id,
                AdventureId = weeklyAdventure.AdventureId,
                TitleEn = weeklyAdventure.Adventure.TitleEn,
                TitleAr = weeklyAdventure.Adventure.TitleAr,
                DescriptionVoiceUrl = weeklyAdventure.Adventure.DescriptionVoiceUrl,
                StartDate = weeklyAdventure.StartDate,
                EndDate = weeklyAdventure.EndDate,
                TotalDays = weeklyAdventure.Adventure.Tasks.Count,
                CurrentDay = currentDay,
                Tasks = tasks
            };

            return _response.Success(result, "Adventure tasks retrieved successfully");
        }

        /// <summary>
        /// Retrieves detailed information for a specific weekly adventure for the logged-in child,
        /// including all tasks, progress, and submission statuses.
        /// </summary>
        /// <remarks>
        /// This method:
        /// 1. Validates the child and ensures the adventure belongs to their class.
        /// 2. Loads the weekly adventure with its tasks and templates.
        /// 3. Calculates the current day of the adventure based on start date.
        /// 4. Retrieves child progress and task submissions.
        /// 5. Determines access status for each task (Locked, Unlocked, Done).
        /// 6. Determines submission status (NotSubmitted, Pending, Approved, Missed).
        /// 7. Hides task content for locked days to enforce progression rules.
        /// 8. Returns full adventure details including progress, rewards, and tasks.
        /// </remarks>
        public async Task<Response<SubmitTaskResponse>> SubmitAdventureTaskAsync(
            string childId,
            SubmitAdventureTaskRequest request)
        {
            // جيب الـ AdventureTask
            var adventureTask = await _context.AdventureTasks
                .Include(at => at.TaskTemplate)
                .FirstOrDefaultAsync(at => at.Id == request.AdventureTaskId && !at.IsDeleted);

            if (adventureTask == null)
                return _response.NotFound<SubmitTaskResponse>("Adventure task not found");

            // تحقق إن الـ WeeklyAdventure موجودة وتابعة لكلاس الطفل
            var child = await _context.Childrens
                .FirstOrDefaultAsync(c => c.Id == childId && !c.IsDeleted);

            var weeklyAdventure = await _context.WeeklyAdventures
                .FirstOrDefaultAsync(wa => wa.Id == request.WeeklyAdventureId
                                        && wa.ClassId == child!.ClassId
                                        && !wa.IsDeleted);

            if (weeklyAdventure == null)
                return _response.NotFound<SubmitTaskResponse>("Weekly adventure not found");

            // تحقق إن اليوم ده مفتوح
            var today = DateTime.UtcNow.Date;
            var currentDay = (today - weeklyAdventure.StartDate.Date).Days + 1;

            if (adventureTask.DayNumber > currentDay)
                return _response.BadRequest<SubmitTaskResponse>("This task is not unlocked yet");

            if (adventureTask.DayNumber < currentDay)
                return _response.BadRequest<SubmitTaskResponse>("This task's day has already passed");

            // تحقق إن ما عملش Submit قبل كده
            var existingSubmission = await _context.ChildAdventureTasks
                .FirstOrDefaultAsync(cat => cat.ChildId == childId
                                         && cat.AdventureTaskId == request.AdventureTaskId
                                         && cat.WeeklyAdventureId == request.WeeklyAdventureId
                                         && !cat.IsDeleted);

            if (existingSubmission != null && existingSubmission.Status != AdventureChildTaskStatus.Pending)
                return _response.BadRequest<SubmitTaskResponse>("Task already submitted");

            var template = adventureTask.TaskTemplate;
            var response = new SubmitTaskResponse { TaskId = request.AdventureTaskId };

            // كرير الـ ChildAdventureTask
            var childAdventureTask = existingSubmission ?? new ChildAdventureTask
            {
                Id = Guid.NewGuid().ToString(),
                ChildId = childId,
                AdventureTaskId = request.AdventureTaskId,
                WeeklyAdventureId = request.WeeklyAdventureId,
                CreatedBy = childId
            };

            childAdventureTask.SubmittedAt = DateTime.UtcNow;
            childAdventureTask.UpdatedAt = DateTime.UtcNow;
            childAdventureTask.UpdatedBy = childId;

            switch (template.TemplateType)
            {
                case TaskTemplateType.InstantReward:
                    childAdventureTask.Status = AdventureChildTaskStatus.Completed;
                    childAdventureTask.CompletedAt = DateTime.UtcNow;
                    childAdventureTask.EarnedStars = adventureTask.Stars;
                    childAdventureTask.IsApproved = true;

                    await _pointsService.AwardPointsAsync(
                        childId,
                        template.BasePoints,
                        PointsSourceType.ChildTask,
                        childAdventureTask.Id,
                        $"Completed Adventure Task: {template.TitleEn}"
                    );

                    response.Status = Enums.Tasks.TaskStatus.Completed;
                    response.AwardedPoints = template.BasePoints;
                    response.Message = "Task completed! Points awarded.";
                    break;

                case TaskTemplateType.VoiceQuestion:
                    if (request.VoiceFile == null)
                        return _response.BadRequest<SubmitTaskResponse>("Voice file is required");

                    var voiceUpload = await _fileUploader.UploadAsync(request.VoiceFile);
                    if (voiceUpload == null)
                        return _response.ServerError<SubmitTaskResponse>("Failed to upload voice file");

                    childAdventureTask.EvidenceUrl = voiceUpload.Url;

                    var aiResult = await CallVoiceShadowingApi(
                        request.VoiceFile,
                        template.VoiceExpectedCorrectAnswer
                    );

                    if (aiResult.ScoreStatus == "Excellent" || aiResult.ScoreStatus == "Good")
                    {
                        childAdventureTask.Status = AdventureChildTaskStatus.Completed;
                        childAdventureTask.CompletedAt = DateTime.UtcNow;
                        childAdventureTask.EarnedStars = adventureTask.Stars;
                        childAdventureTask.IsApproved = true;

                        await _pointsService.AwardPointsAsync(
                            childId,
                            template.BasePoints,
                            PointsSourceType.ChildTask,
                            childAdventureTask.Id,
                            $"Completed Adventure VoiceQuestion: {template.TitleEn}"
                        );

                        response.AwardedPoints = template.BasePoints;
                        response.Message = $"Great job! Score: {aiResult.ScoreStatus}";
                    }
                    else
                    {
                        childAdventureTask.Status = AdventureChildTaskStatus.Pending;
                        response.Message = "Try again!";
                    }

                    response.ShadowingResult = aiResult;
                    response.Status = Enums.Tasks.TaskStatus.Completed;
                    break;

                case TaskTemplateType.EvidenceSubmission:
                    if (request.EvidenceFile == null)
                        return _response.BadRequest<SubmitTaskResponse>("Evidence file is required");

                    var evidenceUpload = await _fileUploader.UploadAsync(request.EvidenceFile);
                    if (evidenceUpload == null)
                        return _response.ServerError<SubmitTaskResponse>("Failed to upload evidence");

                    childAdventureTask.EvidenceUrl = evidenceUpload.Url;
                    childAdventureTask.Status = AdventureChildTaskStatus.Completed;
                    childAdventureTask.IsApproved = null; // في انتظار الـ Supervisor

                    response.Status = Enums.Tasks.TaskStatus.ReviewRequested;
                    response.Message = "Evidence submitted. Waiting for supervisor review.";
                    break;

                default:
                    return _response.BadRequest<SubmitTaskResponse>("Unsupported task type");
            }

            if (existingSubmission == null)
                _context.ChildAdventureTasks.Add(childAdventureTask);

            await _context.SaveChangesAsync();
            return _response.Success(response, "Task submitted successfully");
        }


        private async Task<VoiceShadowingResult> CallVoiceShadowingApi(IFormFile audioFile, string targetText)
        {
            var client = _httpClientFactory.CreateClient();

            var formData = new MultipartFormDataContent();

            var stream = audioFile.OpenReadStream();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(audioFile.ContentType);


            formData.Add(fileContent, "audio", audioFile.FileName);
            formData.Add(
                new StringContent(targetText, Encoding.UTF8, "text/plain"),
                "target"
            );
            formData.Add(new StringContent("shadowing"), "task_type");

            var response = await client.PostAsync(
       "https://waad-moaness-faster-whisper-voice-tasks-api.hf.space/voice_task",formData);

            var errorBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"AI API call failed: {response.StatusCode} - {errorBody}");

            var resultJson = await response.Content.ReadAsStringAsync();

            var aiResponse = JsonSerializer.Deserialize<AiShadowingResponse>(
                resultJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (aiResponse == null)
                return new VoiceShadowingResult();


            var result = new VoiceShadowingResult
            {
                ScoreStatus = Capitalize(aiResponse.score_status)
            };

            result.Words = aiResponse.word_status
                .Select(w => new WordShadowing
                {
                    Word = w.word,
                    Color = w.status
                })
                .ToList();

            return result;
        }
        
        private string Capitalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Poor";

            return char.ToUpper(value[0]) + value.Substring(1).ToLower();
        }
    }
}

