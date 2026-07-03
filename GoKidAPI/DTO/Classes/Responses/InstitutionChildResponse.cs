using GoKidAPI.DTO.Levels.Responses;

namespace GoKidAPI.DTO.Classes.Responses
{
    public class InstitutionChildResponse
    {
        public string ChildId { get; set; } = null!;
        public string ChildName { get; set; } = null!;
        public string? NickName { get; set; }
        public string? AvatarUrl { get; set; }
        public int Age { get; set; }
        public int TotalPoints { get; set; }
        public string? ClassId { get; set; }
        public string? ClassName { get; set; }
        public LevelInfo? Level { get; set; }
    }
}
