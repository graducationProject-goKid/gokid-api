
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

using GoKidAPI.DTO.ImageUploading;
using GoKidAPI.InfrastructreManage.Options;

using Microsoft.Extensions.Options;

namespace GoKidAPI.Services.ImageUploading
{
    public class CloudinaryImageUploadService : IFileUploader
    {
        private readonly Cloudinary _cloudinary;
        private readonly CloudinaryOptions _cloudinarySettings;
        private readonly ILogger<CloudinaryImageUploadService> _logger;

        public CloudinaryImageUploadService(IOptions<CloudinaryOptions> Cloudinaryoptions, ILogger<CloudinaryImageUploadService> logger)
        {
            _cloudinarySettings = Cloudinaryoptions.Value ?? throw new ArgumentNullException(nameof(Cloudinaryoptions));
            var account = new Account(_cloudinarySettings.CloudName, _cloudinarySettings.ApiKey, _cloudinarySettings.ApiSecret);

            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true;
            _logger = logger;
        }

        public async Task<UploadImageResponse> UploadAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or null");

            await using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var extension = Path.GetExtension(file.FileName).ToLower();

            RawUploadResult result;

            if (extension == ".mp3" || extension == ".wav" || extension == ".m4a" || extension == ".aac" || extension == ".ogg")
            {
                var uploadParams = new RawUploadParams
                {
                    File = new FileDescription(file.FileName, memoryStream)
                };

                result = await _cloudinary.UploadAsync(uploadParams);
            }
            else
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, memoryStream)
                };

                result = await _cloudinary.UploadAsync(uploadParams);
            }

            if (result == null)
                throw new Exception("Upload result was null from Cloudinary.");

            if (result.Error != null)
                throw new Exception($"Cloudinary error occurred: {result.Error.Message}");

            return new UploadImageResponse
            {
                Url = result.SecureUrl?.ToString()!,
                PublicId = result.PublicId
            };
        }

        public async Task<bool> DeleteAsync(string publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId))
                return false;

            var deletionParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deletionParams);

            return result.Result == "ok";
        }
    }
}
