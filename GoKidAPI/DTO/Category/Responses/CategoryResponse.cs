using GoKidAPI.DTO.ImageUploading;

namespace GoKidAPI.DTO.Category.Responses
{
    public class CategoryResponse
    {
        public string Id { get; set; } = null!;
        public string NameAr { get; set; } = null!;
        public string NameEn { get; set; } = null!;
        public UploadImageResponse? Icon { get; set; }
        public string? ColorHex { get; set; }
        public int SubCategoriesCount { get; set; }
    }
}
