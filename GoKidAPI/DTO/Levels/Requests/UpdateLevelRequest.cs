namespace GoKidAPI.DTO.Levels.Requests
{
    public class UpdateLevelRequest
    {
        public string? Name { get; set; }
        public int? Order { get; set; }
        public int? MinPoints { get; set; }
        public IFormFile? Badge { get; set; }
    }
}
