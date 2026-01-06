using GoKidAPI.DTO.ImageUploading;

namespace GoKidAPI.DTO.Category.Responses
{
    public class SubCategoryResponse
    {
        public string Id { get; set; } = null!;
        public string NameAr { get; set; } = null!;
        public string NameEn { get; set; } = null!;
        public UploadImageResponse? Icon { get; set; }
        public string CategoryId { get; set; } = null!;
        public string CategoryNameEn { get; set; } = null!;
        public string CategoryNameAr { get; set; } = null!;
    }
}
