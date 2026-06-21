using GoKidAPI.Data;
using GoKidAPI.Hubs;
using GoKidAPI.Services.ImageUploading;
using GoKidAPI.Services.TTSService;

using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GoKidAPI.Jobs
{
    public class AdventureTtsJob
    {
        private readonly AppDbContext _context;
        private readonly ITextToSpeechService _ttsService;
        private readonly IFileUploader _fileUploader;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<AdventureTtsJob> _logger;

        public AdventureTtsJob(
            AppDbContext context,
            ITextToSpeechService ttsService,
            IFileUploader fileUploader,
            IHubContext<NotificationHub> hubContext,
            ILogger<AdventureTtsJob> logger)
        {
            _context = context;
            _ttsService = ttsService;
            _fileUploader = fileUploader;
            _hubContext = hubContext;
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

                // 2. كل Task StoryText
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

                // 3. بعت SignalR Notification للـ Admin
                await _hubContext.Clients
                    .Group(adminId)
                    .SendAsync("AdventureVoiceReady", new
                    {
                        adventureId,
                        title = adventure.TitleEn,
                        descriptionVoiceUrl = adventure.DescriptionVoiceUrl,
                        message = $"Voice for adventure '{adventure.TitleEn}' is ready!"
                    });

                _logger.LogInformation("TTS completed for adventure {Id}", adventureId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TTS processing failed for adventure {Id}", adventureId);

                // بعت notification بالفشل كمان عشان الـ Admin يعرف
                await _hubContext.Clients
                    .Group(adminId)
                    .SendAsync("AdventureVoiceFailed", new
                    {
                        adventureId,
                        message = "Voice generation failed. Please try again."
                    });
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
