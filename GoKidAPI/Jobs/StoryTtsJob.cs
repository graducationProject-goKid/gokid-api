using GoKidAPI.Data;
using GoKidAPI.Enums;
using GoKidAPI.Services.ImageUploading;
using GoKidAPI.Services.Notifications;
using GoKidAPI.Services.TTSService;

using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Jobs
{
    public class StoryTtsJob
    {
        private readonly AppDbContext _context;
        private readonly IStoryTtsService _storyTtsService;
        private readonly IFileUploader _fileUploader;
        private readonly INotificationService _notificationService;
        private readonly ILogger<StoryTtsJob> _logger;

        public StoryTtsJob(
            AppDbContext context,
            IStoryTtsService storyTtsService,
            IFileUploader fileUploader,
            INotificationService notificationService,
            ILogger<StoryTtsJob> logger)
        {
            _context = context;
            _storyTtsService = storyTtsService;
            _fileUploader = fileUploader;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task ProcessStoryTtsAsync(string adventureId, string adminId)
        {
            try
            {
                var adventure = await _context.Adventures
                    .Include(a => a.Tasks)
                    .FirstOrDefaultAsync(a => a.Id == adventureId);

                if (adventure == null)
                {
                    _logger.LogWarning("Adventure {Id} not found for Story TTS", adventureId);
                    return;
                }

                // 1. Intro TTS
                if (!string.IsNullOrWhiteSpace(adventure.IntroStory)
                    && string.IsNullOrEmpty(adventure.IntroVoiceUrl))
                {
                    var bytes = await _storyTtsService.ConvertStoryToSpeechAsync(adventure.IntroStory);
                    var file = ConvertToFormFile(bytes, "intro.mp3");
                    var upload = await _fileUploader.UploadAsync(file);

                    adventure.IntroVoiceUrl = upload.Url;
                    adventure.IntroVoicePublicId = upload.PublicId;

                    adventure.DescriptionVoiceUrl = adventure.IntroVoiceUrl;
                    adventure.DescriptionVoicePublicId = adventure.IntroVoicePublicId;
                }

                // 2. Each Day Story TTS
                foreach (var task in adventure.Tasks
                    .Where(t => !string.IsNullOrWhiteSpace(t.StoryText)
                             && string.IsNullOrEmpty(t.StoryVoiceUrl))
                    .OrderBy(t => t.DayNumber))
                {
                    var bytes = await _storyTtsService.ConvertStoryToSpeechAsync(task.StoryText!);
                    var file = ConvertToFormFile(bytes, $"day_{task.DayNumber}_story.mp3");
                    var upload = await _fileUploader.UploadAsync(file);
                    task.StoryVoiceUrl = upload.Url;
                    task.StoryVoicePublicId = upload.PublicId;
                }

                // 3. Outro TTS
                if (!string.IsNullOrWhiteSpace(adventure.OutroStory)
                    && string.IsNullOrEmpty(adventure.OutroVoiceUrl))
                {
                    var bytes = await _storyTtsService.ConvertStoryToSpeechAsync(adventure.OutroStory);
                    var file = ConvertToFormFile(bytes, "outro.mp3");
                    var upload = await _fileUploader.UploadAsync(file);
                    adventure.OutroVoiceUrl = upload.Url;
                    adventure.OutroVoicePublicId = upload.PublicId;
                }

                await _context.SaveChangesAsync();

                await _notificationService.SendAsync(
                    userId: adminId,
                    type: NotificationType.AdventureStarted,
                    title: "Story Voice Ready",
                    body: $"Story voice for '{adventure.TitleEn}' has been generated successfully.",
                    relatedEntityId: adventureId);

                _logger.LogInformation("Story TTS completed for adventure {Id}", adventureId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Story TTS failed for adventure {Id}", adventureId);

                await _notificationService.SendAsync(
                    userId: adminId,
                    type: NotificationType.AdventureStarted,
                    title: "Story Voice Failed",
                    body: "Story voice generation failed. Please try again.",
                    relatedEntityId: adventureId);
            }
        }

        private IFormFile ConvertToFormFile(byte[] fileBytes, string fileName)
        {
            var stream = new MemoryStream(fileBytes);
            return new FormFile(stream, 0, fileBytes.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "audio/mpeg"
            };
        }
    }
}
