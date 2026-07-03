namespace GoKidAPI.DTO.Levels.Responses
{
    public class LevelResponse
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int Order { get; set; }
        public int MinPoints { get; set; }
        public string? BadgeUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class LevelInfo
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int Order { get; set; }
        public string? BadgeUrl { get; set; }
    }
}
