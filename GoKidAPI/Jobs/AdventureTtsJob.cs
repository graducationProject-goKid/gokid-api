using GoKidAPI.Data;
using GoKidAPI.Enums;
using GoKidAPI.Services.ImageUploading;
using GoKidAPI.Services.Notifications;
using GoKidAPI.Services.TTSService;

using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Jobs
{
    public class AdventureTtsJob
    {
        private readonly AppDbContext _context;
        private readonly ITextToSpeechService _ttsService;
        private readonly IFileUploader _fileUploader;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AdventureTtsJob> _logger;

        public AdventureTtsJob(
            AppDbContext context,
            ITextToSpeechService ttsService,
            IFileUploader fileUploader,
            INotificationService notificationService,
            ILogger<AdventureTtsJob> logger)
        {
            _context = context;
            _ttsService = ttsService;
            _fileUploader = fileUploader;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task ProcessAdventureTtsAsync(string adventureId, string adminId)
        {
            try
            {
                var adventure = await _context.Adventures
                    .Include(a => a.Tasks)
                    .FirstOrDefaultAsync(a => a.Id == adventureId);

                if (adventure == null)
                {
                    _logger.LogWarning("Adventure {Id} not found for TTS processing", adventureId);
                    return;
                }

                // 1. Description Voice
                if (string.IsNullOrEmpty(adventure.DescriptionVoiceUrl)
                    && !string.IsNullOrWhiteSpace(adventure.DescriptionEn))
                {
                    var bytes = await _ttsService.ConvertTextToSpeechAsync(adventure.DescriptionEn);
                    var file = ConvertToFormFile(bytes, "description.mp3");
                    var upload = await _fileUploader.UploadAsync(file);

                    adventure.DescriptionVoiceUrl = upload.Url;
                    adventure.DescriptionVoicePublicId = upload.PublicId;
                }

                // 2. Each Task StoryText
                foreach (var task in adventure.Tasks.Where(t => string.IsNullOrEmpty(t.StoryVoiceUrl)
                                                              && !string.IsNullOrWhiteSpace(t.StoryText)))
                {
                    var bytes = await _ttsService.ConvertTextToSpeechAsync(task.StoryText!);
                    var file = ConvertToFormFile(bytes, $"task_{task.DayNumber}.mp3");
                    var upload = await _fileUploader.UploadAsync(file);

                    task.StoryVoiceUrl = upload.Url;
                    task.StoryVoicePublicId = upload.PublicId;
                }

                await _context.SaveChangesAsync();

                await _notificationService.SendAsync(
                    userId: adminId,
                    type: NotificationType.AdventureStarted,
                    title: "Adventure Voice Ready",
                    body: $"Voice for adventure '{adventure.TitleEn}' has been generated successfully.",
                    relatedEntityId: adventureId);

                _logger.LogInformation("TTS completed for adventure {Id}", adventureId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TTS processing failed for adventure {Id}", adventureId);

                await _notificationService.SendAsync(
                    userId: adminId,
                    type: NotificationType.AdventureStarted,
                    title: "Adventure Voice Failed",
                    body: "Voice generation failed. Please try again.",
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
