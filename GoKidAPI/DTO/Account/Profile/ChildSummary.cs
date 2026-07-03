using GoKidAPI.DTO.Levels.Responses;
using GoKidAPI.Enums;

namespace GoKidAPI.DTO.Account.Profile
{
    public class ChildSummary
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? NickName { get; set; }
        public int Age { get; set; }
        public Gender Gender { get; set; }
        public string? AvatarUrl { get; set; }
        public int TotalPoints { get; set; }
        public int HighestPoints { get; set; }
        public string? RegistrationCode { get; set; }
        public string? ClassName { get; set; }
        public string? InstitutionName { get; set; }
        public LevelInfo? Level { get; set; }
    }
}
