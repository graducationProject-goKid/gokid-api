namespace GoKidAPI.DTO.Adventures.Responses
{
    public class AssignAdventureToClassResponse
    {
        public string WeeklyAdventureId { get; set; } = null!;
        public string AdventureTitleEn { get; set; } = null!;
        public string AdventureTitleAr { get; set; } = null!;
        public string ClassName { get; set; } = null!;
        public string ClassId { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
