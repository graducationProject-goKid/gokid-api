namespace GoKidAPI.DTO.Levels.Requests
{
    public class CreateLevelRequest
    {
        public string Name { get; set; } = null!;
        public int Order { get; set; }
        public int MinPoints { get; set; }
        public IFormFile? Badge { get; set; }
    }
}
