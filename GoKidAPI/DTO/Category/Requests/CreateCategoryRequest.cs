namespace GoKidAPI.DTO.Category.Requests
{
    public class CreateCategoryRequest
    {
        public string NameAr { get; set; } = null!;
        public string NameEn { get; set; } = null!;
        public IFormFile? IconFile { get; set; }
        public string? ColorHex { get; set; }
    }
}
