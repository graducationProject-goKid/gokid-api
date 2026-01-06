namespace GoKidAPI.DTO.Category.Requests
{
    public class CreateSubCategoryRequest
    {
        public string NameAr { get; set; } = null!;
        public string NameEn { get; set; } = null!;
        public IFormFile IconFile { get; set; } = null!;
        public string CategoryId { get; set; } = null!;
    }
}
