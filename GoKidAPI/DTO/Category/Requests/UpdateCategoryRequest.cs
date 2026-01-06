namespace GoKidAPI.DTO.Category.Requests
{
    public class UpdateCategoryRequest
    {
        public string? NameAr { get; set; }
        public string? NameEn { get; set; }
        public IFormFile? IconFile{ get; set; }
        public string? ColorHex { get; set; }
    }
}
