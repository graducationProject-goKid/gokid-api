namespace GoKidAPI.DTO.Category.Requests
{
    public class UpdateSubCategoryRequest
    {
        public string? NameAr { get; set; }
        public string? NameEn { get; set; }
        public IFormFile? IconFile { get; set; }
        public string? CategoryId { get; set; } = null!;
    }
}
