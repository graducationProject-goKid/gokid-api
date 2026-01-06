using GoKidAPI.DTO.ImageUploading;

namespace GoKidAPI.Services.ImageUploading
{
    public interface IFileUploader
    {
        Task<UploadImageResponse> UploadAsync(IFormFile file);

        Task<bool> DeleteAsync(string publicId);
    }
}
